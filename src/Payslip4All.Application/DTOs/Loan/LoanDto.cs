namespace Payslip4All.Application.DTOs.Loan;
public class LoanDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public decimal TotalLoanAmount { get; set; }
    public int NumberOfTerms { get; set; }
    public decimal MonthlyDeductionAmount { get; set; }
    public DateOnly PaymentStartDate { get; set; }
    public int TermsCompleted { get; set; }
    public string Status { get; set; } = "";
    public Guid EmployeeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
