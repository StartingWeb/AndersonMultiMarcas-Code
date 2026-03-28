namespace Core.Dtos;

public class VendedorDto
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Whatsapp { get; set; }
    public string? Cpf { get; set; }
    public string? FotoUrl { get; set; }
    public string? Cargo { get; set; }
    public bool Ativo { get; set; }
    public int LojaId { get; set; }
    public string? LojaNome { get; set; }
    public DateTime? DataCadastro { get; set; }
}
