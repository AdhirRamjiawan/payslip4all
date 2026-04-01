using Payslip4All.Application.DTOs;
using Payslip4All.Application.DTOs.Payslip;
using Payslip4All.Application.DTOs.Wallet;
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
    private readonly IWalletService _walletService;
    private readonly IPayslipPricingService _pricingService;

    public PayslipGenerationService(
        IPayslipRepository payslipRepo,
        IEmployeeRepository employeeRepo,
        ILoanRepository loanRepo,
        IPdfGenerationService pdfService,
        IUnitOfWork unitOfWork,
        IWalletService walletService,
        IPayslipPricingService pricingService)
    {
        _payslipRepo = payslipRepo;
        _employeeRepo = employeeRepo;
        _loanRepo = loanRepo;
        _pdfService = pdfService;
        _unitOfWork = unitOfWork;
        _walletService = walletService;
        _pricingService = pricingService;
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
                    EmployeeLoanId = l.Id,
                    Description = l.Description,
                    Amount = l.MonthlyDeductionAmount
                }).ToList()
            }
        };
    }

    public async Task<PayslipResult> GeneratePayslipAsync(GeneratePayslipCommand command)
    {
        var exists = await _payslipRepo.ExistsAsync(command.EmployeeId, command.PayPeriodMonth, command.PayPeriodYear);
        if (exists && !command.OverwriteExisting)
            return new PayslipResult { Success = false, IsDuplicate = true, ErrorMessage = "Payslip already exists for this period." };

        var employee = await _employeeRepo.GetByIdWithLoansAsync(command.EmployeeId, command.UserId);
        if (employee == null) return new PayslipResult { Success = false, ErrorMessage = "Employee not found." };
        if (employee.MonthlyGrossSalary <= 0) return new PayslipResult { Success = false, ErrorMessage = "Employee has no salary." };

        var pricing = await _pricingService.GetCurrentPriceAsync();
        var wallet = await _walletService.GetWalletAsync(command.UserId);
        if (wallet.CurrentBalance < pricing.PricePerPayslip)
        {
            return new PayslipResult
            {
                Success = false,
                InsufficientFunds = true,
                ChargedAmount = pricing.PricePerPayslip,
                ErrorMessage = "Insufficient wallet balance to generate this payslip.",
            };
        }

        Payslip? existingPayslip = null;
        Payslip? payslip = null;
        IReadOnlyList<EmployeeLoan> activeLoans = Array.Empty<EmployeeLoan>();
        IReadOnlyDictionary<Guid, int> loanTermsSnapshot = new Dictionary<Guid, int>();
        var transactionStarted = await TryBeginTransactionAsync();
        var walletDebited = false;
        var existingPayslipDeleted = false;

        try
        {
            if (exists && command.OverwriteExisting)
            {
                existingPayslip = (await _payslipRepo.GetAllByEmployeeIdAsync(command.EmployeeId, command.UserId))
                    .FirstOrDefault(p => p.PayPeriodMonth == command.PayPeriodMonth && p.PayPeriodYear == command.PayPeriodYear);

                if (existingPayslip != null && transactionStarted)
                {
                    await _payslipRepo.DeleteAsync(existingPayslip);
                    existingPayslipDeleted = true;
                }
            }

            activeLoans = employee.Loans.Where(l => l.IsActiveForPeriod(command.PayPeriodMonth, command.PayPeriodYear)).ToList();
            loanTermsSnapshot = activeLoans.ToDictionary(loan => loan.Id, loan => loan.TermsCompleted);
            var uif = PayslipCalculator.CalculateUifDeduction(employee.MonthlyGrossSalary);
            var loanAmounts = activeLoans.Select(l => l.MonthlyDeductionAmount).ToList();
            var totalLoanDeductions = loanAmounts.Sum();
            var totalDeductions = PayslipCalculator.CalculateTotalDeductions(uif, loanAmounts);
            var netPay = PayslipCalculator.CalculateNetPay(employee.MonthlyGrossSalary, uif, loanAmounts);

            payslip = new Payslip
            {
                EmployeeId = employee.Id,
                PayPeriodMonth = command.PayPeriodMonth,
                PayPeriodYear = command.PayPeriodYear,
                GrossEarnings = employee.MonthlyGrossSalary,
                UifDeduction = uif,
                TotalLoanDeductions = totalLoanDeductions,
                TotalDeductions = totalDeductions,
                NetPay = netPay,
                ChargedAmount = pricing.PricePerPayslip,
            };

            foreach (var loan in activeLoans)
            {
                payslip.LoanDeductions.Add(new PayslipLoanDeduction
                {
                    PayslipId = payslip.Id,
                    EmployeeLoanId = loan.Id,
                    Description = loan.Description,
                    Amount = loan.MonthlyDeductionAmount
                });
                loan.IncrementTermsCompleted();
            }

            await _payslipRepo.AddAsync(payslip);

            foreach (var loan in activeLoans)
                await _loanRepo.UpdateAsync(loan);

            if (pricing.PricePerPayslip > 0m)
            {
                var debited = await _walletService.TryDebitAsync(
                    command.UserId,
                    pricing.PricePerPayslip,
                    $"Payslip generated for {new System.Globalization.DateTimeFormatInfo().GetMonthName(command.PayPeriodMonth)} {command.PayPeriodYear}",
                    "Payslip",
                    payslip.Id.ToString());

                if (!debited)
                {
                    if (transactionStarted)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                    }
                    else
                    {
                        await RevertGeneratedPayslipAsync(
                            payslip,
                            activeLoans,
                            loanTermsSnapshot,
                            existingPayslip,
                            existingPayslipDeleted);
                    }

                    return new PayslipResult
                    {
                        Success = false,
                        ChargedAmount = pricing.PricePerPayslip,
                        InsufficientFunds = true,
                        ErrorMessage = "Payslip could not be generated because the wallet charge did not complete.",
                    };
                }

                walletDebited = true;
            }

            if (existingPayslip != null && !transactionStarted)
            {
                await _payslipRepo.DeleteAsync(existingPayslip);
                existingPayslipDeleted = true;
            }

            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            if (transactionStarted)
                await _unitOfWork.CommitTransactionAsync();

            return new PayslipResult
            {
                Success = true,
                PayslipDto = ToDto(payslip),
                ChargedAmount = pricing.PricePerPayslip,
            };
        }
        catch
        {
            if (transactionStarted)
                await _unitOfWork.RollbackTransactionAsync();
            else if (payslip != null)
            {
                try
                {
                    await RevertGeneratedPayslipAsync(
                        payslip,
                        activeLoans,
                        loanTermsSnapshot,
                        existingPayslip,
                        existingPayslipDeleted);

                    if (walletDebited && pricing.PricePerPayslip > 0m)
                    {
                        await _walletService.TopUpAsync(new AddWalletCreditCommand
                        {
                            UserId = command.UserId,
                            Amount = pricing.PricePerPayslip,
                            Description = $"Wallet refund for failed payslip generation for {new System.Globalization.DateTimeFormatInfo().GetMonthName(command.PayPeriodMonth)} {command.PayPeriodYear}",
                            ReferenceType = "PayslipRefund",
                            ReferenceId = payslip.Id.ToString(),
                        });
                    }
                }
                catch
                {
                    // Best-effort compensation for immediate-write providers.
                }
            }

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
        if (payslip == null) return null;

        var employee = payslip.Employee;
        var monthName = new System.Globalization.DateTimeFormatInfo().GetMonthName(payslip.PayPeriodMonth);
        var doc = new PayslipDocument(
            CompanyName: employee.Company?.Name ?? "Company",
            CompanyAddress: employee.Company?.Address,
            EmployeeName: $"{employee.FirstName} {employee.LastName}",
            EmployeeNumber: employee.EmployeeNumber,
            Occupation: employee.Occupation,
            PayPeriod: $"{monthName} {payslip.PayPeriodYear}",
            GrossEarnings: payslip.GrossEarnings,
            UifDeduction: payslip.UifDeduction,
            LoanDeductions: payslip.LoanDeductions
                .Select(d => (d.Description, d.Amount))
                .ToList<(string, decimal)>(),
            TotalDeductions: payslip.TotalDeductions,
            NetPay: payslip.NetPay,
            CompanyUifNumber: employee.Company?.UifNumber,
            CompanySarsPayeNumber: employee.Company?.SarsPayeNumber,
            EmployeeIdNumber: employee.IdNumber,
            EmployeeStartDate: employee.StartDate,
            EmployeeUifReference: employee.UifReference,
            PaymentDate: new DateOnly(
                payslip.PayPeriodYear,
                payslip.PayPeriodMonth,
                DateTime.DaysInMonth(payslip.PayPeriodYear, payslip.PayPeriodMonth))
        );
        return _pdfService.GeneratePayslip(doc);
    }

    private static PayslipDto ToDto(Payslip p) => new()
    {
        Id = p.Id,
        PayPeriodMonth = p.PayPeriodMonth,
        PayPeriodYear = p.PayPeriodYear,
        GrossEarnings = p.GrossEarnings,
        UifDeduction = p.UifDeduction,
        TotalLoanDeductions = p.TotalLoanDeductions,
        TotalDeductions = p.TotalDeductions,
        NetPay = p.NetPay,
        ChargedAmount = p.ChargedAmount,
        EmployeeId = p.EmployeeId,
        GeneratedAt = p.GeneratedAt,
        LoanDeductions = p.LoanDeductions.Select(d => new PayslipLoanDeductionDto
        {
            EmployeeLoanId = d.EmployeeLoanId,
            Description = d.Description,
            Amount = d.Amount
        }).ToList()
    };

    private async Task<bool> TryBeginTransactionAsync()
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private async Task RevertGeneratedPayslipAsync(
        Payslip payslip,
        IReadOnlyList<EmployeeLoan> activeLoans,
        IReadOnlyDictionary<Guid, int> loanTermsSnapshot,
        Payslip? previousPayslip,
        bool previousPayslipDeleted)
    {
        await _payslipRepo.DeleteAsync(payslip);

        foreach (var loan in activeLoans)
        {
            if (loanTermsSnapshot.TryGetValue(loan.Id, out var termsCompleted))
                loan.RestoreTermsCompleted(termsCompleted);
            await _loanRepo.UpdateAsync(loan);
        }

        if (previousPayslipDeleted && previousPayslip != null)
            await _payslipRepo.AddAsync(previousPayslip);
    }
}
