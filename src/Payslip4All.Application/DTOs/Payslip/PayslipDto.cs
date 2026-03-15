namespace Payslip4All.Application.DTOs.Payslip;
public class PayslipDto
{
    public Guid Id { get; set; }
    public int PayPeriodMonth { get; set; }
    public int PayPeriodYear { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal UifDeduction { get; set; }
    public decimal TotalLoanDeductions { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public List<PayslipLoanDeductionDto> LoanDeductions { get; set; } = new();
}
