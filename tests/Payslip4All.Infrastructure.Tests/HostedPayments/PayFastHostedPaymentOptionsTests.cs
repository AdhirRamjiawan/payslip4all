using Payslip4All.Infrastructure.HostedPayments;

namespace Payslip4All.Infrastructure.Tests.HostedPayments;

public class PayFastHostedPaymentOptionsTests
{
    [Fact]
    public void SectionKey_IsHostedPaymentsPayFast()
        => Assert.Equal("HostedPayments:PayFast", PayFastHostedPaymentOptions.SectionKey);

    [Fact]
    public void DefaultConfiguration_UsesLiveEndpoints_AndExpectedProviderKey()
    {
        var options = new PayFastHostedPaymentOptions();

        Assert.Equal("payfast", options.ProviderKey);
        Assert.False(options.UseSandbox);
        Assert.Equal("https://www.payfast.co.za/eng/process", options.GetProcessUrl());
        Assert.Equal("https://www.payfast.co.za/eng/query/validate", options.GetValidationUrl());
    }

    [Fact]
    public void SandboxConfiguration_UsesSandboxEndpoints()
    {
        var options = new PayFastHostedPaymentOptions
        {
            UseSandbox = true
        };

        Assert.Equal("https://sandbox.payfast.co.za/eng/process", options.GetProcessUrl());
        Assert.Equal("https://sandbox.payfast.co.za/eng/query/validate", options.GetValidationUrl());
    }

    [Fact]
    public void ValidateForStart_WhenMerchantCredentialsAreMissing_Throws()
    {
        var options = new PayFastHostedPaymentOptions
        {
            PublicNotifyUrl = "https://example.test/api/payments/payfast/notify"
        };

        var ex = Assert.Throws<InvalidOperationException>(options.ValidateForStart);

        Assert.Contains("merchant credentials", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://localhost:5000/api/payments/payfast/notify")]
    [InlineData("https://localhost/api/payments/payfast/notify")]
    [InlineData("https://sandbox.payfast.co.za/eng/process")]
    public void ValidateForStart_WhenPublicNotifyUrlIsInvalid_Throws(string notifyUrl)
    {
        var options = new PayFastHostedPaymentOptions
        {
            MerchantId = "10000100",
            MerchantKey = "46f0cd694581a",
            PublicNotifyUrl = notifyUrl
        };

        Assert.Throws<InvalidOperationException>(options.ValidateForStart);
    }

    [Fact]
    public void ValidateForStart_WithPublicHttpsNotifyUrl_DoesNotThrow()
    {
        var options = new PayFastHostedPaymentOptions
        {
            MerchantId = "10000100",
            MerchantKey = "46f0cd694581a",
            Passphrase = "sandbox-passphrase",
            PublicNotifyUrl = "https://notify.example.test/api/payments/payfast/notify"
        };

        options.ValidateForStart();
    }
}
