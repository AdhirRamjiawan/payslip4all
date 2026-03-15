namespace Payslip4All.Application.DTOs.Loan;
public class CreateLoanCommand
{
    public string Description { get; set; } = "";
    public decimal TotalLoanAmount { get; set; }
    public int NumberOfTerms { get; set; }
    public decimal MonthlyDeductionAmount { get; set; }
    public DateOnly PaymentStartDate { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid UserId { get; set; }
}
