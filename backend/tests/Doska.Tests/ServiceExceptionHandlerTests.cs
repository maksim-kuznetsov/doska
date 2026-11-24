using Doska.Api.Infrastructure;
using Doska.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Doska.Tests;

/// <summary>Сопоставление категории ошибки сценария с HTTP-кодом ответа.</summary>
public sealed class ServiceExceptionHandlerTests
{
    [Theory]
    [InlineData(ServiceErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ServiceErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ServiceErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ServiceErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ServiceErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData((ServiceErrorKind)99, StatusCodes.Status409Conflict)]
    public async Task Service_error_kind_maps_to_expected_status_code(ServiceErrorKind kind, int expected)
    {
        var handler = new ServiceExceptionHandler(NullLogger<ServiceExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new ServiceException(kind, "ошибка"),
            TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(expected, context.Response.StatusCode);
    }
}
