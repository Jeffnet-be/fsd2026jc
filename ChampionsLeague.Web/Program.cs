using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using ChampionsLeague.Infrastructure.Repositories;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + localisation ────────────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// ── AutoMapper 13 ────────────────────────────────────────────────────
builder.Services.AddAutoMapper(
    typeof(ChampionsLeague.Web.AutoMapper.AutoMapperProfile).Assembly);

// ── EF Core ───────────────────────────────────────────────────────────
// On Azure, the connection string is set as an App Service environment
// variable named:  ConnectionStrings__DefaultConnection
// (double underscore = colon in Azure configuration)
// Locally it falls back to the value in appsettings.json.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.MigrationsAssembly("ChampionsLeague.Infrastructure");
            // Retry on transient Azure SQL connection errors
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }
    )
);

// ── ASP.NET Core Identity ─────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit           = true;
        options.Password.RequiredLength         = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedEmail     = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/Account/Login";
    options.LogoutPath       = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";

    // Required when running behind Azure's reverse proxy (HTTPS offloading)
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.SameSite     = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// ── Repositories (Scoped = one per HTTP request) ──────────────────────
builder.Services.AddScoped<IMatchRepository,        MatchRepository>();
builder.Services.AddScoped<ITicketRepository,       TicketRepository>();
builder.Services.AddScoped<IOrderRepository,        OrderRepository>();
builder.Services.AddScoped<IClubRepository,         ClubRepository>();
builder.Services.AddScoped<ISeasonTicketRepository, SeasonTicketRepository>();

// ── Application services ──────────────────────────────────────────────
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// ── Hotel API (typed HttpClient) ──────────────────────────────────────
builder.Services.AddHttpClient<IHotelApiService, HotelApiService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["HotelApi:BaseUrl"] ?? "https://api.hotels.example.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ── Localisation ──────────────────────────────────────────────────────
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
var supportedCultures = new[] { new CultureInfo("nl"), new CultureInfo("fr"), new CultureInfo("en") };
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    opts.DefaultRequestCulture = new RequestCulture("nl");
    opts.SupportedCultures     = supportedCultures;
    opts.SupportedUICultures   = supportedCultures;
    opts.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
});

// ── Session ───────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opts =>
{
    opts.IdleTimeout        = TimeSpan.FromMinutes(30);
    opts.Cookie.HttpOnly    = true;
    opts.Cookie.IsEssential = true;
    opts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
});

// ════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ════════════════════════════════════════════════════════════════════

// ── Run EF Core migrations on startup ────────────────────────────────
// Works both locally and on Azure — safe to run every deployment because
// EF Core skips migrations that have already been applied.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying EF Core migrations...");
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "User" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }
    catch (Exception ex)
    {
        // Log but don't crash — lets you diagnose connection issues
        // from the Azure App Service logs without a cold-start failure.
        logger.LogError(ex,
            "An error occurred applying migrations. " +
            "Check the ConnectionStrings__DefaultConnection app setting in Azure.");
    }
}

// ── Middleware pipeline ───────────────────────────────────────────────

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Azure terminates TLS at the load balancer — forwarded headers let
// the app know the original request was HTTPS.
app.UseForwardedHeaders(new Microsoft.AspNetCore.HttpOverrides.ForwardedHeadersOptions
{
    ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
