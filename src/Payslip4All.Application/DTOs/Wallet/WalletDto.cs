namespace Payslip4All.Application.DTOs.Wallet;

public class WalletDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal CurrentPayslipPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<WalletActivityDto> Activities { get; set; } = new();
}
