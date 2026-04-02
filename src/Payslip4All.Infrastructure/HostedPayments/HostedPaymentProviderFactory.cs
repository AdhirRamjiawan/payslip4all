using Payslip4All.Application.Interfaces;

namespace Payslip4All.Infrastructure.HostedPayments;

public class HostedPaymentProviderFactory : IHostedPaymentProviderFactory
{
    private readonly IReadOnlyDictionary<string, IHostedPaymentProvider> _providers;

    public HostedPaymentProviderFactory(IEnumerable<IHostedPaymentProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public IHostedPaymentProvider GetProvider(string providerKey)
        => _providers.TryGetValue(providerKey, out var provider)
            ? provider
            : throw new InvalidOperationException($"Hosted payment provider '{providerKey}' is not registered.");

    public IHostedPaymentProvider GetDefault()
        => _providers.Values.FirstOrDefault()
           ?? throw new InvalidOperationException("No hosted payment providers are registered.");
}
