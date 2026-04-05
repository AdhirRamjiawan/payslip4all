using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.DTOs.Wallet;

public class FinalizedWalletTopUpResultDto
{
    public Guid WalletTopUpAttemptId { get; set; }
    public WalletTopUpAttemptStatus Status { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? ConfirmedChargedAmount { get; set; }
    public decimal WalletBalance { get; set; }
    public Guid? CreditedWalletActivityId { get; set; }
    public bool CreditedWallet { get; set; }
    public string DisplayMessage { get; set; } = string.Empty;
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string? OutcomeReasonCode { get; set; }
    public string? OutcomeMessage { get; set; }
    public DateTimeOffset? AuthoritativeOutcomeAcceptedAt { get; set; }
    public DateTimeOffset? HostedPageDeadline { get; set; }
    public DateTimeOffset? NextReconciliationDueAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public DateTimeOffset? AbandonedAt { get; set; }
    public Guid? AuthoritativeEvidenceId { get; set; }
}
