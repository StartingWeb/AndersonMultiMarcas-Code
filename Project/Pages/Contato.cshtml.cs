using Core.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Project.Pages;

public class ContatoModel : PageModel
{
    private readonly ILojaService _lojaService;
    private readonly IVendedorService _vendedorService;

    public ContatoModel(ILojaService lojaService, IVendedorService vendedorService)
    {
        _lojaService = lojaService;
        _vendedorService = vendedorService;
    }

    public IReadOnlyList<ContactStoreItem> Stores { get; private set; } = [];
    public IReadOnlyList<ContactSellerItem> Sellers { get; private set; } = [];
    public string? SellersLoadMessage { get; private set; }
    public int TotalStores => Stores.Count;
    public int TotalSellers => Sellers.Count;

    public async Task OnGetAsync()
    {
        var lojasTask = _lojaService.ListarAtivasAsync();
        var vendedoresTask = _vendedorService.ListarAsync();

        await Task.WhenAll(lojasTask, vendedoresTask);

        var lojas = (lojasTask.Result.Data ?? [])
            .Where(loja => loja.Ativo)
            .OrderBy(loja => loja.Nome)
            .ToList();

        var resultadoVendedores = vendedoresTask.Result;
        if (resultadoVendedores.Status != Core.Enums.PackageStatus.Success || resultadoVendedores.Data == null)
        {
            Sellers = [];
            SellersLoadMessage = string.IsNullOrWhiteSpace(resultadoVendedores.UserMessage)
                ? "Não foi possível carregar os vendedores no momento."
                : resultadoVendedores.UserMessage;
        }
        else
        {
            Sellers = resultadoVendedores.Data
                .OrderBy(vendedor => vendedor.Nome)
                .Select(ContactSellerItem.From)
                .ToList();

            SellersLoadMessage = null;
        }

        var vendedoresPorLoja = Sellers
            .GroupBy(vendedor => vendedor.StoreId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<ContactSellerItem>)group.ToList());

        Stores = lojas
            .Select(loja => ContactStoreItem.From(
                loja,
                vendedoresPorLoja.TryGetValue(loja.Id, out var sellers) ? sellers : []))
            .ToList();
    }

    public sealed class ContactStoreItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Address { get; init; } = "Endereço não informado.";
        public string Email { get; init; } = string.Empty;
        public string PhoneDisplay { get; init; } = string.Empty;
        public string PhoneUrl { get; init; } = string.Empty;
        public string MapsEmbedUrl { get; init; } = string.Empty;
        public string MapsLinkUrl { get; init; } = string.Empty;
        public IReadOnlyList<ContactSellerItem> Sellers { get; init; } = [];

        public static ContactStoreItem From(Core.Dtos.LojaDto loja, IReadOnlyList<ContactSellerItem> sellers)
        {
            var address = string.Join(", ", new[]
            {
                string.Join(", ", new[] { loja.Endereco, loja.Numero }.Where(value => !string.IsNullOrWhiteSpace(value))),
                loja.Complemento,
                loja.Bairro,
                string.Join(" - ", new[] { loja.Cidade, loja.Uf }.Where(value => !string.IsNullOrWhiteSpace(value))),
                loja.Cep
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

            var mapsQuery = Uri.EscapeDataString(string.IsNullOrWhiteSpace(address) ? loja.Nome : address);

            return new ContactStoreItem
            {
                Id = loja.Id,
                Name = loja.Nome,
                Address = string.IsNullOrWhiteSpace(address) ? "Endereço não informado." : address,
                Email = loja.Email ?? string.Empty,
                PhoneDisplay = FormatPhone(loja.Telefone),
                PhoneUrl = BuildPhoneUrl(loja.Telefone),
                MapsEmbedUrl = $"https://www.google.com/maps?q={mapsQuery}&output=embed",
                MapsLinkUrl = $"https://www.google.com/maps/search/?api=1&query={mapsQuery}",
                Sellers = sellers
            };
        }
    }

    public sealed class ContactSellerItem
    {
        public int Id { get; init; }
        public int StoreId { get; init; }
        public string StoreName { get; init; } = "Loja";
        public string Name { get; init; } = string.Empty;
        public string Role { get; init; } = "Consultor de vendas";
        public string PhotoUrl { get; init; } = string.Empty;
        public string Initials { get; init; } = "AM";
        public string PhoneDisplay { get; init; } = string.Empty;
        public string PhoneUrl { get; init; } = string.Empty;
        public string WhatsappDisplay { get; init; } = string.Empty;
        public string WhatsappUrl { get; init; } = string.Empty;

        public static ContactSellerItem From(Domain.Vendedor vendedor)
        {
            return new ContactSellerItem
            {
                Id = vendedor.Id,
                StoreId = vendedor.LojaId,
                StoreName = string.IsNullOrWhiteSpace(vendedor.Loja?.Nome) ? "Loja" : vendedor.Loja.Nome,
                Name = vendedor.Nome,
                Role = string.IsNullOrWhiteSpace(vendedor.Cargo) ? "Consultor de vendas" : vendedor.Cargo,
                PhotoUrl = NormalizePhotoUrl(vendedor.FotoUrl),
                Initials = BuildInitials(vendedor.Nome),
                PhoneDisplay = FormatPhone(vendedor.Telefone),
                PhoneUrl = BuildPhoneUrl(vendedor.Telefone),
                WhatsappDisplay = FormatPhone(vendedor.Whatsapp),
                WhatsappUrl = BuildWhatsappUrl(vendedor.Whatsapp, vendedor.Nome)
            };
        }
    }

    private static string NormalizePhotoUrl(string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(photoUrl, UriKind.Absolute, out _))
        {
            return photoUrl;
        }

        return photoUrl.StartsWith('/') ? photoUrl : $"/{photoUrl.TrimStart('/')}";
    }

    private static string BuildInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "AM";
        }

        var parts = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]));

        var initials = string.Concat(parts);
        return string.IsNullOrWhiteSpace(initials) ? "AM" : initials;
    }

    private static string FormatPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var digits = DigitsOnly(value);

        if (digits.Length == 11)
        {
            return $"({digits[..2]}) {digits[2..7]}-{digits[7..]}";
        }

        if (digits.Length == 10)
        {
            return $"({digits[..2]}) {digits[2..6]}-{digits[6..]}";
        }

        return value.Trim();
    }

    private static string BuildPhoneUrl(string? value)
    {
        var digits = DigitsOnly(value);
        return string.IsNullOrWhiteSpace(digits) ? string.Empty : $"tel:+55{digits}";
    }

    private static string BuildWhatsappUrl(string? value, string sellerName)
    {
        var digits = DigitsOnly(value);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        var message = Uri.EscapeDataString($"Olá, quero falar com {sellerName} sobre um veículo.");
        return $"https://wa.me/55{digits}?text={message}";
    }

    private static string DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }
}
