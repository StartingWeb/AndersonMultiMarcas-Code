using System.ComponentModel.DataAnnotations;

namespace Core.Dtos;

public class LojaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? Cnpj { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCadastro { get; set; }
}
