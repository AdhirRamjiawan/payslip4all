namespace Payslip4All.Application.DTOs.Wallet;

public class StartWalletTopUpCommand
{
    public Guid UserId { get; set; }
    public decimal RequestedAmount { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public string? CancelUrl { get; set; }
}
