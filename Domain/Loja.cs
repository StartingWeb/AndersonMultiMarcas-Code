using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain;

[Table("Loja")]
public class Loja
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RazaoSocial { get; set; }

    [MaxLength(20)]
    public string? Cnpj { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(200)]
    public string? Endereco { get; set; }

    [MaxLength(20)]
    public string? Numero { get; set; }

    [MaxLength(100)]
    public string? Complemento { get; set; }

    [MaxLength(100)]
    public string? Bairro { get; set; }

    [MaxLength(100)]
    public string? Cidade { get; set; }

    [MaxLength(2)]
    public string? Uf { get; set; }

    [MaxLength(10)]
    public string? Cep { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCadastro { get; set; }
}