namespace Payslip4All.Domain.Entities;
public class Payslip
{
    public Guid Id { get; private set; }
    public int PayPeriodMonth { get; set; }
    public int PayPeriodYear { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal UifDeduction { get; set; }
    public decimal TotalLoanDeductions { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public decimal ChargedAmount { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public List<PayslipLoanDeduction> LoanDeductions { get; set; } = new();
    public Employee Employee { get; set; } = null!;

    public Payslip()
    {
        Id = Guid.NewGuid();
        GeneratedAt = DateTimeOffset.UtcNow;
    }
}
