using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.DTOs.Wallet;

public class WalletActivityDto
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public WalletActivityType ActivityType { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Description { get; set; }
    public decimal BalanceAfterActivity { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
