using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.DTOs.Wallet;

public sealed class SiteAdministratorPaymentReviewQueryDto
{
    public bool RequestingUserIsSiteAdministrator { get; set; }
    public Guid? AttemptId { get; set; }
    public Guid? PaymentConfirmationRecordId { get; set; }
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public WalletTopUpAttemptStatus? Outcome { get; set; }
    public bool ConflictsOnly { get; set; }
    public bool UnmatchedOnly { get; set; }
}

public sealed class SiteAdministratorPaymentReviewDto
{
    public Guid? WalletTopUpAttemptId { get; set; }
    public Guid? UnmatchedPaymentReturnRecordId { get; set; }
    public bool IsUnmatchedReturn { get; set; }
    public Guid? OwnerUserId { get; set; }
    public decimal? RequestedAmount { get; set; }
    public decimal? ConfirmedChargedAmount { get; set; }
    public WalletTopUpAttemptStatus? Status { get; set; }
    public bool CreditedWallet { get; set; }
    public Guid? WalletActivityId { get; set; }
    public Guid? PaymentConfirmationRecordId { get; set; }
    public string? EvidenceSourceChannel { get; set; }
    public string? DecisionType { get; set; }
    public string? DecisionReasonCode { get; set; }
    public string? DecisionSummary { get; set; }
    public bool ConflictWithAcceptedFinalOutcome { get; set; }
    public string? CorrelationDisposition { get; set; }
    public string? MerchantPaymentReference { get; set; }
    public string? ProviderPaymentReference { get; set; }
    public DateTimeOffset? AttemptCreatedAt { get; set; }
    public DateTimeOffset? EvidenceReceivedAt { get; set; }
    public DateTimeOffset? DecisionAt { get; set; }
    public string? SafePayloadSnapshot { get; set; }
}
