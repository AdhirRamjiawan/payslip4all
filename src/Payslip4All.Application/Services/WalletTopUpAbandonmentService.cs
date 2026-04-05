using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.Services;

public sealed class WalletTopUpAbandonmentService : IWalletTopUpAbandonmentService
{
    private readonly IWalletTopUpAttemptRepository _attemptRepository;
    private readonly IOutcomeNormalizationDecisionRepository _decisionRepository;
    private readonly ITimeProvider _timeProvider;

    public WalletTopUpAbandonmentService(
        IWalletTopUpAttemptRepository attemptRepository,
        IOutcomeNormalizationDecisionRepository decisionRepository,
        ITimeProvider timeProvider)
    {
        _attemptRepository = attemptRepository;
        _decisionRepository = decisionRepository;
        _timeProvider = timeProvider;
    }

    public async Task AbandonExpiredAttemptsAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.UtcNow;
        var dueAttempts = await _attemptRepository.GetDueForReconciliationAsync(now, cancellationToken);

        foreach (var attempt in dueAttempts)
            await ReconcileAttemptIfDueAsync(attempt, "ScheduledSweep", cancellationToken);
    }

    public async Task<bool> ReconcileAttemptIfDueAsync(WalletTopUpAttempt attempt, string triggerSource, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.UtcNow;
        if (!attempt.NextReconciliationDueAt.HasValue || attempt.NextReconciliationDueAt.Value > now)
            return false;

        var before = attempt.Status;

        if (attempt.Status is WalletTopUpAttemptStatus.Pending or WalletTopUpAttemptStatus.NotConfirmed)
        {
            attempt.MarkExpired("expired", "Top-up not confirmed", now, now.AddMinutes(1));
        }
        else if (attempt.Status == WalletTopUpAttemptStatus.Expired)
        {
            attempt.MarkAbandoned(now);
        }
        else
        {
            return false;
        }

        attempt.RecordReconciled(now);
        await _attemptRepository.UpdateAsync(attempt, cancellationToken);
        await _decisionRepository.AddAsync(new OutcomeNormalizationDecision
        {
            AttemptId = attempt.Id,
            DecisionType = "Reconciliation",
            TriggerSource = triggerSource,
            AppliedPrecedence = "DueAttempt",
            NormalizedOutcome = attempt.Status.ToString(),
            AuthoritativeOutcomeBefore = before.ToString(),
            AuthoritativeOutcomeAfter = attempt.Status.ToString(),
            DecisionReasonCode = attempt.Status == WalletTopUpAttemptStatus.Expired ? "expired" : "abandoned",
            DecisionSummary = attempt.Status == WalletTopUpAttemptStatus.Expired
                ? "Attempt expired after the hosted payment deadline passed without authoritative confirmation."
                : "Attempt abandoned after follow-up reconciliation still found no authoritative confirmation.",
            SupersededNotConfirmed = before == WalletTopUpAttemptStatus.NotConfirmed,
            WalletEffect = "NoCredit"
        }, cancellationToken);

        return true;
    }
}
