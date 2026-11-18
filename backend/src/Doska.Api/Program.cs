using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

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

await ApplyMigrationsAsync(app);

app.MapControllers();

app.Run();

// Миграции применяются до обработки запросов: если база недоступна, приложение
// падает при старте, а не отвечает ошибками на каждый запрос.
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

/// <summary>Точка входа, открытая для интеграционных тестов.</summary>
public partial class Program;
