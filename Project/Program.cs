using Data;
using Domain.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var Producao = false;
var builder = WebApplication.CreateBuilder(args);

// ==============================
// BANCO DE DADOS
// ==============================
if (Producao)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnectionProd")
        )
    );
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnectionDev")
        )
    );
}


// ==============================
// IDENTITY
// ==============================
builder.Services
    .AddIdentity<AspNetCoreUser, IdentityRole>(options =>
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
// DEPENDENCY INJECTION
// ==============================

// ==============================
// RAZOR PAGES
// ==============================
builder.Services.AddRazorPages();

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
// MIGRATIONS AUTOMÁTICAS (DEV + PROD)
// ==============================
using (var scope = app.Services.CreateScope())
{
    await IdentitySeed.EnsureDeveloperUserAsync(scope.ServiceProvider);
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

await app.RunAsync();
