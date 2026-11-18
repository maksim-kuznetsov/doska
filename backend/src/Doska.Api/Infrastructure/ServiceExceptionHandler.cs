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
            ServiceException serviceException => (
                (int)serviceException.Kind switch
                {
                    0 => StatusCodes.Status400BadRequest,
                    1 => StatusCodes.Status401Unauthorized,
                    2 => StatusCodes.Status403Forbidden,
                    3 => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status409Conflict,
                },
                serviceException.Message),
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, argumentException.Message),
            InvalidOperationException invalidOperationException => (
                StatusCodes.Status409Conflict,
                invalidOperationException.Message),
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
}
