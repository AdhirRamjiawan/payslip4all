using Payslip4All.Application.DTOs.Payslip;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Services;
namespace Payslip4All.Application.Services;
public class PayslipGenerationService : IPayslipService
{
    private readonly IPayslipRepository _payslipRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ILoanRepository _loanRepo;
    private readonly IPdfGenerationService _pdfService;
    private readonly IUnitOfWork _unitOfWork;

    public PayslipGenerationService(
        IPayslipRepository payslipRepo, IEmployeeRepository employeeRepo,
        ILoanRepository loanRepo, IPdfGenerationService pdfService, IUnitOfWork unitOfWork)
    {
        _payslipRepo = payslipRepo;
        _employeeRepo = employeeRepo;
        _loanRepo = loanRepo;
        _pdfService = pdfService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PayslipResult> PreviewPayslipAsync(PreviewPayslipQuery query)
    {
        var employee = await _employeeRepo.GetByIdWithLoansAsync(query.EmployeeId, query.UserId);
        if (employee == null) return new PayslipResult { Success = false, ErrorMessage = "Employee not found." };
        if (employee.MonthlyGrossSalary <= 0) return new PayslipResult { Success = false, ErrorMessage = "Employee has no salary." };

        var activeLoans = employee.Loans.Where(l => l.IsActiveForPeriod(query.PayPeriodMonth, query.PayPeriodYear)).ToList();
        var uif = PayslipCalculator.CalculateUifDeduction(employee.MonthlyGrossSalary);
        var loanAmounts = activeLoans.Select(l => l.MonthlyDeductionAmount);
        var totalDeductions = PayslipCalculator.CalculateTotalDeductions(uif, loanAmounts);
        var netPay = PayslipCalculator.CalculateNetPay(employee.MonthlyGrossSalary, uif, loanAmounts);

        return new PayslipResult
        {
            Success = true,
            PayslipDto = new PayslipDto
            {
                EmployeeId = employee.Id,
                PayPeriodMonth = query.PayPeriodMonth,
                PayPeriodYear = query.PayPeriodYear,
                GrossEarnings = employee.MonthlyGrossSalary,
                UifDeduction = uif,
                TotalLoanDeductions = activeLoans.Sum(l => l.MonthlyDeductionAmount),
                TotalDeductions = totalDeductions,
                NetPay = netPay,
                LoanDeductions = activeLoans.Select(l => new PayslipLoanDeductionDto
                {
                    EmployeeLoanId = l.Id, Description = l.Description, Amount = l.MonthlyDeductionAmount
                }).ToList()
            }
        };
    }

    public async Task<PayslipResult> GeneratePayslipAsync(GeneratePayslipCommand command)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var exists = await _payslipRepo.ExistsAsync(command.EmployeeId, command.PayPeriodMonth, command.PayPeriodYear);
            if (exists && !command.OverwriteExisting)
                return new PayslipResult { Success = false, IsDuplicate = true, ErrorMessage = "Payslip already exists for this period." };

            if (exists && command.OverwriteExisting)
            {
                var existing = (await _payslipRepo.GetAllByEmployeeIdAsync(command.EmployeeId, command.UserId))
                    .FirstOrDefault(p => p.PayPeriodMonth == command.PayPeriodMonth && p.PayPeriodYear == command.PayPeriodYear);
                if (existing != null) await _payslipRepo.DeleteAsync(existing);
            }

            var employee = await _employeeRepo.GetByIdWithLoansAsync(command.EmployeeId, command.UserId);
            if (employee == null) return new PayslipResult { Success = false, ErrorMessage = "Employee not found." };
            if (employee.MonthlyGrossSalary <= 0) return new PayslipResult { Success = false, ErrorMessage = "Employee has no salary." };

            var activeLoans = employee.Loans.Where(l => l.IsActiveForPeriod(command.PayPeriodMonth, command.PayPeriodYear)).ToList();
            var uif = PayslipCalculator.CalculateUifDeduction(employee.MonthlyGrossSalary);
            var loanAmounts = activeLoans.Select(l => l.MonthlyDeductionAmount).ToList();
            var totalLoanDeductions = loanAmounts.Sum();
            var totalDeductions = PayslipCalculator.CalculateTotalDeductions(uif, loanAmounts);
            var netPay = PayslipCalculator.CalculateNetPay(employee.MonthlyGrossSalary, uif, loanAmounts);

            var payslip = new Payslip
            {
                EmployeeId = employee.Id, PayPeriodMonth = command.PayPeriodMonth,
                PayPeriodYear = command.PayPeriodYear, GrossEarnings = employee.MonthlyGrossSalary,
                UifDeduction = uif, TotalLoanDeductions = totalLoanDeductions,
                TotalDeductions = totalDeductions, NetPay = netPay
            };

            foreach (var loan in activeLoans)
            {
                payslip.LoanDeductions.Add(new PayslipLoanDeduction
                {
                    PayslipId = payslip.Id, EmployeeLoanId = loan.Id,
                    Description = loan.Description, Amount = loan.MonthlyDeductionAmount
                });
                loan.IncrementTermsCompleted();
            }

            var monthName = new System.Globalization.DateTimeFormatInfo().GetMonthName(command.PayPeriodMonth);
            var doc = new PayslipDocument(
                CompanyName: employee.Company?.Name ?? "Company",
                CompanyAddress: employee.Company?.Address,
                EmployeeName: $"{employee.FirstName} {employee.LastName}",
                EmployeeNumber: employee.EmployeeNumber,
                Occupation: employee.Occupation,
                PayPeriod: $"{monthName} {command.PayPeriodYear}",
                GrossEarnings: employee.MonthlyGrossSalary,
                UifDeduction: uif,
                LoanDeductions: activeLoans.Select(l => (l.Description, l.MonthlyDeductionAmount)).ToList(),
                TotalDeductions: totalDeductions,
                NetPay: netPay
            );
            payslip.PdfContent = _pdfService.GeneratePayslip(doc);

            await _payslipRepo.AddAsync(payslip);

            foreach (var loan in activeLoans)
                await _loanRepo.UpdateAsync(loan);

            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            await _unitOfWork.CommitTransactionAsync();

            return new PayslipResult { Success = true, PayslipDto = ToDto(payslip) };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<PayslipDto>> GetPayslipsForEmployeeAsync(Guid employeeId, Guid userId)
    {
        var payslips = await _payslipRepo.GetAllByEmployeeIdAsync(employeeId, userId);
        return payslips.Select(ToDto).ToList();
    }

    public async Task<byte[]?> GetPdfAsync(Guid payslipId, Guid userId)
    {
        var payslip = await _payslipRepo.GetByIdAsync(payslipId, userId);
        return payslip?.PdfContent;
    }

    private static PayslipDto ToDto(Payslip p) => new()
    {
        Id = p.Id, PayPeriodMonth = p.PayPeriodMonth, PayPeriodYear = p.PayPeriodYear,
        GrossEarnings = p.GrossEarnings, UifDeduction = p.UifDeduction,
        TotalLoanDeductions = p.TotalLoanDeductions, TotalDeductions = p.TotalDeductions,
        NetPay = p.NetPay, EmployeeId = p.EmployeeId, GeneratedAt = p.GeneratedAt,
        LoanDeductions = p.LoanDeductions.Select(d => new PayslipLoanDeductionDto
        {
            EmployeeLoanId = d.EmployeeLoanId, Description = d.Description, Amount = d.Amount
        }).ToList()
    };
}
