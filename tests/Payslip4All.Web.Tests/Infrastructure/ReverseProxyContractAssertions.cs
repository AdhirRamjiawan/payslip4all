using System.Diagnostics;
using System.Net;

namespace Payslip4All.Web.Tests.Infrastructure;

internal static class ReverseProxyContractAssertions
{
    internal static void AssertWrongHost(HttpStatusCode statusCode)
    {
        Assert.Equal((HttpStatusCode)421, statusCode);
    }

    internal static void AssertServiceUnavailable(HttpStatusCode statusCode, string body)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, statusCode);
        Assert.Equal("Service temporarily unavailable.", body);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.Ordinal);
        Assert.DoesNotContain("8080", body, StringComparison.Ordinal);
    }

    internal static void AssertCompletedWithin(Stopwatch stopwatch, TimeSpan deadline)
    {
        Assert.True(
            stopwatch.Elapsed <= deadline,
            $"Expected the operation to complete within {deadline.TotalSeconds:0} seconds, but it took {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
    }
}
