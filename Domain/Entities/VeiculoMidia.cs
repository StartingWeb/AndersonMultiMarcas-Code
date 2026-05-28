using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class VeiculoMidia : BaseEntity
{
    public int VeiculoId { get; private set; }
    public string NomeArquivo { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public string? BlobName { get; private set; }
    public string? Container { get; private set; }
    public TipoMidia Tipo { get; private set; }
    public string? ContentType { get; private set; }
    public long? TamanhoBytes { get; private set; }
    public bool Capa { get; private set; }
    public int Ordem { get; private set; }

    public Veiculo Veiculo { get; private set; } = null!;

    private VeiculoMidia() { }

    public VeiculoMidia(int veiculoId, string nomeArquivo, string url, TipoMidia tipo, int ordem)
    {
        VeiculoId = veiculoId;
        NomeArquivo = string.IsNullOrWhiteSpace(nomeArquivo) ? throw new ArgumentException("Nome do arquivo obrigatorio.") : nomeArquivo.Trim();
        Url = string.IsNullOrWhiteSpace(url) ? throw new ArgumentException("Url obrigatoria.") : url.Trim();
        Tipo = tipo;
        Ordem = ordem;
    }

    public void DefinirComoCapa() => Capa = true;

    public void UpdateStorage(string? blobName, string? container, string? contentType, long? tamanhoBytes)
    {
        BlobName = string.IsNullOrWhiteSpace(blobName) ? null : blobName.Trim();
        Container = string.IsNullOrWhiteSpace(container) ? null : container.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        TamanhoBytes = tamanhoBytes;
    }
}
