namespace Payslip4All.Web.Endpoints;

public static class HealthEndpoint
{
    public const string Path = "/health";
    public const string HealthyStatus = "Healthy";

    public static IResult Handle()
    {
        return Results.Ok(new
        {
            status = HealthyStatus,
            utc = DateTimeOffset.UtcNow
        });
    }
}
