namespace Domain.ValueObjects;

public readonly record struct Telefone
{
    public string Valor { get; }

    public Telefone(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException("Telefone invalido.");
        Valor = new string(valor.Where(char.IsDigit).ToArray());
    }

    public override string ToString() => Valor;
}
