using Payslip4All.Application.Interfaces;

namespace Payslip4All.Web.Endpoints;

public static class PayFastNotifyEndpoint
{
    public static async Task<IResult> HandleAsync(HttpRequest request, IWalletTopUpService walletTopUpService, CancellationToken cancellationToken)
    {
        var form = await request.ReadFormAsync(cancellationToken);
        var payload = form.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        await walletTopUpService.ProcessAuthoritativeCallbackAsync("payfast", payload, cancellationToken);
        return Results.Ok();
    }
}
