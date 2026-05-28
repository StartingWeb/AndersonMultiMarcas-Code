using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Vendedor : BaseEntity
{
    public int LojaId { get; private set; }
    public string Nome { get; private set; } = null!;
    public Email? Email { get; private set; }
    public Telefone? Telefone { get; private set; }
    public Telefone? Whatsapp { get; private set; }
    public Documento? Cpf { get; private set; }
    public string? FotoUrl { get; private set; }
    public string? Cargo { get; private set; }

    public Loja Loja { get; private set; } = null!;
    public ICollection<Veiculo> Veiculos { get; private set; } = new List<Veiculo>();

    private Vendedor() { }

    public Vendedor(int lojaId, string nome)
    {
        LojaId = lojaId;
        Nome = string.IsNullOrWhiteSpace(nome) ? throw new ArgumentException("Nome obrigatorio.") : nome.Trim();
    }

    public void Update(string nome, Email? email, Telefone? telefone, Telefone? whatsapp, Documento? cpf, string? fotoUrl, string? cargo)
    {
        Nome = string.IsNullOrWhiteSpace(nome) ? throw new ArgumentException("Nome obrigatorio.") : nome.Trim();
        Email = email;
        Telefone = telefone;
        Whatsapp = whatsapp;
        Cpf = cpf;
        FotoUrl = string.IsNullOrWhiteSpace(fotoUrl) ? null : fotoUrl.Trim();
        Cargo = string.IsNullOrWhiteSpace(cargo) ? null : cargo.Trim();
    }
}
