using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Controllers;

/// <summary>Проверочный эндпоинт каркаса: подтверждает, что API отвечает.</summary>
[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public HealthController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        return Ok(new HealthResponse(_environment.EnvironmentName, version));
    }
}

/// <summary>Ответ проверочного эндпоинта.</summary>
public sealed record HealthResponse(string Environment, string Version);
