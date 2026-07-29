using System.Globalization;
using Core.Storage;
using Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Project.Features.Storage.R2Sync;

public sealed partial class R2VehicleImageSyncService(
    ApplicationDbContext db,
    R2StorageService r2,
    ILogger<R2VehicleImageSyncService> logger)
{
    private const int PageSize = 100;
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public async Task SynchronizeAsync(
        Action<R2VehicleImageSyncProgress> onProgress,
        Action<int?, int?, string, string, string> onLog,
        CancellationToken ct,
        int? vehicleId = null)
    {
        if (!r2.IsConfigured)
        {
            throw new InvalidOperationException("Cloudflare R2 nao esta configurado. Verifique Storage:R2.");
        }

        var query = db.Veiculos.AsNoTracking();
        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.Id == vehicleId.Value);
        }

        var stats = new SyncStats
        {
            TotalVehicles = await query.CountAsync(ct)
        };
        onProgress(stats.ToProgress(null));
        onLog(null, null, "Preparacao", "Sucesso", $"Total de veiculos selecionados: {stats.TotalVehicles}.");
        if (vehicleId.HasValue && stats.TotalVehicles == 0)
        {
            onLog(vehicleId, vehicleId, "Preparacao", "Erro", $"Veiculo {vehicleId.Value} nao encontrado no banco.");
            return;
        }

        var lastId = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var vehicles = await query
                .Where(x => x.Id > lastId)
                .OrderBy(x => x.Id)
                .Select(x => new VehicleSyncRow(x.Id))
                .Take(PageSize)
                .ToListAsync(ct);

            if (vehicles.Count == 0)
            {
                break;
            }

            var vehicleIds = vehicles.Select(x => x.Id).ToList();
            var mediaByVehicle = (await db.VeiculoMidias
                    .Where(x => vehicleIds.Contains(x.VeiculoId) && x.Tipo == TipoMidia.Imagem)
                    .OrderBy(x => x.VeiculoId)
                    .ThenByDescending(x => x.Ativo)
                    .ThenBy(x => x.Ordem)
                    .ThenBy(x => x.Id)
                    .ToListAsync(ct))
                .GroupBy(x => x.VeiculoId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var vehicle in vehicles)
            {
                ct.ThrowIfCancellationRequested();
                lastId = vehicle.Id;
                mediaByVehicle.TryGetValue(vehicle.Id, out var media);
                await ProcessVehicleAsync(vehicle, media ?? [], stats, onProgress, onLog, ct);
            }

            db.ChangeTracker.Clear();
        }

        onProgress(stats.ToProgress(null));
        logger.LogInformation(
            "Sincronizacao R2 concluida. Total={Total}; Processados={Processed}; Encontrados={Found}; Sincronizados={Synced}; SemImagens={WithoutImages}; ImagensVinculadas={ImagesLinked}; MidiasCriadas={Created}; MidiasAtualizadas={Updated}; Corrigidos={Corrected}; Erros={Errors}",
            stats.TotalVehicles,
            stats.VehiclesProcessed,
            stats.VehiclesFound,
            stats.VehiclesSynchronized,
            stats.VehiclesWithoutImages,
            stats.ImagesLinked,
            stats.MediaCreated,
            stats.MediaUpdated,
            stats.RecordsCorrected,
            stats.Errors);
    }

    private async Task ProcessVehicleAsync(
        VehicleSyncRow vehicle,
        IReadOnlyList<VeiculoMidia> media,
        SyncStats stats,
        Action<R2VehicleImageSyncProgress> onProgress,
        Action<int?, int?, string, string, string> onLog,
        CancellationToken ct)
    {
        try
        {
            onProgress(stats.ToProgress(vehicle.Id));

            var prefix = BuildVehiclePrefix(vehicle.Id);
            var objects = await ListVehicleImagesAsync(prefix, vehicle.Id, vehicle.Id, onLog, ct);
            if (objects.Count == 0)
            {
                stats.VehiclesWithoutImages++;
                onLog(vehicle.Id, vehicle.Id, "R2", "SemImagem", $"Nenhuma imagem encontrada em {prefix}.");
                return;
            }

            stats.VehiclesFound++;
            onLog(vehicle.Id, vehicle.Id, "R2", "Encontrado", $"{objects.Count} imagem(ns) encontrada(s) em {prefix}.");
            onLog(vehicle.Id, vehicle.Id, "R2", "Capa", BuildObjectListMessage(objects));

            var result = ApplyMediaUpdates(vehicle.Id, media, objects);
            await db.SaveChangesAsync(ct);

            stats.VehiclesSynchronized++;
            stats.ImagesLinked += objects.Count;
            stats.MediaCreated += result.Created;
            stats.MediaUpdated += result.Updated;
            stats.RecordsCorrected += result.Changed;
            onLog(vehicle.Id, vehicle.Id, "Banco", "Sucesso", $"Veiculo sincronizado. Imagens vinculadas: {objects.Count}; midias criadas: {result.Created}; midias atualizadas: {result.Updated}; midias desativadas: {result.Deactivated}.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stats.Errors++;
            logger.LogError(ex, "Erro ao sincronizar imagens R2 do veiculo {VehicleId}. PastaR2={R2FolderId}", vehicle.Id, vehicle.Id);
            onLog(vehicle.Id, vehicle.Id, "Veiculo", "Erro", ex.Message);
        }
        finally
        {
            stats.VehiclesProcessed++;
            onProgress(stats.ToProgress(null));
        }
    }

    private async Task<IReadOnlyList<StorageObjectMetadata>> ListVehicleImagesAsync(
        string prefix,
        int vehicleId,
        int r2FolderId,
        Action<int?, int?, string, string, string> onLog,
        CancellationToken ct)
    {
        var objects = new List<StorageObjectMetadata>();
        var ignored = 0;

        await foreach (var item in r2.ListAsync(prefix, ct))
        {
            if (item.Key.EndsWith("/", StringComparison.Ordinal)
                || !item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!ImageExtensions.Contains(Path.GetExtension(item.Key)))
            {
                ignored++;
                continue;
            }

            objects.Add(item);
        }

        if (ignored > 0)
        {
            onLog(vehicleId, r2FolderId, "R2", "Ignorado", $"{ignored} arquivo(s) nao reconhecido(s) como imagem no prefixo.");
        }

        return objects;
    }

    private MediaUpdateResult ApplyMediaUpdates(int vehicleId, IReadOnlyList<VeiculoMidia> media, IReadOnlyList<StorageObjectMetadata> objects)
    {
        var result = new MediaUpdateResult();
        ValidateObjects(objects);
        var slots = media
            .OrderByDescending(x => x.Ativo)
            .ThenBy(x => x.Ordem)
            .ThenBy(x => x.Id)
            .ToList();
        var usedIds = new HashSet<int>();
        var objectKeys = objects
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < objects.Count; index++)
        {
            var item = objects[index];
            var target = ResolveTargetMedia(slots, usedIds, objectKeys, item);
            if (target is null)
            {
                db.VeiculoMidias.Add(CreateMedia(vehicleId, item, index));
                result.Created++;
                result.Changed++;
                continue;
            }

            usedIds.Add(target.Id);
            if (ApplyMediaUpdate(target, item, index))
            {
                result.Updated++;
                result.Changed++;
            }
        }

        foreach (var extra in slots.Where(x => !usedIds.Contains(x.Id)))
        {
            if (DeactivateMedia(extra))
            {
                result.Deactivated++;
                result.Changed++;
            }
        }

        return result;
    }

    private VeiculoMidia CreateMedia(int vehicleId, StorageObjectMetadata item, int index)
    {
        var fileName = StoragePath.GetFileName(item.Key);
        var url = r2.GetPublicUrl(item.Key);
        var media = new VeiculoMidia(vehicleId, fileName, url, TipoMidia.Imagem, index);
        if (index == 0)
        {
            media.DefinirComoCapa();
        }

        media.UpdateStorage(item.Key, item.Container, ResolveContentType(item.Key), item.SizeBytes);
        return media;
    }

    private bool ApplyMediaUpdate(VeiculoMidia media, StorageObjectMetadata item, int index)
    {
        var changed = false;
        var fileName = StoragePath.GetFileName(item.Key);
        var url = r2.GetPublicUrl(item.Key);
        var contentType = ResolveContentType(item.Key);
        var isCover = index == 0;

        if (!media.Ativo)
        {
            media.Ativar();
            changed = true;
        }

        if (!string.Equals(media.Url, url, StringComparison.Ordinal))
        {
            media.UpdateUrl(url);
            changed = true;
        }

        if (!string.Equals(media.NomeArquivo, fileName, StringComparison.Ordinal))
        {
            db.Entry(media).Property(nameof(VeiculoMidia.NomeArquivo)).CurrentValue = fileName;
            changed = true;
        }

        if (!string.Equals(media.BlobName, item.Key, StringComparison.Ordinal))
        {
            changed = true;
        }

        if (!string.Equals(media.Container, item.Container, StringComparison.Ordinal))
        {
            changed = true;
        }

        if (!string.Equals(media.ContentType, contentType, StringComparison.Ordinal))
        {
            changed = true;
        }

        if (media.TamanhoBytes != item.SizeBytes)
        {
            changed = true;
        }

        if (media.Capa != isCover)
        {
            db.Entry(media).Property(nameof(VeiculoMidia.Capa)).CurrentValue = isCover;
            changed = true;
        }

        if (media.Ordem != index)
        {
            db.Entry(media).Property(nameof(VeiculoMidia.Ordem)).CurrentValue = index;
            changed = true;
        }

        media.UpdateStorage(item.Key, item.Container, contentType, item.SizeBytes);
        return changed;
    }

    private bool DeactivateMedia(VeiculoMidia media)
    {
        var changed = false;

        if (media.Capa)
        {
            db.Entry(media).Property(nameof(VeiculoMidia.Capa)).CurrentValue = false;
            changed = true;
        }

        if (media.Ativo)
        {
            media.Desativar();
            changed = true;
        }

        return changed;
    }

    private void ValidateObjects(IReadOnlyList<StorageObjectMetadata> objects)
    {
        foreach (var item in objects)
        {
            ValidateDatabaseLengths(item);
            ValidateUrlLength(r2.GetPublicUrl(item.Key));
        }
    }

    private static void ValidateDatabaseLengths(StorageObjectMetadata item)
    {
        var fileName = StoragePath.GetFileName(item.Key);
        if (fileName.Length > 200)
        {
            throw new InvalidOperationException($"Nome do arquivo excede 200 caracteres: {fileName}.");
        }

        if (item.Key.Length > 250)
        {
            throw new InvalidOperationException($"BlobName excede 250 caracteres: {item.Key}.");
        }
    }

    private static void ValidateUrlLength(string url)
    {
        if (url.Length > 500)
        {
            throw new InvalidOperationException($"URL publica excede 500 caracteres: {url}.");
        }
    }

    private static VeiculoMidia? ResolveTargetMedia(
        IReadOnlyList<VeiculoMidia> media,
        ISet<int> usedIds,
        ISet<string> objectKeys,
        StorageObjectMetadata item)
    {
        var byBlobName = media.FirstOrDefault(x =>
            !usedIds.Contains(x.Id)
            && string.Equals(NormalizeStorageKey(x.BlobName), item.Key, StringComparison.OrdinalIgnoreCase));

        if (byBlobName is not null)
        {
            return byBlobName;
        }

        return media.FirstOrDefault(x =>
            !usedIds.Contains(x.Id)
            && !objectKeys.Contains(NormalizeStorageKey(x.BlobName)));
    }

    private static string BuildVehiclePrefix(int vehicleId)
        => StoragePath.Combine(
            StoragePath.LegacyImportedVehiclePrefix,
            vehicleId.ToString(CultureInfo.InvariantCulture)) + "/";

    private static string NormalizeStorageKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            return StoragePath.NormalizeKey(value);
        }
        catch (ArgumentException)
        {
            return value.Trim().Replace('\\', '/');
        }
    }

    private static string? ResolveContentType(string key)
        => ContentTypeProvider.TryGetContentType(key, out var contentType) ? contentType : null;

    private static string BuildObjectListMessage(IReadOnlyList<StorageObjectMetadata> objects)
    {
        var names = objects.Select(x => StoragePath.GetFileName(x.Key)).ToList();
        var listed = string.Join(", ", names.Take(12));
        if (names.Count > 12)
        {
            listed += $", ... (+{names.Count - 12})";
        }

        return $"Imagem principal: {names[0]}. Arquivos: {listed}.";
    }

    private sealed class SyncStats
    {
        public int TotalVehicles { get; set; }
        public int VehiclesProcessed { get; set; }
        public int VehiclesFound { get; set; }
        public int VehiclesWithoutImages { get; set; }
        public int VehiclesSynchronized { get; set; }
        public int ImagesLinked { get; set; }
        public int MediaCreated { get; set; }
        public int MediaUpdated { get; set; }
        public int RecordsCorrected { get; set; }
        public int Errors { get; set; }

        public R2VehicleImageSyncProgress ToProgress(int? currentVehicleId)
            => new(
                TotalVehicles,
                VehiclesProcessed,
                VehiclesFound,
                VehiclesWithoutImages,
                VehiclesSynchronized,
                ImagesLinked,
                MediaCreated,
                MediaUpdated,
                RecordsCorrected,
                Errors,
                currentVehicleId);
    }

    private sealed class MediaUpdateResult
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Deactivated { get; set; }
        public int Changed { get; set; }
    }

    private sealed record VehicleSyncRow(int Id);
}
