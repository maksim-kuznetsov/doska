using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Doska.Api.Background;
using Doska.Api.Infrastructure;
using Doska.Application.Abstractions;
using Doska.Application.Accounts;
using Doska.Application.Ads;
using Doska.Application.Auth;
using Doska.Application.Categories;
using Doska.Application.Common;
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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ServiceExceptionHandler>();

// Числовые поля формы (например, цена объявления) разбираются в инвариантной
// культуре, чтобы результат не зависел от локали сервера.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
    options.SupportedCultures = [CultureInfo.InvariantCulture];
    options.SupportedUICultures = [CultureInfo.InvariantCulture];
});

// Фотографии до 5 МБ держим в памяти, чтобы не создавать временные файлы на диск.
builder.Services.Configure<FormOptions>(options =>
{
    options.MemoryBufferThreshold = 10 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 32 * 1024 * 1024;
});

// Согласуем лимит Kestrel с FormOptions и веб-сервером: запрос с фотографиями
// в пределах прикладных ограничений не должен отклоняться транспортом с 413.
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 32L * 1024 * 1024);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(TimeProvider.System);

// В production перед API стоят Caddy и nginx: доверяем заголовкам прокси,
// чтобы запрос считался https, а ограничение частоты видело адрес клиента.
// API не публикует порт наружу, поэтому доверять можно всей внутренней сети.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// В Development ключи Data Protection сохраняются на диске, чтобы сессии
// и antiforgery-токены переживали перезапуск API.
if (builder.Environment.IsDevelopment())
{
    var keyRingPath = Path.Combine(builder.Environment.ContentRootPath, "media", "keys");
    Directory.CreateDirectory(keyRingPath);
    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<PublicUrlOptions>(builder.Configuration.GetSection(PublicUrlOptions.SectionName));
builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection(MediaOptions.SectionName));
builder.Services.Configure<GoogleSignInOptions>(
    builder.Configuration.GetSection(GoogleSignInOptions.SectionName));

var googleSettings = builder.Configuration
    .GetSection(GoogleSignInOptions.SectionName)
    .Get<GoogleSignInOptions>() ?? new GoogleSignInOptions();
builder.Services.AddSingleton<IPublicUrlProvider, ConfiguredPublicUrlProvider>();
builder.Services.AddScoped<IAdPhotoStorage, LocalAdPhotoStorage>();
builder.Services.AddScoped<IMediaMaintenance, MediaMaintenanceService>();
builder.Services.AddHostedService<AdArchiveBackgroundService>();

// Режим отправки писем: журнал, файловый ящик или SMTP. При пустом Mode он
// выводится из наличия Email:Host — так сохраняется прежнее поведение.
var emailHost = builder.Configuration[$"{EmailOptions.SectionName}:Host"];
var emailMode = builder.Configuration[$"{EmailOptions.SectionName}:Mode"];
if (string.IsNullOrWhiteSpace(emailMode))
{
    emailMode = string.IsNullOrWhiteSpace(emailHost) ? EmailModes.Log : EmailModes.Smtp;
}

ProductionSettings.Validate(builder.Configuration, builder.Environment, emailMode);

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

// Чтение файлового ящика доступно независимо от режима отправки: вне File каталог пуст.
builder.Services.AddScoped<IEmailOutbox, FileEmailOutbox>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GoogleAuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<AdService>();
builder.Services.AddScoped<AdSearchService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CityService>();
builder.Services.AddScoped<ModerationService>();
builder.Services.AddScoped<ComplaintService>();
builder.Services.AddScoped<TrustService>();

var authentication = builder.Services
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
                var authService = context.HttpContext.RequestServices.GetRequiredService<AuthService>();
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    await RejectSessionAsync(context);
                    return;
                }

                try
                {
                    var user = await authService.GetAsync(userId, context.HttpContext.RequestAborted);
                    context.ReplacePrincipal(ClaimsPrincipalExtensions.CreateSessionPrincipal(user));
                }
                catch (ServiceException)
                {
                    await RejectSessionAsync(context);
                }
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

// Google-провайдер подключается только при наличии кредов. Без них приложение
// работает как раньше, а публичный статус провайдера сообщает, что вход выключен.
if (googleSettings.IsConfigured)
{
    authentication.AddCookie(GoogleAuthentication.ExternalScheme, options =>
    {
        options.Cookie.Name = "doska.google";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = false;
    });

    authentication.AddGoogle(options =>
    {
        options.ClientId = googleSettings.ClientId!.Trim();
        options.ClientSecret = googleSettings.ClientSecret!.Trim();
        options.CallbackPath = "/api/auth/google/callback";
        options.SignInScheme = GoogleAuthentication.ExternalScheme;
        options.SaveTokens = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");

        options.CorrelationCookie.Name = "doska.google.correlation";
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.Events.OnCreatingTicket = GoogleAuthentication.OnCreatingTicket;
        options.Events.OnTicketReceived = GoogleAuthentication.OnTicketReceived;
        options.Events.OnRemoteFailure = GoogleAuthentication.OnRemoteFailure;
        options.Events.OnAccessDenied = GoogleAuthentication.OnAccessDenied;
    });
}

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

    // Пределы настраиваются, чтобы интеграционные тесты могли поднять их для обычных
    // сценариев и понизить лимит повторных писем для проверки отказа 429.
    var sensitivePermitLimit = builder.Configuration.GetValue(
        "RateLimit:Sensitive:PermitLimit",
        30);
    var confirmationResendPermitLimit = builder.Configuration.GetValue(
        "RateLimit:ConfirmationResend:PermitLimit",
        5);

    options.AddPolicy(RateLimitPolicies.Sensitive, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = sensitivePermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy(RateLimitPolicies.ConfirmationResend, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = confirmationResendPermitLimit,
                Window = TimeSpan.FromMinutes(15),
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

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
}

// Google OAuth строит redirect_uri из Host запроса. Обратный прокси может
// потерять нестандартный порт, поэтому для этих путей берём публичный адрес сайта.
app.UseGooglePublicHost();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(frontendDevPolicy);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRequestLocalization();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

await ApplyMigrationsAsync(app);

if (args.Contains("init-data", StringComparer.OrdinalIgnoreCase))
{
    await InitReferenceDataAsync(app);
    return;
}

if (args.Contains("seed-catalog", StringComparer.OrdinalIgnoreCase))
{
    await SeedCatalogAsync(app);
    return;
}

if (args.Contains("create-admin", StringComparer.OrdinalIgnoreCase))
{
    Environment.ExitCode = await CreateAdminAsync(app);
    return;
}

if (args.Contains("seed", StringComparer.OrdinalIgnoreCase))
{
    await SeedDemoDataAsync(app);
    return;
}

if (args.Contains("cleanup-media", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var maintenance = scope.ServiceProvider.GetRequiredService<IMediaMaintenance>();
    var result = await maintenance.CleanupAsync();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Doska.Maintenance");

    logger.LogInformation(
        "Очистка медиа: удалено каталогов — {Directories}, файлов — {Files}.",
        result.RemovedDirectories,
        result.RemovedFiles);

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

static async Task InitReferenceDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Doska.InitData");
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

    await ReferenceDataSeeder.SeedAsync(context, clock);
    logger.LogInformation("Справочники городов и категорий созданы.");
}

static async Task SeedCatalogAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Doska.SeedCatalog");
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

    var result = await ExtendedCatalogSeeder.SeedAsync(context, clock);

    logger.LogInformation(
        "Расширенный справочник: добавлено городов — {Cities}, разделов — {Sections}, подразделов — {Subsections}; пропущено занятых названий — {Skipped}.",
        result.CitiesAdded,
        result.SectionsAdded,
        result.SubsectionsAdded,
        result.Skipped.Count);

    if (result.Skipped.Count > 0)
    {
        logger.LogWarning(
            "Названия, занятые категорией с другим родителем, пропущены: {Names}.",
            string.Join(", ", result.Skipped));
    }
}

static async Task<int> CreateAdminAsync(WebApplication app)
{
    var logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Doska.CreateAdmin");

    var email = app.Configuration["Admin:Email"]?.Trim();
    var password = app.Configuration["Admin:Password"];

    if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
    {
        logger.LogError("Укажите корректный email в переменной Admin__Email.");
        return 1;
    }

    if (string.IsNullOrWhiteSpace(password))
    {
        logger.LogError("Укажите пароль в переменной Admin__Password.");
        return 1;
    }

    try
    {
        AuthService.ValidatePassword(password);
    }
    catch (ServiceException exception)
    {
        logger.LogError("{Message}", exception.Message);
        return 1;
    }

    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

    var created = await AdminSeeder.CreateOrUpdateAsync(context, passwordHasher, email, password, clock);

    logger.LogInformation(
        created
            ? "Администратор {Email} создан."
            : "Пользователь {Email} назначен администратором, пароль обновлён.",
        email);

    return 0;
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
    var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

    await DemoDataSeeder.SeedAsync(context, passwordHasher, clock);
    logger.LogInformation("Демонстрационные данные созданы.");
}

/// <summary>Точка входа приложения: требуется для интеграционных тестов.</summary>
public partial class Program;
