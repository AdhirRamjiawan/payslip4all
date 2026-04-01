namespace Payslip4All.Application.DTOs.Payslip;
public class PayslipResult
{
    public bool Success { get; set; }
    public PayslipDto? PayslipDto { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsDuplicate { get; set; }
    public decimal ChargedAmount { get; set; }
    public bool InsufficientFunds { get; set; }
}
