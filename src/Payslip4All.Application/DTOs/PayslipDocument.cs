namespace Payslip4All.Application.DTOs;

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
    decimal NetPay,
    // ── New SA-compliance fields (appended after NetPay — backward-compatible) ──
    string? CompanyUifNumber = null,
    string? CompanySarsPayeNumber = null,
    string EmployeeIdNumber = "",
    DateOnly EmployeeStartDate = default,
    string? EmployeeUifReference = null,
    DateOnly PaymentDate = default
);
