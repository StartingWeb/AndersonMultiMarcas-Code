using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain;

[Table("Marca")]
public class Marca
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(255)]
    public string LogoUrl { get; set; } = string.Empty;

    public bool Ativo { get; set; }

    public DateTime DataCadastro { get; set; }
}