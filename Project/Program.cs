using Data;
using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// BANCO DE DADOS
// ==============================
if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnectionProd"))
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnectionDev"))
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
}

// ==============================
// IDENTITY
// ==============================
builder.Services
    .AddIdentity<AspNetCoreUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ==============================
// RAZOR PAGES
// ==============================
builder.Services.AddRazorPages();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// ==============================
// COOKIE / AUTH
// ==============================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();
var siteBaseUrl = builder.Configuration["Seo:BaseUrl"]?.TrimEnd('/');

// ==============================
// PIPELINE
// ==============================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ==============================
// MIGRATIONS AUTOMATICAS (APENAS DEV)
// ==============================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await IdentitySeed.EnsureDeveloperUserAsync(scope.ServiceProvider);
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 30;
        context.Context.Response.Headers.CacheControl = $"public,max-age={durationInSeconds}";
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/robots.txt", (HttpContext context) =>
{
    var baseUrl = siteBaseUrl ?? $"{context.Request.Scheme}://{context.Request.Host}";
    var sb = new StringBuilder();
    sb.AppendLine("User-agent: *");
    sb.AppendLine("Allow: /");
    sb.AppendLine("Disallow: /Error");
    sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
    return Results.Text(sb.ToString(), "text/plain");
});

app.MapGet("/sitemap.xml", (HttpContext context) =>
{
    var baseUrl = siteBaseUrl ?? $"{context.Request.Scheme}://{context.Request.Host}";
    var now = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var urls = new[]
    {
        $"{baseUrl}/",
        $"{baseUrl}/Privacy"
    };

    var sb = new StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
    foreach (var url in urls)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{url}</loc>");
        sb.AppendLine($"    <lastmod>{now}</lastmod>");
        sb.AppendLine("    <changefreq>weekly</changefreq>");
        sb.AppendLine("    <priority>0.8</priority>");
        sb.AppendLine("  </url>");
    }
    sb.AppendLine("</urlset>");
    return Results.Text(sb.ToString(), "application/xml");
});

app.MapRazorPages();

await app.RunAsync();
