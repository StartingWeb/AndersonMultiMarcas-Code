using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Veiculo : AuditableEntity
{
    public int LojaId { get; private set; }
    public int MarcaId { get; private set; }
    public int? VendedorId { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Modelo { get; private set; } = null!;
    public string? Versao { get; private set; }
    public int? AnoFabricacao { get; private set; }
    public int AnoModelo { get; private set; }
    public string? Cor { get; private set; }
    public Combustivel Combustivel { get; private set; }
    public Cambio Cambio { get; private set; }
    public int? Quilometragem { get; private set; }
    public string? Placa { get; private set; }
    public Dinheiro PrecoVenda { get; private set; }
    public bool AceitaTroca { get; private set; }
    public bool Financiavel { get; private set; }
    public bool Destaque { get; private set; }
    public bool Seminovo { get; private set; }
    public bool Vendido { get; private set; }
    public DateTime? DataVenda { get; private set; }
    public string? Descricao { get; private set; }
    public string? UrlVideo { get; private set; }
    public string? ObservacoesInternas { get; private set; }
    public int? IdLegado { get; private set; }
    public bool ImportadoMidia { get; private set; }
    public bool MotoEletrica { get; private set; }
    public int QuantidadeCliques { get; private set; }
    public int QuantidadeVisualizacoes { get; private set; }

    public string NomeCompleto => string.IsNullOrWhiteSpace(Versao) ? $"{Titulo} {Modelo}" : $"{Titulo} {Modelo} {Versao}";
    public bool EstaDisponivel => Ativo && !Vendido;

    public Loja Loja { get; private set; } = null!;
    public Marca Marca { get; private set; } = null!;
    public Vendedor? Vendedor { get; private set; }
    public VeiculoCaracteristica? Caracteristicas { get; private set; }
    public ICollection<VeiculoMidia> Midias { get; private set; } = new List<VeiculoMidia>();

    private Veiculo() { }

    public Veiculo(int lojaId, int marcaId, string titulo, string modelo, int anoModelo, Dinheiro precoVenda)
    {
        LojaId = lojaId;
        MarcaId = marcaId;
        Titulo = string.IsNullOrWhiteSpace(titulo) ? throw new ArgumentException("Titulo obrigatorio.") : titulo.Trim();
        Modelo = string.IsNullOrWhiteSpace(modelo) ? throw new ArgumentException("Modelo obrigatorio.") : modelo.Trim();
        AnoModelo = anoModelo;
        PrecoVenda = precoVenda;
    }

    public void Update(string titulo, string modelo, string? versao, int? anoFabricacao, int anoModelo, Combustivel combustivel, Cambio cambio, int? quilometragem, string? placa, string? cor, string? descricao)
    {
        Titulo = string.IsNullOrWhiteSpace(titulo) ? throw new ArgumentException("Titulo obrigatorio.") : titulo.Trim();
        Modelo = string.IsNullOrWhiteSpace(modelo) ? throw new ArgumentException("Modelo obrigatorio.") : modelo.Trim();
        Versao = string.IsNullOrWhiteSpace(versao) ? null : versao.Trim();
        AnoFabricacao = anoFabricacao;
        AnoModelo = anoModelo;
        Combustivel = combustivel;
        Cambio = cambio;
        Quilometragem = quilometragem;
        Placa = string.IsNullOrWhiteSpace(placa) ? null : placa.Trim().ToUpperInvariant();
        Cor = string.IsNullOrWhiteSpace(cor) ? null : cor.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        MarcarAtualizacao();
    }

    public void AtualizarVinculos(int lojaId, int marcaId)
    {
        if (lojaId <= 0) throw new ArgumentException("Loja obrigatoria.", nameof(lojaId));
        if (marcaId <= 0) throw new ArgumentException("Marca obrigatoria.", nameof(marcaId));

        LojaId = lojaId;
        MarcaId = marcaId;
        MarcarAtualizacao();
    }

    public void AtualizarComercial(
        bool aceitaTroca,
        bool financiavel,
        bool destaque,
        bool seminovo,
        string? urlVideo,
        string? observacoesInternas,
        int? vendedorId)
    {
        AceitaTroca = aceitaTroca;
        Financiavel = financiavel;
        Destaque = destaque;
        Seminovo = seminovo;
        UrlVideo = string.IsNullOrWhiteSpace(urlVideo) ? null : urlVideo.Trim();
        ObservacoesInternas = string.IsNullOrWhiteSpace(observacoesInternas) ? null : observacoesInternas.Trim();
        VendedorId = vendedorId;
        MarcarAtualizacao();
    }

    public void MarcarComoVendido(DateTime dataVenda)
    {
        Vendido = true;
        DataVenda = dataVenda;
        Desativar();
    }

    public void MarcarComoVendido(DateTime dataVenda, int vendedorId)
    {
        VendedorId = vendedorId;
        MarcarComoVendido(dataVenda);
    }

    public void AtualizarPreco(Dinheiro novoPreco)
    {
        PrecoVenda = novoPreco;
        MarcarAtualizacao();
    }

    public void RegistrarVisualizacao()
    {
        QuantidadeVisualizacoes++;
        MarcarAtualizacao();
    }

    public void RegistrarClique()
    {
        QuantidadeCliques++;
        MarcarAtualizacao();
    }

    public void DefinirDestaque() => Destaque = true;
    public void RemoverDestaque() => Destaque = false;
    public override void Ativar() => base.Ativar();
    public override void Desativar() => base.Desativar();
}
