using System.Globalization;
using System.Text.RegularExpressions;
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

            var vehicleIds = await db.Veiculos
                .AsNoTracking()
                .Where(x => x.Id > lastId)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(PageSize)
                .ToListAsync(ct);

            if (vehicleIds.Count == 0)
            {
                break;
            }

            foreach (var vehicleId in vehicleIds)
            {
                ct.ThrowIfCancellationRequested();
                lastId = vehicleId;
                await ProcessVehicleAsync(vehicleId, stats, onProgress, onLog, ct);
            }
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
        int vehicleId,
        SyncStats stats,
        Action<R2VehicleImageSyncProgress> onProgress,
        Action<int?, int?, string, string, string> onLog,
        CancellationToken ct)
    {
        int? legacyId = null;

        try
        {
            var vehicle = await db.Veiculos
                .Include(x => x.Midias)
                .FirstAsync(x => x.Id == vehicleId, ct);
            legacyId = vehicle.IdLegado;

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

            var corrected = ApplyMediaUpdates(vehicle, objects);
            await db.SaveChangesAsync(ct);

            stats.VehiclesSynchronized++;
            stats.RecordsCorrected += corrected;
            onLog(vehicle.Id, vehicle.IdLegado, "Banco", "Sucesso", $"Veiculo sincronizado. Registros corrigidos: {corrected}.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stats.Errors++;
            logger.LogError(ex, "Erro ao sincronizar imagens R2 do veiculo {VehicleId}. IdLegado={LegacyVehicleId}", vehicleId, legacyId);
            onLog(vehicleId, legacyId, "Veiculo", "Erro", ex.Message);
        }
        finally
        {
            stats.VehiclesProcessed++;
            db.ChangeTracker.Clear();
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

        return objects
            .OrderBy(x => NaturalSortKey(StoragePath.GetFileName(x.Key)), StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private int ApplyMediaUpdates(Veiculo vehicle, IReadOnlyList<StorageObjectMetadata> objects)
    {
        var corrected = 0;
        var activeImages = vehicle.Midias
            .Where(x => x.Ativo && x.Tipo == TipoMidia.Imagem)
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Id)
            .ToList();
        var allImages = vehicle.Midias
            .Where(x => x.Tipo == TipoMidia.Imagem)
            .OrderByDescending(x => x.Ativo)
            .ThenBy(x => x.Ordem)
            .ThenBy(x => x.Id)
            .ToList();
        var assigned = new HashSet<int>();

        for (var index = 0; index < objects.Count; index++)
        {
            var item = objects[index];
            var target = FindTargetMedia(allImages, activeImages, assigned, item);
            if (target is null)
            {
                db.VeiculoMidias.Add(CreateMedia(vehicle.Id, item, index));
                corrected++;
                continue;
            }

            assigned.Add(target.Id);
            if (ApplyMediaUpdate(target, item, index))
            {
                corrected++;
            }
        }

        foreach (var media in activeImages.Where(x => !assigned.Contains(x.Id)))
        {
            media.Desativar();
            corrected++;
        }

        return corrected;
    }

    private VeiculoMidia CreateMedia(int vehicleId, StorageObjectMetadata item, int index)
    {
        ValidateDatabaseLengths(item);

        var fileName = StoragePath.GetFileName(item.Key);
        var url = r2.GetPublicUrl(item.Key);
        ValidateUrlLength(url);
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
        ValidateDatabaseLengths(item);

        var changed = false;
        var fileName = StoragePath.GetFileName(item.Key);
        var url = r2.GetPublicUrl(item.Key);
        var contentType = ResolveContentType(item.Key);
        var isCover = index == 0;
        ValidateUrlLength(url);

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

    private static VeiculoMidia? FindTargetMedia(
        IReadOnlyList<VeiculoMidia> allImages,
        IReadOnlyList<VeiculoMidia> activeImages,
        ISet<int> assigned,
        StorageObjectMetadata item)
    {
        var key = item.Key;
        var fileName = StoragePath.GetFileName(key);

        var byBlob = allImages.FirstOrDefault(x =>
            x.Id > 0
            && !assigned.Contains(x.Id)
            && string.Equals(NormalizeStorageKey(x.BlobName), key, StringComparison.OrdinalIgnoreCase));
        if (byBlob is not null)
        {
            return byBlob;
        }

        var byFile = allImages.FirstOrDefault(x =>
            x.Id > 0
            && !assigned.Contains(x.Id)
            && (string.Equals(NormalizeFileName(x.NomeArquivo), fileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeFileNameFromSource(x.Url), fileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeFileNameFromSource(x.BlobName), fileName, StringComparison.OrdinalIgnoreCase)));
        if (byFile is not null)
        {
            return byFile;
        }

        return activeImages.FirstOrDefault(x => x.Id > 0 && !assigned.Contains(x.Id));
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

    private static string NaturalSortKey(string value)
        => NumberRegex().Replace(value, match => match.Value.PadLeft(12, '0'));

    private static string NormalizeStorageKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Replace('\\', '/');

    private static string NormalizeFileName(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeFileNameFromSource(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return NormalizeFileName(Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath)));
        }

        return NormalizeFileName(Path.GetFileName(value.Replace('\\', '/')));
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    private sealed class SyncStats
    {
        public int TotalVehicles { get; set; }
        public int VehiclesProcessed { get; set; }
        public int VehiclesFound { get; set; }
        public int VehiclesWithoutImages { get; set; }
        public int VehiclesSynchronized { get; set; }
        public int RecordsCorrected { get; set; }
        public int Errors { get; set; }

        public R2VehicleImageSyncProgress ToProgress(int? currentVehicleId)
            => new(
                TotalVehicles,
                VehiclesProcessed,
                VehiclesFound,
                VehiclesWithoutImages,
                VehiclesSynchronized,
                RecordsCorrected,
                Errors,
                currentVehicleId);
    }
}
