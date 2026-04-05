using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces;

public interface IWalletTopUpAbandonmentService
{
    Task AbandonExpiredAttemptsAsync(CancellationToken cancellationToken = default);
    Task<bool> ReconcileAttemptIfDueAsync(WalletTopUpAttempt attempt, string triggerSource, CancellationToken cancellationToken = default);
}
