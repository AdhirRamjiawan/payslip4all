namespace Payslip4All.Infrastructure.HostedPayments;

public class FakeHostedPaymentOptions
{
    public const string SectionKey = "HostedPayments:Fake";

    public string ProviderKey { get; set; } = "fake";
    public string HostedPagePath { get; set; } = "/hosted-payments/fake";
}
