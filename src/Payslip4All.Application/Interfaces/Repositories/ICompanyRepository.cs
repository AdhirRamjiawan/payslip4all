using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Interfaces.Repositories;
public interface ICompanyRepository
{
    Task<IReadOnlyList<Company>> GetAllByUserIdAsync(Guid userId);
    Task<Company?> GetByIdAsync(Guid id, Guid userId);
    Task<Company?> GetByIdWithEmployeesAsync(Guid id, Guid userId);
    Task AddAsync(Company company);
    Task UpdateAsync(Company company);
    Task DeleteAsync(Company company);
    Task<bool> HasEmployeesAsync(Guid id);
}
