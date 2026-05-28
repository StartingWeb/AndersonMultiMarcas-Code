using Domain.Enums;

namespace Domain.ValueObjects;

public sealed record Endereco(
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    Uf Uf,
    string Cep);
