namespace Payslip4All.Application.DTOs.Pricing;

public class PayslipPricingSettingDto
{
    public Guid Id { get; set; }
    public decimal PricePerPayslip { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
