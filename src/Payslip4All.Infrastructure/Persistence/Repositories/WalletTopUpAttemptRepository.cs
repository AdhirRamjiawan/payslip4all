using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Domain.Services;

namespace Payslip4All.Infrastructure.Persistence.Repositories;

public class WalletTopUpAttemptRepository : IWalletTopUpAttemptRepository
{
    private readonly PayslipDbContext _db;

    public WalletTopUpAttemptRepository(PayslipDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _db.WalletTopUpAttempts.AddAsync(attempt, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default)
    {
        _db.WalletTopUpAttempts.Update(attempt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WalletTopUpAttempt?> GetByIdAsync(Guid attemptId, Guid userId, CancellationToken cancellationToken = default)
        => await _db.WalletTopUpAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<WalletTopUpAttempt>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var results = await _db.WalletTopUpAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        return results.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<WalletTopUpAttempt?> GetByCorrelationTokenAsync(string token, CancellationToken cancellationToken = default)
        => await _db.WalletTopUpAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ReturnCorrelationToken == token, cancellationToken);

    public async Task<IReadOnlyList<WalletTopUpAttempt>> GetPendingOrUnverifiedExpiredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        var attempts = await _db.WalletTopUpAttempts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return attempts
            .Where(a => (a.Status == WalletTopUpAttemptStatus.Pending || a.Status == WalletTopUpAttemptStatus.Unverified)
                        && a.AbandonAfterUtc <= cutoff)
            .OrderBy(a => a.AbandonAfterUtc)
            .ToList();
    }

    public async Task<WalletTopUpSettlementResult> SettleSuccessfulAsync(WalletTopUpAttempt attempt, CancellationToken cancellationToken = default)
    {
        var startedTransaction = _db.Database.CurrentTransaction == null;
        if (startedTransaction)
            await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var persistedAttempt = await _db.WalletTopUpAttempts.FirstAsync(
                a => a.Id == attempt.Id && a.UserId == attempt.UserId,
                cancellationToken);

            if (persistedAttempt.CreditedWalletActivityId.HasValue)
            {
                var existingWallet = await _db.Wallets.FirstAsync(w => w.UserId == attempt.UserId, cancellationToken);
                return new WalletTopUpSettlementResult
                {
                    WalletId = existingWallet.Id,
                    WalletActivityId = persistedAttempt.CreditedWalletActivityId.Value,
                    WalletBalance = existingWallet.CurrentBalance,
                    CreditedNow = false
                };
            }

            if (!attempt.ConfirmedChargedAmount.HasValue)
                throw new InvalidOperationException("A confirmed charged amount is required before settlement.");

            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == attempt.UserId, cancellationToken);
            var isNewWallet = wallet == null;
            wallet ??= Wallet.CreateForUser(attempt.UserId);

            if (isNewWallet)
                await _db.Wallets.AddAsync(wallet, cancellationToken);

            wallet.CurrentBalance = WalletCalculator.CalculateBalanceAfterCredit(wallet.CurrentBalance, attempt.ConfirmedChargedAmount.Value);
            wallet.UpdatedAt = DateTimeOffset.UtcNow;
            wallet.EnsureValid();

            var activity = new WalletActivity
            {
                WalletId = wallet.Id,
                ActivityType = WalletActivityType.Credit,
                Amount = attempt.ConfirmedChargedAmount.Value,
                Description = WalletActivity.HostedCardTopUpDescription,
                ReferenceType = WalletActivity.WalletTopUpReferenceType,
                ReferenceId = attempt.Id.ToString(),
                BalanceAfterActivity = wallet.CurrentBalance
            };
            activity.EnsureValid();
            await _db.WalletActivities.AddAsync(activity, cancellationToken);

            persistedAttempt.MarkCompleted(
                attempt.ConfirmedChargedAmount.Value,
                attempt.ProviderPaymentReference,
                attempt.AuthoritativeOutcomeAcceptedAt ?? attempt.LastValidatedAt ?? DateTimeOffset.UtcNow,
                activity.Id);
            persistedAttempt.AuthoritativeEvidenceId = attempt.AuthoritativeEvidenceId;
            persistedAttempt.LastEvidenceReceivedAt = attempt.LastEvidenceReceivedAt;
            persistedAttempt.LastEvaluatedAt = attempt.LastEvaluatedAt ?? attempt.LastValidatedAt;
            persistedAttempt.AuthoritativeOutcomeAcceptedAt = attempt.AuthoritativeOutcomeAcceptedAt ?? attempt.LastValidatedAt ?? DateTimeOffset.UtcNow;
            persistedAttempt.OutcomeReasonCode = null;
            persistedAttempt.OutcomeMessage = null;
            persistedAttempt.FailureCode = null;
            persistedAttempt.FailureMessage = null;

            await _db.SaveChangesAsync(cancellationToken);

            if (startedTransaction)
                await _db.Database.CommitTransactionAsync(cancellationToken);

            return new WalletTopUpSettlementResult
            {
                WalletId = wallet.Id,
                WalletActivityId = activity.Id,
                WalletBalance = wallet.CurrentBalance,
                CreditedNow = true
            };
        }
        catch
        {
            if (startedTransaction)
                await _db.Database.RollbackTransactionAsync(cancellationToken);

            throw;
        }
    }
}
