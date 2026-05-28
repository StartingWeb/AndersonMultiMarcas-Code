namespace Domain.ValueObjects;

public readonly record struct Email
{
    public string Valor { get; }

    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !valor.Contains('@')) throw new ArgumentException("E-mail invalido.");
        Valor = valor.Trim().ToLowerInvariant();
    }

    public override string ToString() => Valor;
}
