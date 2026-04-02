namespace Payslip4All.Application.Interfaces;

public interface IHostedPaymentProviderFactory
{
    IHostedPaymentProvider GetProvider(string providerKey);
    IHostedPaymentProvider GetDefault();
}
