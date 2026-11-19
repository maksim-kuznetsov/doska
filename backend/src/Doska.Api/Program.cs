using System.Security.Claims;
using System.Threading.RateLimiting;
using Doska.Api.Background;
using Doska.Api.Infrastructure;
using Doska.Application.Abstractions;
using Doska.Application.Accounts;
using Doska.Application.Ads;
using Doska.Application.Auth;
using Doska.Application.Categories;
using Doska.Application.Locations;
using Doska.Application.Moderation;
using Doska.Domain.Enums;
using Doska.Infrastructure.Email;
using Doska.Infrastructure.Media;
using Doska.Infrastructure.Persistence;
using Doska.Infrastructure.Security;
using Doska.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ServiceExceptionHandler>();

// Фотографии до 5 МБ держим в памяти, чтобы не создавать временные файлы на диск.
builder.Services.Configure<FormOptions>(options =>
{
    options.MemoryBufferThreshold = 10 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 32 * 1024 * 1024;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection(MediaOptions.SectionName));
builder.Services.AddScoped<IAdPhotoStorage, LocalAdPhotoStorage>();
builder.Services.AddHostedService<AdArchiveBackgroundService>();

// Режим отправки писем: журнал, файловый ящик или SMTP. При пустом Mode он
// выводится из наличия Email:Host — так сохраняется прежнее поведение.
var emailHost = builder.Configuration[$"{EmailOptions.SectionName}:Host"];
var emailMode = builder.Configuration[$"{EmailOptions.SectionName}:Mode"];
if (string.IsNullOrWhiteSpace(emailMode))
{
    emailMode = string.IsNullOrWhiteSpace(emailHost) ? EmailModes.Log : EmailModes.Smtp;
}

switch (emailMode.Trim().ToLowerInvariant())
{
    case "smtp":
        if (string.IsNullOrWhiteSpace(emailHost))
        {
            throw new InvalidOperationException(
                "Email:Host обязателен при Email:Mode=Smtp.");
        }

        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        break;

    case "file":
        builder.Services.AddScoped<IEmailSender, FileEmailSender>();
        break;

    default:
        builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
        break;
}

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdService>();
builder.Services.AddScoped<AdSearchService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CityService>();
builder.Services.AddScoped<ModerationService>();
builder.Services.AddScoped<ComplaintService>();
builder.Services.AddScoped<TrustService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "doska.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            // Роль в cookie фиксируется при входе, поэтому на каждом запросе берём
            // актуальные данные пользователя: смена роли и блокировка действуют сразу.
            OnValidatePrincipal = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    await RejectSessionAsync(context);
                    return;
                }

                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        candidate => candidate.Id == userId,
                        context.HttpContext.RequestAborted);

                if (user is null || user.IsBlocked)
                {
                    await RejectSessionAsync(context);
                    return;
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Role, user.Role.ToString()),
                };

                context.ReplacePrincipal(new ClaimsPrincipal(
                    new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            },
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.ModeratorOrAdmin,
        policy => policy.RequireRole(nameof(UserRole.Moderator), nameof(UserRole.Admin)));

    options.AddPolicy(
        AuthorizationPolicies.Admin,
        policy => policy.RequireRole(nameof(UserRole.Admin)));
});

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Sensitive, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

const string frontendDevPolicy = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendDevPolicy, policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(frontendDevPolicy);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

await ApplyMigrationsAsync(app);

if (args.Contains("seed", StringComparer.OrdinalIgnoreCase))
{
    await SeedDemoDataAsync(app);
    return;
}

app.MapControllers();

app.Run();

static async Task RejectSessionAsync(CookieValidatePrincipalContext context)
{
    context.RejectPrincipal();
    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}

static async Task ApplyMigrationsAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Database:MigrateOnStartup", true))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Doska.Startup");
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    logger.LogInformation("Применение миграций базы данных.");

    try
    {
        await context.Database.MigrateAsync();
    }
    catch (Exception exception)
    {
        logger.LogCritical(exception, "Не удалось применить миграции базы данных. Приложение останавливается.");
        throw;
    }

    logger.LogInformation("Миграции базы данных применены.");
}

static async Task SeedDemoDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Doska.Seed");

    if (app.Environment.IsProduction())
    {
        logger.LogWarning("Демонстрационные данные не создаются в окружении Production.");
        return;
    }

    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await DemoDataSeeder.SeedAsync(context, passwordHasher);
    logger.LogInformation("Демонстрационные данные созданы.");
}

/// <summary>Точка входа приложения: требуется для интеграционных тестов.</summary>
public partial class Program;
