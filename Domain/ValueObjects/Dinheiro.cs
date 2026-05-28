namespace Domain.ValueObjects;

public readonly record struct Dinheiro
{
    public decimal Valor { get; }

    public Dinheiro(decimal valor)
    {
        if (valor < 0) throw new ArgumentException("Valor monetario nao pode ser negativo.");
        Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    public static Dinheiro Zero => new(0m);
    public override string ToString() => Valor.ToString("F2");
}
