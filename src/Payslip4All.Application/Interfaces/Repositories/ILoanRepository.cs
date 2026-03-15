using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Interfaces.Repositories;
public interface ILoanRepository
{
    Task<IReadOnlyList<EmployeeLoan>> GetAllByEmployeeIdAsync(Guid employeeId, Guid userId);
    Task<EmployeeLoan?> GetByIdAsync(Guid id, Guid userId);
    Task AddAsync(EmployeeLoan loan);
    Task UpdateAsync(EmployeeLoan loan);
    Task DeleteAsync(EmployeeLoan loan);
}
