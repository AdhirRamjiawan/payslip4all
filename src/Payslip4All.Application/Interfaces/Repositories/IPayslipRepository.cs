using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Interfaces.Repositories;
public interface IPayslipRepository
{
    Task<IReadOnlyList<Payslip>> GetAllByEmployeeIdAsync(Guid employeeId, Guid userId);
    Task<Payslip?> GetByIdAsync(Guid id, Guid userId);
    Task<bool> ExistsAsync(Guid employeeId, int month, int year);
    Task AddAsync(Payslip payslip);
    Task DeleteAsync(Payslip payslip);
}
