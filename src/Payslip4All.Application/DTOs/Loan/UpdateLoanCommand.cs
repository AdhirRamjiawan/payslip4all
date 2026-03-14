namespace Payslip4All.Application.DTOs.Loan;
public class UpdateLoanCommand : CreateLoanCommand
{
    public Guid LoanId { get; set; }
}
