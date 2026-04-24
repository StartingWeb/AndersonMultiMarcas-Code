using Data;
using Domain.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Project.Config;
using System.IO.Compression;
using System.Globalization;

var Producao = true;
var builder = WebApplication.CreateBuilder(args);
var ptBrCulture = new CultureInfo("pt-BR");

CultureInfo.DefaultThreadCurrentCulture = ptBrCulture;
CultureInfo.DefaultThreadCurrentUICulture = ptBrCulture;

// BANCO
if (Producao)
{
    builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnectionProd")));
}
else
{
    builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnectionDev")));
}

// IDENTITY
builder.Services
    .AddIdentity<AspNetCoreUser, AspNetCoreRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// SEUS SERVICOS
builder.Services.AddProjectDependencies();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Login";
    options.AccessDeniedPath = "/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBrCulture),
    SupportedCultures = [ptBrCulture],
    SupportedUICultures = [ptBrCulture]
};

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("StartupMigration");

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("Aplicando migrations pendentes no banco de dados.");
        await db.Database.MigrateAsync();

        logger.LogInformation("Executando seed de usuarios, perfis e menus.");
        await IdentitySeed.EnsureDeveloperUserAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Falha ao aplicar migrations automaticas na inicializacao.");
        throw;
    }
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var extension = Path.GetExtension(context.File.Name);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return;
        }

        var headers = context.Context.Response.Headers;
        var cacheDuration = extension.ToLowerInvariant() switch
        {
            ".css" or ".js" => TimeSpan.FromDays(30),
            ".png" or ".jpg" or ".jpeg" or ".webp" or ".svg" or ".ico" or ".woff" or ".woff2" => TimeSpan.FromDays(30),
            ".mp4" => TimeSpan.FromDays(7),
            _ => TimeSpan.Zero
        };

        if (cacheDuration <= TimeSpan.Zero)
        {
            return;
        }

        headers[HeaderNames.CacheControl] = $"public,max-age={(int)cacheDuration.TotalSeconds}";
    }
});
app.UseRequestLocalization(localizationOptions);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
await app.RunAsync();
