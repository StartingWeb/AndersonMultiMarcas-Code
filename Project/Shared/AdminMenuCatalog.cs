namespace Project.Shared;

public static class AdminMenuCatalog
{
    public const string ClaimType = "AdminMenu";

    public static IReadOnlyList<AdminMenuSection> Sections { get; } =
    [
        new("Principal",
        [
            new("dashboard", "Dashboard", "/Admin", "bi bi-speedometer2", true),
            new("veiculos", "Veiculos", "/Veiculo", "bi bi-car-front-fill"),
            new("vendas-veiculos", "Venda de Veiculos", "/Admin/VeiculosVenda", "bi bi-clipboard-check-fill")
        ]),
        new("Cadastros",
        [
            new("lojas", "Lojas", "/Admin/Cadastros/Lojas", "bi bi-shop"),
            new("marcas", "Marcas", "/Admin/Cadastros/Marcas", "bi bi-tags-fill"),
            new("vendedores", "Vendedores", "/Admin/Cadastros/Vendedores", "bi bi-person-badge-fill")
        ]),
        new("Auth",
        [
            new("usuarios", "Usuarios", "/Admin/Auth/Usuarios", "bi bi-people-fill", DeveloperOnly: true),
            new("perfil", "Perfil", "/Admin/Auth/Perfil", "bi bi-person-lines-fill", DeveloperOnly: true)
        ])
    ];

    public static IReadOnlyList<AdminMenuItem> AllItems => Sections.SelectMany(x => x.Items).ToList();

    public static HashSet<string> Normalize(IEnumerable<string>? menuIds)
    {
        var validIds = AllItems.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (menuIds ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(validIds.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record AdminMenuSection(string Title, IReadOnlyList<AdminMenuItem> Items);

public sealed record AdminMenuItem(
    string Id,
    string Label,
    string Url,
    string Icon,
    bool ExactMatch = false,
    bool DeveloperOnly = false);
