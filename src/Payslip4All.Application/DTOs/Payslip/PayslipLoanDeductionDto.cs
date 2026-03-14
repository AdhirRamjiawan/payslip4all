namespace Payslip4All.Application.DTOs.Payslip;
public class PayslipLoanDeductionDto
{
    public Guid EmployeeLoanId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}
