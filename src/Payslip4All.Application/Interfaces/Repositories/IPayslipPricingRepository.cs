using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces.Repositories;

public interface IPayslipPricingRepository
{
    Task<PayslipPricingSetting?> GetCurrentAsync();
    Task AddAsync(PayslipPricingSetting setting);
    Task UpdateAsync(PayslipPricingSetting setting);
}
