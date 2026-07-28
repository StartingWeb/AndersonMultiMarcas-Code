using Domain.Common;

namespace Domain.Entities;

public sealed class ImportJobLog : BaseEntity
{
    public int ImportJobId { get; private set; }
    public int? ImportJobItemId { get; private set; }
    public int? VeiculoId { get; private set; }
    public int? ImagemOrdem { get; private set; }
    public string? UrlLegada { get; private set; }
    public string Etapa { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public string Mensagem { get; private set; } = null!;
    public DateTime CriadoEm { get; private set; }

    public ImportJob ImportJob { get; private set; } = null!;
    public ImportJobItem? ImportJobItem { get; private set; }

    private ImportJobLog() { }

    public ImportJobLog(
        int importJobId,
        int? importJobItemId,
        int? veiculoId,
        int? imagemOrdem,
        string? urlLegada,
        string etapa,
        string status,
        string mensagem)
    {
        ImportJobId = importJobId;
        ImportJobItemId = importJobItemId;
        VeiculoId = veiculoId;
        ImagemOrdem = imagemOrdem;
        UrlLegada = string.IsNullOrWhiteSpace(urlLegada) ? null : urlLegada.Trim();
        Etapa = string.IsNullOrWhiteSpace(etapa) ? throw new ArgumentException("Etapa obrigatoria.") : etapa.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? throw new ArgumentException("Status obrigatorio.") : status.Trim();
        Mensagem = string.IsNullOrWhiteSpace(mensagem) ? throw new ArgumentException("Mensagem obrigatoria.") : mensagem.Trim();
        CriadoEm = DateTime.UtcNow;
    }
}
