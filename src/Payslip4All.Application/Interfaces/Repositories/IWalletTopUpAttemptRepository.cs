using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces.Repositories;

public interface IWalletTopUpAttemptRepository
{
    Task AddAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default);
    Task UpdateAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default);
    Task<WalletTopUpAttempt?> GetByIdAsync(Guid attemptId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTopUpAttempt>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WalletTopUpSettlementResult> SettleSuccessfulAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default);
}

public class WalletTopUpSettlementResult
{
    public Guid WalletId { get; set; }
    public Guid WalletActivityId { get; set; }
    public decimal WalletBalance { get; set; }
    public bool CreditedNow { get; set; }
}
