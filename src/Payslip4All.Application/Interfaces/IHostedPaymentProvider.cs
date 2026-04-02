using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces;

public interface IHostedPaymentProvider
{
    string ProviderKey { get; }

    Task<HostedPaymentSessionResult> StartHostedTopUpAsync(
        WalletTopUpAttempt attempt,
        Uri returnUrl,
        Uri? cancelUrl,
        CancellationToken cancellationToken = default);

    Task<HostedPaymentReturnResult> ValidateReturnAsync(
        WalletTopUpAttempt attempt,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default);
}

public sealed record HostedPaymentSessionResult(
    string RedirectUrl,
    string ProviderSessionReference,
    string ReturnCorrelationToken,
    DateTimeOffset? HostedPageDeadline);
