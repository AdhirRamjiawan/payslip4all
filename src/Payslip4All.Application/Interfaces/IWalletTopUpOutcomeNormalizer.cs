using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces;

public interface IWalletTopUpOutcomeNormalizer
{
    Task<HostedPaymentReturnResolutionDto> NormalizeAsync(
        PaymentReturnEvidence evidence,
        WalletTopUpAttempt? matchedAttempt,
        CancellationToken cancellationToken = default);
}
