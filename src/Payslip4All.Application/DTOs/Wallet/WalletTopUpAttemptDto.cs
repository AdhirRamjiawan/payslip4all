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
    public DateTimeOffset? HostedPageDeadline { get; set; }
    public Guid? CreditedWalletActivityId { get; set; }
    public string? FailureMessage { get; set; }
}
