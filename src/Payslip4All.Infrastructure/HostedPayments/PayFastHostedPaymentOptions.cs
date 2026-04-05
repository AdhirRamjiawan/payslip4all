namespace Payslip4All.Infrastructure.HostedPayments;

public sealed class PayFastHostedPaymentOptions
{
    public const string SectionKey = "HostedPayments:PayFast";

    public string ProviderKey { get; set; } = "payfast";
    public bool UseSandbox { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string? Passphrase { get; set; }
    public string SandboxBaseUrl { get; set; } = "https://sandbox.payfast.co.za/eng/process";
    public string LiveBaseUrl { get; set; } = "https://www.payfast.co.za/eng/process";
    public string SandboxValidationUrl { get; set; } = "https://sandbox.payfast.co.za/eng/query/validate";
    public string LiveValidationUrl { get; set; } = "https://www.payfast.co.za/eng/query/validate";
    public string PublicNotifyUrl { get; set; } = string.Empty;
    public string ItemName { get; set; } = "Payslip4All wallet top-up";

    public string GetProcessUrl()
        => UseSandbox ? SandboxBaseUrl : LiveBaseUrl;

    public string GetValidationUrl()
        => UseSandbox ? SandboxValidationUrl : LiveValidationUrl;

    public void ValidateForStart()
    {
        if (string.IsNullOrWhiteSpace(MerchantId) || string.IsNullOrWhiteSpace(MerchantKey))
            throw new InvalidOperationException("PayFast merchant credentials are not configured.");

        if (!Uri.TryCreate(PublicNotifyUrl, UriKind.Absolute, out var notifyUrl)
            || notifyUrl.Scheme != Uri.UriSchemeHttps
            || notifyUrl.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PayFast notify_url must be a public HTTPS address.");
        }

        if (notifyUrl.Host.EndsWith("payfast.co.za", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PayFast notify_url must point to your public Payslip4All callback endpoint, not a PayFast URL.");
    }
}
