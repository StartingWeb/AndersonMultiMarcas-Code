namespace Project.Navigation;

public sealed class AdminMenuViewModel
{
    public string UserDisplayName { get; init; } = "Usuario";
    public string UserRoleLabel { get; init; } = "Sem perfil";
    public IReadOnlyList<MenuNodeViewModel> RootItems { get; init; } = [];

    public sealed class MenuNodeViewModel
    {
        public Guid Id { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string? Url { get; init; }
        public string Icone { get; init; } = "bi bi-grid";
        public bool IsActive { get; init; }
        public IReadOnlyList<MenuNodeViewModel> Children { get; init; } = [];
    }
}
