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
}
