using Doska.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Infrastructure;

/// <summary>
/// Преобразует ошибки сценариев и предметной области в ответы <c>ProblemDetails</c>
/// с подходящим HTTP-кодом.
/// </summary>
public sealed class ServiceExceptionHandler(ILogger<ServiceExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ServiceException serviceException => (StatusCodeFor(serviceException.Kind), serviceException.Message),
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, argumentException.Message),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
        {
            return false;
        }

        logger.LogInformation(exception, "Запрос отклонён: {Title}", title);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
            },
            cancellationToken);

        return true;
    }

    /// <summary>Сопоставляет категорию ошибки сценария с HTTP-кодом ответа.</summary>
    private static int StatusCodeFor(ServiceErrorKind kind) => kind switch
    {
        ServiceErrorKind.Validation => StatusCodes.Status400BadRequest,
        ServiceErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ServiceErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ServiceErrorKind.NotFound => StatusCodes.Status404NotFound,
        ServiceErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status409Conflict,
    };
}
