using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain;

[Table("Veiculo")]
public class Veiculo
{
    [Key]
    public int Id { get; set; }

    public int? IdLegado { get; set; }

    [Required]
    public int LojaId { get; set; }
    public Loja? Loja { get; set; }

    [Required]
    [MaxLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    public int MarcaId { get; set; }
    public Marca? Marca { get; set; }

    public int? VendedorId { get; set; }
    public Vendedor? Vendedor { get; set; }

    // =========================
    // IDENTIFICAÇÃO
    // =========================
    [MaxLength(100)]
    public string? Modelo { get; set; }

    [MaxLength(100)]
    public string? Versao { get; set; }

    public int? AnoFabricacao { get; set; }
    public int? AnoModelo { get; set; }

    [MaxLength(30)]
    public string? Cor { get; set; }

    [MaxLength(30)]
    public string? Combustivel { get; set; }

    [MaxLength(30)]
    public string? Cambio { get; set; }

    public int? Quilometragem { get; set; }

    // =========================
    // DOCUMENTAÇÃO
    // =========================
    [MaxLength(20)]
    public string? Placa { get; set; }

    // =========================
    // COMERCIAL
    // =========================
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecoVenda { get; set; }

    public bool AceitaTroca { get; set; }
    public bool Financiavel { get; set; }
    public bool Destaque { get; set; }
    public bool Seminovo { get; set; }
    public bool MotoEletrica { get; set; }
    public bool ImportadoMidia { get; set; }

    // =========================
    // STATUS
    // =========================
    public bool Ativo { get; set; }
    public bool Vendido { get; set; }
    public DateTime? DataVenda { get; set; }
    public Guid? VendidoPorUsuarioId { get; set; }
    public Domain.Application.AspNetCoreUser? VendidoPorUsuario { get; set; }

    public DateTime DataCadastro { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // =========================
    // CONTEÚDO / MÍDIA
    // =========================
    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [MaxLength(255)]
    public string? UrlVideo { get; set; }

    [MaxLength(255)]
    public string? ObservacoesInternas { get; set; }
    public VeiculoCaracteristica? Caracteristica { get; set; }
    public ICollection<VeiculoMidia> Midias { get; set; } = new List<VeiculoMidia>();
}
