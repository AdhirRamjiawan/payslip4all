using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.DTOs.Wallet;

public class StartWalletTopUpResultDto
{
    public Guid WalletTopUpAttemptId { get; set; }
    public string RedirectUrl { get; set; } = string.Empty;
    public WalletTopUpAttemptStatus Status { get; set; }
    public DateTimeOffset? HostedPageDeadline { get; set; }
}
