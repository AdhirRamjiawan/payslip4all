using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Interfaces.Repositories;
public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> GetAllByCompanyIdAsync(Guid companyId, Guid userId);
    Task<Employee?> GetByIdAsync(Guid id, Guid userId);
    Task<Employee?> GetByIdWithLoansAsync(Guid id, Guid userId);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(Employee employee);
    Task<bool> HasPayslipsAsync(Guid id);
}
