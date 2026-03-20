using Serilog.Context;
using System.Security.Claims;

namespace Payslip4All.Web.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var path = context.Request.Path.ToString();
            var method = context.Request.Method;

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("RequestPath", path))
            using (LogContext.PushProperty("RequestMethod", method))
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {RequestPath}", method, path);
            }

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var isApiPath = path.StartsWith("/payslips", StringComparison.OrdinalIgnoreCase);
                var acceptHeader = context.Request.Headers["Accept"].ToString();

                if (isApiPath || acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"An unexpected error occurred.\"}");
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("An unexpected error occurred. Please try again later.");
                }
            }
            else
            {
                throw;
            }
        }
    }
}
