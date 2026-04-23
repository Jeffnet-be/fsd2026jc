using AutoMapper;
using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using ChampionsLeague.Infrastructure.Repositories;
using ChampionsLeague.Infrastructure.Services;
using ChampionsLeague.Services;
using ChampionsLeague.Web.AutoMapper;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + localisation ────────────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// ── Swagger / OpenAPI (curriculum section 4) ──────────────────────────
// Swagger UI available at /swagger — documents all API endpoints.
// Use Postman to call /api/tickets, /api/matches etc.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Champions League Ticket Portal API",
        Version     = "v1",
        Description = "REST API for the CL Tickets portal — Full Stack Development VIVES Hogeschool"
    });

    // Support Bearer token auth in Swagger UI (for testing authenticated endpoints)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your bearer token here"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ── AutoMapper 13 ────────────────────────────────────────────────────
builder.Services.AddAutoMapper(typeof(Program));

// ── EF Core ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.MigrationsAssembly("ChampionsLeague.Infrastructure");
            sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        }
    )
);

// ── ASP.NET Core Identity ─────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit           = true;
        options.Password.RequiredLength         = 12;
        options.Password.RequireNonAlphanumeric = true;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ChampionsLeague.Web.Services.TranslationService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IEmailService, EmailService>(); // MailKit — reads Email:* from config

// ── Hotel API ─────────────────────────────────────────────────────────
builder.Services.AddHttpClient<IHotelApiService, HotelApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["HotelApi:BaseUrl"]
                                 ?? "https://api.hotels.example.com/");
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
    opts.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
    opts.RequestCultureProviders.Insert(1, new QueryStringRequestCultureProvider());
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

// ── Migrations on startup ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying EF Core migrations...");
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "User" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed. Check ConnectionStrings__DefaultConnection.");
    }
}

// ── Middleware pipeline ───────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Swagger — available in all environments so jury can test on Azure
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CL Tickets API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "CL Tickets API";
});

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
