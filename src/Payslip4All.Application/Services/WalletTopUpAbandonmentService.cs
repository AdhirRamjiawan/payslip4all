using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

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
        var expiredAttempts = await _attemptRepository.GetPendingOrUnverifiedExpiredAsync(now, cancellationToken);

        foreach (var attempt in expiredAttempts)
        {
            var before = attempt.Status.ToString();
            attempt.MarkAbandoned(now);
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);

            await _decisionRepository.AddAsync(new OutcomeNormalizationDecision
            {
                AttemptId = attempt.Id,
                DecisionType = "AbandonmentTimeout",
                AppliedPrecedence = "AbandonmentThreshold",
                NormalizedOutcome = attempt.Status.ToString(),
                AuthoritativeOutcomeBefore = before,
                AuthoritativeOutcomeAfter = attempt.Status.ToString(),
                DecisionReasonCode = "abandonment_timeout",
                DecisionSummary = "Attempt automatically marked abandoned at the exact one-hour threshold.",
                WalletEffect = "NoCredit"
            }, cancellationToken);
        }
    }
}
