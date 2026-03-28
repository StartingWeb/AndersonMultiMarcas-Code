using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain;

[Table("VeiculoMidia")]
public class VeiculoMidia
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VeiculoId { get; set; }
    public Veiculo? Veiculo { get; set; }

    [Required]
    [MaxLength(255)]
    public string NomeArquivo { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? BlobName { get; set; }

    [MaxLength(100)]
    public string? Container { get; set; }

    [MaxLength(20)]
    public string? Tipo { get; set; } // imagem, video

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long? TamanhoBytes { get; set; }

    public bool Capa { get; set; }

    public int Ordem { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCadastro { get; set; }
}