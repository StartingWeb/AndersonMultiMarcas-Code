using Domain.Common;

namespace Domain.Entities;

public sealed class ImportJob : BaseEntity
{
    public string Status { get; private set; } = "Pendente";
    public DateTime CriadoEm { get; private set; }
    public DateTime? IniciadoEm { get; private set; }
    public DateTime? FinalizadoEm { get; private set; }
    public DateTime? CanceladoEm { get; private set; }
    public string? UsuarioId { get; private set; }
    public string? UsuarioNome { get; private set; }
    public string UrlBase { get; private set; } = null!;
    public bool DryRun { get; private set; }
    public bool SomenteSemBlobName { get; private set; }
    public bool Sobrescrever { get; private set; }
    public bool PreparacaoConcluida { get; private set; }
    public int? IdInicial { get; private set; }
    public int? QuantidadeMaxima { get; private set; }
    public int TotalVeiculos { get; private set; }
    public int VeiculosProcessados { get; private set; }
    public int TotalImagens { get; private set; }
    public int ImagensImportadas { get; private set; }
    public int ImagensIgnoradas { get; private set; }
    public int ImagensComErro { get; private set; }
    public string? UltimaMensagem { get; private set; }
    public DateTime? UltimaAtualizacaoEm { get; private set; }
    public int? VeiculoAtualId { get; private set; }
    public string? RelatorioConsolidadoJson { get; private set; }
    public DateTime? RelatorioGeradoEm { get; private set; }
    public string? LockId { get; private set; }
    public DateTime? LockExpiraEm { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public ICollection<ImportJobItem> Items { get; private set; } = [];
    public ICollection<ImportJobLog> Logs { get; private set; } = [];
    public ICollection<ImportJobHistory> Historico { get; private set; } = [];

    private ImportJob() { }

    public ImportJob(
        string urlBase,
        bool dryRun,
        bool somenteSemBlobName,
        bool sobrescrever,
        int? idInicial,
        int? quantidadeMaxima,
        string? usuarioId,
        string? usuarioNome)
    {
        CriadoEm = DateTime.UtcNow;
        UrlBase = string.IsNullOrWhiteSpace(urlBase) ? throw new ArgumentException("URL base obrigatoria.") : urlBase.TrimEnd('/');
        DryRun = dryRun;
        SomenteSemBlobName = somenteSemBlobName;
        Sobrescrever = sobrescrever;
        IdInicial = idInicial;
        QuantidadeMaxima = quantidadeMaxima;
        UsuarioId = string.IsNullOrWhiteSpace(usuarioId) ? null : usuarioId.Trim();
        UsuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? null : usuarioNome.Trim();
        Touch("Job criado.");
    }

    public void MarkQueued(string? message = null)
    {
        Status = "Pendente";
        LockId = null;
        LockExpiraEm = null;
        FinalizadoEm = null;
        CanceladoEm = null;
        Touch(message ?? "Job pendente.");
    }

    public void MarkRunning(string lockId, DateTime lockExpiresAt)
    {
        Status = "EmExecucao";
        IniciadoEm ??= DateTime.UtcNow;
        LockId = lockId;
        LockExpiraEm = lockExpiresAt;
        Touch("Job em execucao.");
    }

    public void RefreshLock(DateTime lockExpiresAt)
    {
        LockExpiraEm = lockExpiresAt;
        UltimaAtualizacaoEm = DateTime.UtcNow;
    }

    public void RequestCancellation()
    {
        Status = "CancelamentoSolicitado";
        CanceladoEm = DateTime.UtcNow;
        Touch("Cancelamento solicitado.");
    }

    public void MarkCancelled()
    {
        Status = "Cancelado";
        FinalizadoEm = DateTime.UtcNow;
        CanceladoEm ??= DateTime.UtcNow;
        LockId = null;
        LockExpiraEm = null;
        VeiculoAtualId = null;
        Touch("Job cancelado.");
    }

    public void MarkCompleted()
    {
        Status = ImagensComErro > 0 ? "ConcluidoComFalhas" : "Concluido";
        FinalizadoEm = DateTime.UtcNow;
        LockId = null;
        LockExpiraEm = null;
        VeiculoAtualId = null;
        Touch("Job finalizado.");
    }

    public void MarkFailed(string message)
    {
        Status = "Falha";
        FinalizadoEm = DateTime.UtcNow;
        LockId = null;
        LockExpiraEm = null;
        VeiculoAtualId = null;
        Touch(message);
    }

    public void SetCurrentVehicle(int? vehicleId)
    {
        VeiculoAtualId = vehicleId;
        UltimaAtualizacaoEm = DateTime.UtcNow;
    }

    public void SetTotals(int totalVehicles, int totalImages)
    {
        TotalVeiculos = Math.Max(0, totalVehicles);
        TotalImagens = Math.Max(0, totalImages);
        UltimaAtualizacaoEm = DateTime.UtcNow;
    }

    public void MarkPreparationCompleted(int totalImages)
    {
        PreparacaoConcluida = true;
        TotalImagens = Math.Max(0, totalImages);
        Touch("Preparacao concluida.");
    }

    public void UpdateProgress(int vehiclesProcessed, int importedImages, int skippedImages, int failedImages)
    {
        VeiculosProcessados = Math.Max(0, vehiclesProcessed);
        ImagensImportadas = Math.Max(0, importedImages);
        ImagensIgnoradas = Math.Max(0, skippedImages);
        ImagensComErro = Math.Max(0, failedImages);
        UltimaAtualizacaoEm = DateTime.UtcNow;
    }

    public void SetConsolidatedReport(string json)
    {
        RelatorioConsolidadoJson = string.IsNullOrWhiteSpace(json) ? null : json;
        RelatorioGeradoEm = RelatorioConsolidadoJson is null ? null : DateTime.UtcNow;
        UltimaAtualizacaoEm = DateTime.UtcNow;
    }

    public void Touch(string? message)
    {
        UltimaMensagem = string.IsNullOrWhiteSpace(message) ? UltimaMensagem : message.Trim();
        UltimaAtualizacaoEm = DateTime.UtcNow;
    }
}
