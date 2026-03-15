namespace Payslip4All.Domain.Entities;
public class PayslipLoanDeduction
{
    public Guid Id { get; private set; }
    public Guid PayslipId { get; set; }
    public Guid EmployeeLoanId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    
    public PayslipLoanDeduction()
    {
        Id = Guid.NewGuid();
    }
}
