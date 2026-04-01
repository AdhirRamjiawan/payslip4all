using Payslip4All.Application.DTOs.Pricing;

namespace Payslip4All.Application.Interfaces;

public interface IPayslipPricingService
{
    Task<PayslipPricingSettingDto> GetCurrentPriceAsync();
    Task<PayslipPricingSettingDto> UpdatePriceAsync(UpdatePayslipPriceCommand command);
}
