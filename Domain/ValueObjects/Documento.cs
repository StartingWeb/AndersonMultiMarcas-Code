namespace Domain.ValueObjects;

public readonly record struct Documento
{
    public string Valor { get; }

    public Documento(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException("Documento invalido.");
        Valor = SomenteDigitos(valor);
    }

    public static string SomenteDigitos(string valor) => new(valor.Where(char.IsDigit).ToArray());
    public override string ToString() => Valor;
}
