namespace Payslip4All.Application.Interfaces;
public record PayslipDocument(
    string CompanyName,
    string? CompanyAddress,
    string EmployeeName,
    string EmployeeNumber,
    string Occupation,
    string PayPeriod,
    decimal GrossEarnings,
    decimal UifDeduction,
    IReadOnlyList<(string Description, decimal Amount)> LoanDeductions,
    decimal TotalDeductions,
    decimal NetPay
);
public interface IPdfGenerationService
{
    byte[] GeneratePayslip(PayslipDocument document);
}
