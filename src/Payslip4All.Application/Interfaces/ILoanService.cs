using Payslip4All.Application.DTOs.Loan;
namespace Payslip4All.Application.Interfaces;
public interface ILoanService
{
    Task<IReadOnlyList<LoanDto>> GetLoansForEmployeeAsync(Guid employeeId, Guid userId);
    Task<LoanDto?> GetLoanByIdAsync(Guid id, Guid userId);
    Task<LoanDto> CreateLoanAsync(CreateLoanCommand command);
    Task<LoanDto?> UpdateLoanAsync(UpdateLoanCommand command);
    Task<bool> DeleteLoanAsync(Guid id, Guid userId);
}
