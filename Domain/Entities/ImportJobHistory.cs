using Domain.Common;

namespace Domain.Entities;

public sealed class ImportJobHistory : BaseEntity
{
    public int ImportJobId { get; private set; }
    public string Tipo { get; private set; } = null!;
    public string? UsuarioId { get; private set; }
    public string? UsuarioNome { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public int? Quantidade { get; private set; }
    public long? DuracaoMs { get; private set; }
    public string? Resultado { get; private set; }
    public string? Mensagem { get; private set; }

    public ImportJob ImportJob { get; private set; } = null!;

    private ImportJobHistory() { }

    public ImportJobHistory(
        int importJobId,
        string tipo,
        string? usuarioId,
        string? usuarioNome,
        int? quantidade,
        TimeSpan? duracao,
        string? resultado,
        string? mensagem)
    {
        ImportJobId = importJobId;
        Tipo = string.IsNullOrWhiteSpace(tipo) ? throw new ArgumentException("Tipo obrigatorio.") : tipo.Trim();
        UsuarioId = string.IsNullOrWhiteSpace(usuarioId) ? null : usuarioId.Trim();
        UsuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? null : usuarioNome.Trim();
        CriadoEm = DateTime.UtcNow;
        Quantidade = quantidade;
        DuracaoMs = duracao.HasValue ? Math.Max(0, (long)duracao.Value.TotalMilliseconds) : null;
        Resultado = string.IsNullOrWhiteSpace(resultado) ? null : resultado.Trim();
        Mensagem = string.IsNullOrWhiteSpace(mensagem) ? null : mensagem.Trim();
    }
}
