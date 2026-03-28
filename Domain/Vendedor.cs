using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain;

[Table("Vendedor")]
public class Vendedor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int LojaId { get; set; }
    public Loja? Loja { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(20)]
    public string? Whatsapp { get; set; }

    [MaxLength(20)]
    public string? Cpf { get; set; }

    [MaxLength(255)]
    public string? FotoUrl { get; set; }

    [MaxLength(100)]
    public string? Cargo { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCadastro { get; set; }
}