using Payslip4All.Application.DTOs.Loan;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Services;
public class LoanService : ILoanService
{
    private readonly ILoanRepository _repo;
    public LoanService(ILoanRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<LoanDto>> GetLoansForEmployeeAsync(Guid employeeId, Guid userId)
    {
        var loans = await _repo.GetAllByEmployeeIdAsync(employeeId, userId);
        return loans.Select(l => ToDto(l)).ToList();
    }
    public async Task<LoanDto?> GetLoanByIdAsync(Guid id, Guid userId)
    {
        var l = await _repo.GetByIdAsync(id, userId);
        return l == null ? null : ToDto(l);
    }
    public async Task<LoanDto> CreateLoanAsync(CreateLoanCommand command)
    {
        var loan = new EmployeeLoan
        {
            Description = command.Description,
            TotalLoanAmount = command.TotalLoanAmount,
            NumberOfTerms = command.NumberOfTerms,
            MonthlyDeductionAmount = command.MonthlyDeductionAmount,
            PaymentStartDate = command.PaymentStartDate,
            EmployeeId = command.EmployeeId
        };
        await _repo.AddAsync(loan);
        return ToDto(loan);
    }
    public async Task<LoanDto?> UpdateLoanAsync(UpdateLoanCommand command)
    {
        var loan = await _repo.GetByIdAsync(command.LoanId, command.UserId);
        if (loan == null || loan.TermsCompleted > 0) return null;
        loan.Description = command.Description;
        loan.TotalLoanAmount = command.TotalLoanAmount;
        loan.NumberOfTerms = command.NumberOfTerms;
        loan.MonthlyDeductionAmount = command.MonthlyDeductionAmount;
        loan.PaymentStartDate = command.PaymentStartDate;
        await _repo.UpdateAsync(loan);
        return ToDto(loan);
    }
    public async Task<bool> DeleteLoanAsync(Guid id, Guid userId)
    {
        var loan = await _repo.GetByIdAsync(id, userId);
        if (loan == null || loan.TermsCompleted > 0) return false;
        await _repo.DeleteAsync(loan);
        return true;
    }
    private static LoanDto ToDto(EmployeeLoan l) => new LoanDto
    {
        Id = l.Id,
        Description = l.Description,
        TotalLoanAmount = l.TotalLoanAmount,
        NumberOfTerms = l.NumberOfTerms,
        MonthlyDeductionAmount = l.MonthlyDeductionAmount,
        PaymentStartDate = l.PaymentStartDate,
        TermsCompleted = l.TermsCompleted,
        Status = l.Status.ToString(),
        EmployeeId = l.EmployeeId,
        CreatedAt = l.CreatedAt
    };
}
