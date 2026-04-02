namespace Payslip4All.Domain.Entities;

public class OutcomeNormalizationDecision
{
    public Guid Id { get; private set; }
    public Guid? AttemptId { get; set; }
    public Guid? PaymentReturnEvidenceId { get; set; }
    public Guid? UnmatchedPaymentReturnRecordId { get; set; }
    public string DecisionType { get; set; } = "EvidenceEvaluation";
    public string AppliedPrecedence { get; set; } = string.Empty;
    public string NormalizedOutcome { get; set; } = string.Empty;
    public string? AuthoritativeOutcomeBefore { get; set; }
    public string? AuthoritativeOutcomeAfter { get; set; }
    public string DecisionReasonCode { get; set; } = string.Empty;
    public string DecisionSummary { get; set; } = string.Empty;
    public bool SupersededAbandonment { get; set; }
    public bool ConflictWithAcceptedFinalOutcome { get; set; }
    public string WalletEffect { get; set; } = "NoCredit";
    public Guid? WalletActivityId { get; set; }
    public DateTimeOffset DecidedAt { get; private set; }

    public OutcomeNormalizationDecision()
    {
        Id = Guid.NewGuid();
        DecidedAt = DateTimeOffset.UtcNow;
    }
}
