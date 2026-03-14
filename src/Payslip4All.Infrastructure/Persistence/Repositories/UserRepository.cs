using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;
namespace Payslip4All.Infrastructure.Persistence.Repositories;
public class UserRepository : IUserRepository
{
    private readonly PayslipDbContext _db;
    public UserRepository(PayslipDbContext db) => _db = db;
    public async Task<User?> GetByEmailAsync(string email)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
    public async Task AddAsync(User user)
    {
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
    }
    public async Task<bool> ExistsAsync(string email)
        => await _db.Users.AnyAsync(u => u.Email == email.ToLower());
}
