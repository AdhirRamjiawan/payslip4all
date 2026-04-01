namespace Payslip4All.Application.DTOs.Wallet;

public class AddWalletCreditCommand
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
}
