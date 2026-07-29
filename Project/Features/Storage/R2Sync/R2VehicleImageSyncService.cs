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
        CancellationToken ct)
    {
        if (!r2.IsConfigured)
        {
            throw new InvalidOperationException("Cloudflare R2 nao esta configurado. Verifique Storage:R2.");
        }

        var stats = new SyncStats
        {
            TotalVehicles = await db.Veiculos.AsNoTracking().CountAsync(ct)
        };
        onProgress(stats.ToProgress(null));
        onLog(null, null, "Preparacao", "Sucesso", $"Total de veiculos selecionados: {stats.TotalVehicles}.");

        var lastId = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var vehicles = await db.Veiculos
                .AsNoTracking()
                .Where(x => x.Id > lastId)
                .OrderBy(x => x.Id)
                .Select(x => new VehicleSyncRow(x.Id, x.IdLegado))
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
            "Sincronizacao R2 concluida. Total={Total}; Processados={Processed}; Encontrados={Found}; Sincronizados={Synced}; SemImagens={WithoutImages}; Corrigidos={Corrected}; Erros={Errors}",
            stats.TotalVehicles,
            stats.VehiclesProcessed,
            stats.VehiclesFound,
            stats.VehiclesSynchronized,
            stats.VehiclesWithoutImages,
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

            if (!vehicle.IdLegado.HasValue)
            {
                stats.VehiclesWithoutImages++;
                onLog(vehicle.Id, null, "Veiculo", "SemImagem", "Veiculo sem IdLegado; banco nao alterado.");
                return;
            }

            var prefix = BuildVehiclePrefix(vehicle.IdLegado.Value);
            var objects = await ListVehicleImagesAsync(prefix, vehicle.Id, vehicle.IdLegado.Value, onLog, ct);
            if (objects.Count == 0)
            {
                stats.VehiclesWithoutImages++;
                onLog(vehicle.Id, vehicle.IdLegado, "R2", "SemImagem", $"Nenhuma imagem encontrada em {prefix}.");
                return;
            }

            stats.VehiclesFound++;
            onLog(vehicle.Id, vehicle.IdLegado, "R2", "Encontrado", $"{objects.Count} imagem(ns) encontrada(s) em {prefix}.");
            onLog(vehicle.Id, vehicle.IdLegado, "R2", "Capa", BuildObjectListMessage(objects));

            var corrected = ApplyMediaUpdates(vehicle.Id, media, objects);
            await db.SaveChangesAsync(ct);

            stats.VehiclesSynchronized++;
            stats.ImagesLinked += objects.Count;
            stats.RecordsCorrected += corrected;
            onLog(vehicle.Id, vehicle.IdLegado, "Banco", "Sucesso", $"Veiculo sincronizado. Imagens vinculadas: {objects.Count}; registros corrigidos: {corrected}.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stats.Errors++;
            logger.LogError(ex, "Erro ao sincronizar imagens R2 do veiculo {VehicleId}. IdLegado={LegacyVehicleId}", vehicle.Id, vehicle.IdLegado);
            onLog(vehicle.Id, vehicle.IdLegado, "Veiculo", "Erro", ex.Message);
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
        int legacyVehicleId,
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
            onLog(vehicleId, legacyVehicleId, "R2", "Ignorado", $"{ignored} arquivo(s) nao reconhecido(s) como imagem no prefixo.");
        }

        return objects;
    }

    private int ApplyMediaUpdates(int vehicleId, IReadOnlyList<VeiculoMidia> media, IReadOnlyList<StorageObjectMetadata> objects)
    {
        var corrected = 0;
        ValidateObjects(objects);
        var slots = media
            .OrderByDescending(x => x.Ativo)
            .ThenBy(x => x.Ordem)
            .ThenBy(x => x.Id)
            .ToList();

        for (var index = 0; index < objects.Count; index++)
        {
            var item = objects[index];
            var target = index < slots.Count ? slots[index] : null;
            if (target is null)
            {
                db.VeiculoMidias.Add(CreateMedia(vehicleId, item, index));
                corrected++;
                continue;
            }

            if (ApplyMediaUpdate(target, item, index))
            {
                corrected++;
            }
        }

        foreach (var extra in slots.Skip(objects.Count))
        {
            if (DeactivateMedia(extra))
            {
                corrected++;
            }
        }

        return corrected;
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

    private static string BuildVehiclePrefix(int legacyVehicleId)
        => StoragePath.Combine(
            StoragePath.LegacyImportedVehiclePrefix,
            legacyVehicleId.ToString(CultureInfo.InvariantCulture)) + "/";

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
                RecordsCorrected,
                Errors,
                currentVehicleId);
    }

    private sealed record VehicleSyncRow(int Id, int? IdLegado);
}
