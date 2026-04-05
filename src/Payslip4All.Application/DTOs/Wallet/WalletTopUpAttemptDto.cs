using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.DTOs.Wallet;

public class WalletTopUpAttemptDto
{
    public Guid Id { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? ConfirmedChargedAmount { get; set; }
    public WalletTopUpAttemptStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? RedirectedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public DateTimeOffset? AbandonedAt { get; set; }
    public DateTimeOffset? HostedPageDeadline { get; set; }
    public DateTimeOffset? NextReconciliationDueAt { get; set; }
    public DateTimeOffset AbandonAfterUtc { get; set; }
    public DateTimeOffset? AuthoritativeOutcomeAcceptedAt { get; set; }
    public Guid? CreditedWalletActivityId { get; set; }
    public Guid? AuthoritativeEvidenceId { get; set; }
    public string? FailureMessage { get; set; }
    public string? OutcomeMessage { get; set; }
}
