using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using ChampionsLeague.Infrastructure.Repositories;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using Microsoft.AspNetCore.HttpOverrides;
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
// On Azure: set App Setting  ConnectionStrings__DefaultConnection  (type SQLAzure)
// Locally:  falls back to appsettings.json DefaultConnection value
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.MigrationsAssembly("ChampionsLeague.Infrastructure");
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
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.SameSite     = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// ── Repositories ──────────────────────────────────────────────────────
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
var supportedCultures = new[]
{
    new CultureInfo("nl"),
    new CultureInfo("fr"),
    new CultureInfo("en")
};
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
    opts.IdleTimeout         = TimeSpan.FromMinutes(30);
    opts.Cookie.HttpOnly     = true;
    opts.Cookie.IsEssential  = true;
    opts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
});

// ════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ════════════════════════════════════════════════════════════════════

// ── Run EF Core migrations on startup ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
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
        logger.LogError(ex,
            "Migration failed. Check ConnectionStrings__DefaultConnection in Azure App Settings.");
    }
}

// ── Middleware pipeline ───────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// UseForwardedHeaders MUST come before UseHttpsRedirection so Azure's
// load-balancer HTTPS is correctly recognised.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
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
