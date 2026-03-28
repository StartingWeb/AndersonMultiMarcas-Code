using System.Globalization;
using System.Text;

namespace Project.Catalog;

public static class VehicleCatalogStore
{
    public static IReadOnlyList<VehicleCatalogItem> All { get; } = BuildVehicles();

    public static VehicleCatalogItem? GetBySlug(string? slug) =>
        All.FirstOrDefault(vehicle => vehicle.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<VehicleCatalogItem> GetRelated(VehicleCatalogItem vehicle, int take = 3) =>
        All.Where(item => item.Slug != vehicle.Slug)
            .OrderByDescending(item => item.Type.Equals(vehicle.Type, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(item => item.Brand.Equals(vehicle.Brand, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(item => Math.Abs(item.Price - vehicle.Price) <= 25000)
            .ThenByDescending(item => item.Year)
            .Take(take)
            .ToList();

    private static List<VehicleCatalogItem> BuildVehicles()
    {
        return
        [
            CreateVehicle(
                brand: "Chevrolet",
                model: "Onix 1.0 Turbo Premier",
                year: 2020,
                type: "Hatch",
                gearbox: "Automático",
                fuel: "Flex",
                price: 82995M,
                mileage: 82995,
                tag: "Promoção",
                highlight: "Entrega imediata",
                color: "Branco Summit",
                category: "Hatch premium",
                version: "Premier Turbo AT",
                plateFinal: "7",
                description: "Compacto completo para quem quer economia no dia a dia sem abrir mão de conforto, tecnologia e acabamento acima da média.",
                features:
                [
                    "Central multimídia com espelhamento",
                    "Banco em couro",
                    "Chave presencial com partida por botão",
                    "Câmera de ré",
                    "Seis airbags",
                    "Rodas de liga leve"
                ],
                photoLabels: ["Frente", "Lateral", "Interior", "Painel"]),

            CreateVehicle(
                brand: "Chevrolet",
                model: "Tracker LTZ 1.2 Turbo",
                year: 2022,
                type: "SUV",
                gearbox: "Automático",
                fuel: "Flex",
                price: 119900M,
                mileage: 31800,
                tag: "Destaque",
                highlight: "Único dono",
                color: "Cinza Rush",
                category: "SUV urbano",
                version: "LTZ Turbo AT",
                plateFinal: "2",
                description: "SUV moderno, espaçoso e muito procurado, ideal para quem quer posição de dirigir elevada, conectividade e ótimo custo-benefício.",
                features:
                [
                    "Painel digital",
                    "Multimídia com Wi-Fi nativo",
                    "Ar digital",
                    "Sensor de estacionamento",
                    "Controle de estabilidade",
                    "Piloto automático"
                ],
                photoLabels: ["Frente", "Traseira", "Cabine", "Porta-malas"]),

            CreateVehicle(
                brand: "Fiat",
                model: "Toro Freedom 1.8",
                year: 2021,
                type: "Picape",
                gearbox: "Automático",
                fuel: "Flex",
                price: 128900M,
                mileage: 56200,
                tag: "Novidade",
                highlight: "Cabine dupla",
                color: "Vermelho Montecarlo",
                category: "Picape intermediária",
                version: "Freedom AT6",
                plateFinal: "9",
                description: "Versátil para trabalho e lazer, com cabine dupla confortável, bom espaço interno e visual forte para quem quer presença.",
                features:
                [
                    "Capota marítima",
                    "Central multimídia",
                    "Direção elétrica",
                    "Piloto automático",
                    "Santo Antônio integrado",
                    "Sensor de estacionamento traseiro"
                ],
                photoLabels: ["Frente", "Caçamba", "Bancos", "Detalhes"]),

            CreateVehicle(
                brand: "Honda",
                model: "HR-V EX 1.8",
                year: 2020,
                type: "SUV",
                gearbox: "CVT",
                fuel: "Flex",
                price: 112800M,
                mileage: 63700,
                tag: "Família",
                highlight: "Muito conservado",
                color: "Prata Platinum",
                category: "SUV familiar",
                version: "EX CVT",
                plateFinal: "4",
                description: "SUV conhecido pela confiabilidade Honda, dirigibilidade suave e excelente espaço interno para rotina urbana e viagens.",
                features:
                [
                    "Câmera lateral LaneWatch",
                    "Bancos rebatíveis Magic Seat",
                    "Ar digital",
                    "Multimídia touchscreen",
                    "Freio de estacionamento eletrônico",
                    "Rodas aro 17"
                ],
                photoLabels: ["Frente", "Perfil", "Interior", "Painel"]),

            CreateVehicle(
                brand: "Jeep",
                model: "Compass Longitude T270",
                year: 2023,
                type: "SUV",
                gearbox: "Automático",
                fuel: "Flex",
                price: 162900M,
                mileage: 19400,
                tag: "Premium",
                highlight: "Painel digital",
                color: "Azul Jazz",
                category: "SUV premium",
                version: "Longitude T270 AT6",
                plateFinal: "1",
                description: "Perfil premium, acabamento refinado e pacote tecnológico forte para quem busca um SUV sofisticado, pronto para impressionar.",
                features:
                [
                    "Painel 10,25\"",
                    "Faróis full LED",
                    "Carregador por indução",
                    "Ar dual zone",
                    "Assistente de partida em rampa",
                    "Chave presencial"
                ],
                photoLabels: ["Frente", "Lateral", "Cabine", "Console"]),

            CreateVehicle(
                brand: "Toyota",
                model: "Hilux SRV 4x4",
                year: 2019,
                type: "Picape",
                gearbox: "Automático",
                fuel: "Diesel",
                price: 169995M,
                mileage: 102500,
                tag: "Impecável",
                highlight: "Pronta para trabalho",
                color: "Preto Attitude",
                category: "Picape diesel 4x4",
                version: "SRV 4x4 AT",
                plateFinal: "5",
                description: "Picape robusta e valorizada, excelente para quem precisa de força, confiabilidade e conforto em uso misto urbano e rural.",
                features:
                [
                    "Tração 4x4 reduzida",
                    "Bancos em couro",
                    "Santantônio cromado",
                    "Multimídia com câmera",
                    "Ar digital",
                    "Controle de tração"
                ],
                photoLabels: ["Frente", "Traseira", "Cabine", "4x4"])
        ];
    }

    private static VehicleCatalogItem CreateVehicle(
        string brand,
        string model,
        int year,
        string type,
        string gearbox,
        string fuel,
        decimal price,
        int mileage,
        string tag,
        string highlight,
        string color,
        string category,
        string version,
        string plateFinal,
        string description,
        IReadOnlyList<string> features,
        IReadOnlyList<string> photoLabels)
    {
        var slug = Slugify($"{brand}-{model}-{year}");
        var fullName = $"{brand} {model}";
        var photos = photoLabels
            .Select((label, index) => new VehiclePhotoItem(
                $"{label} {fullName}",
                BuildSvgDataUri(brand, model, label, index)))
            .ToList();

        return new VehicleCatalogItem(
            Slug: slug,
            Brand: brand,
            Model: model,
            Year: year,
            Type: type,
            Gearbox: gearbox,
            Fuel: fuel,
            Price: price,
            Mileage: mileage,
            Tag: tag,
            Highlight: highlight,
            Color: color,
            Category: category,
            Version: version,
            PlateFinal: plateFinal,
            Description: description,
            Features: features,
            Photos: photos);
    }

    private static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (builder.Length == 0 || builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string BuildSvgDataUri(string brand, string model, string label, int index)
    {
        var gradients = new[]
        {
            ("#111827", "#a61d24"),
            ("#1f2937", "#7f1419"),
            ("#0f172a", "#b42318"),
            ("#1e293b", "#8f1d21")
        };

        var selected = gradients[index % gradients.Length];
        var svg = $"""
            <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1600 1000'>
                <defs>
                    <linearGradient id='bg' x1='0' y1='0' x2='1' y2='1'>
                        <stop offset='0%' stop-color='{selected.Item1}' />
                        <stop offset='100%' stop-color='{selected.Item2}' />
                    </linearGradient>
                </defs>
                <rect width='1600' height='1000' fill='url(#bg)' />
                <circle cx='1220' cy='180' r='260' fill='rgba(255,255,255,.08)' />
                <circle cx='320' cy='840' r='300' fill='rgba(255,255,255,.06)' />
                <text x='120' y='170' fill='rgba(255,255,255,.7)' font-size='52' font-family='Arial, sans-serif'>{label}</text>
                <text x='120' y='280' fill='white' font-size='88' font-weight='700' font-family='Arial, sans-serif'>{brand}</text>
                <text x='120' y='380' fill='white' font-size='64' font-family='Arial, sans-serif'>{model}</text>
                <rect x='120' y='560' rx='28' ry='28' width='560' height='170' fill='rgba(255,255,255,.12)' stroke='rgba(255,255,255,.18)' />
                <text x='160' y='640' fill='white' font-size='42' font-family='Arial, sans-serif'>Anderson Multimarcas</text>
                <text x='160' y='696' fill='rgba(255,255,255,.76)' font-size='30' font-family='Arial, sans-serif'>Galeria demonstrativa pronta para receber fotos reais</text>
            </svg>
            """;

        return $"data:image/svg+xml;charset=UTF-8,{Uri.EscapeDataString(svg)}";
    }
}

public sealed record VehicleCatalogItem(
    string Slug,
    string Brand,
    string Model,
    int Year,
    string Type,
    string Gearbox,
    string Fuel,
    decimal Price,
    int Mileage,
    string Tag,
    string Highlight,
    string Color,
    string Category,
    string Version,
    string PlateFinal,
    string Description,
    IReadOnlyList<string> Features,
    IReadOnlyList<VehiclePhotoItem> Photos);

public sealed record VehiclePhotoItem(string Alt, string Url);
