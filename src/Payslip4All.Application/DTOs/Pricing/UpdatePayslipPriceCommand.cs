namespace Payslip4All.Application.DTOs.Pricing;

public class UpdatePayslipPriceCommand
{
    public decimal PricePerPayslip { get; set; }
    public string? UpdatedByUserId { get; set; }
}
