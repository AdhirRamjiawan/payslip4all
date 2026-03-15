using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Interfaces.Repositories;
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task<bool> ExistsAsync(string email);
}
