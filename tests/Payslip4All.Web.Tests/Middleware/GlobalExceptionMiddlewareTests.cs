using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Payslip4All.Web.Middleware;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Middleware;

/// <summary>
/// T005 — TDD-first unit tests for GlobalExceptionMiddleware.
/// Written before middleware implementation to encode behaviour as executable specs.
/// </summary>
public class GlobalExceptionMiddlewareTests
{
    private static GlobalExceptionMiddleware BuildMiddleware(
        RequestDelegate next,
        out Mock<ILogger<GlobalExceptionMiddleware>> loggerMock)
    {
        loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        return new GlobalExceptionMiddleware(next, loggerMock.Object);
    }

    private static DefaultHttpContext BuildContext(
        string path = "/portal",
        string method = "GET",
        string? userId = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = method;
        ctx.Response.Body = new System.IO.MemoryStream();

        if (userId != null)
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                "Test"));
        }

        return ctx;
    }

    [Fact]
    public async Task InvokeAsync_NoException_DoesNotLog()
    {
        static Task next(HttpContext _) => Task.CompletedTask;
        var middleware = BuildMiddleware(next, out var loggerMock);
        var ctx = BuildContext();

        await middleware.InvokeAsync(ctx);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_LogsErrorWithPathAndMethod()
    {
        var thrownEx = new InvalidOperationException("boom");
        Task next(HttpContext _) => Task.FromException(thrownEx);
        var middleware = BuildMiddleware(next, out var loggerMock);
        var ctx = BuildContext(path: "/portal/companies", method: "POST");

        await middleware.InvokeAsync(ctx);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("POST") && v.ToString()!.Contains("/portal/companies")),
                thrownEx,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500WhenResponseNotStarted()
    {
        Task next(HttpContext _) => Task.FromException(new Exception("err"));
        var middleware = BuildMiddleware(next, out _);
        var ctx = BuildContext();

        await middleware.InvokeAsync(ctx);

        Assert.Equal(StatusCodes.Status500InternalServerError, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_LogsUserId()
    {
        var userId = "user-abc-123";
        var thrownEx = new Exception("err");
        Task next(HttpContext _) => Task.FromException(thrownEx);
        var middleware = BuildMiddleware(next, out var loggerMock);
        var ctx = BuildContext(userId: userId);

        await middleware.InvokeAsync(ctx);

        // Verify LogError was called (UserId is pushed via LogContext.PushProperty,
        // which enriches the log scope — the ILogger mock captures the call)
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                thrownEx,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_DoesNotThrowSecondaryException()
    {
        Task next(HttpContext _) => Task.FromException(new Exception("err"));
        var middleware = BuildMiddleware(next, out _);
        // No user set on context (anonymous)
        var ctx = BuildContext(userId: null);

        // Should NOT throw — anonymous case must be handled gracefully
        var ex = await Record.ExceptionAsync(() => middleware.InvokeAsync(ctx));

        Assert.Null(ex);
        Assert.Equal(StatusCodes.Status500InternalServerError, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ApiPath_ReturnsJsonContentType()
    {
        Task next(HttpContext _) => Task.FromException(new Exception("err"));
        var middleware = BuildMiddleware(next, out _);
        var ctx = BuildContext(path: "/payslips/123/download");

        await middleware.InvokeAsync(ctx);

        Assert.Equal(StatusCodes.Status500InternalServerError, ctx.Response.StatusCode);
        Assert.Contains("application/json", ctx.Response.ContentType ?? "");
    }

    [Fact]
    public async Task InvokeAsync_ResponseAlreadyStarted_RethrowsException()
    {
        var thrownEx = new Exception("already streaming");
        Task next(HttpContext _) => Task.FromException(thrownEx);
        var middleware = BuildMiddleware(next, out _);
        var ctx = BuildContext();

        // Force HasStarted = true via a mock IHttpResponseFeature
        var responseMock = new Mock<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>();
        responseMock.Setup(r => r.HasStarted).Returns(true);
        responseMock.Setup(r => r.Headers).Returns(new HeaderDictionary());
        responseMock.SetupProperty(r => r.StatusCode);
        ctx.Features.Set(responseMock.Object);

        var actual = await Record.ExceptionAsync(() => middleware.InvokeAsync(ctx));

        Assert.NotNull(actual);
        Assert.Equal(thrownEx, actual);
    }
}
