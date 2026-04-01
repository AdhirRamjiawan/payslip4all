using Payslip4All.Application.DTOs.Pricing;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Services;

public class PayslipPricingService : IPayslipPricingService
{
    private readonly IPayslipPricingRepository _pricingRepository;

    public PayslipPricingService(IPayslipPricingRepository pricingRepository)
    {
        _pricingRepository = pricingRepository;
    }

    public async Task<PayslipPricingSettingDto> GetCurrentPriceAsync()
    {
        var setting = await _pricingRepository.GetCurrentAsync();
        if (setting == null)
        {
            return new PayslipPricingSettingDto
            {
                Id = PayslipPricingSetting.DefaultId,
                PricePerPayslip = PayslipPricingSetting.DefaultPricePerPayslip,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
        }

        return Map(setting);
    }

    public async Task<PayslipPricingSettingDto> UpdatePriceAsync(UpdatePayslipPriceCommand command)
    {
        if (command.PricePerPayslip < 0m)
            throw new ArgumentException("Price per payslip cannot be negative.", nameof(command.PricePerPayslip));

        var setting = await _pricingRepository.GetCurrentAsync();
        if (setting == null)
        {
            setting = new PayslipPricingSetting
            {
                PricePerPayslip = command.PricePerPayslip,
                UpdatedByUserId = command.UpdatedByUserId,
            };

            typeof(PayslipPricingSetting).GetProperty(nameof(PayslipPricingSetting.Id))!
                .SetValue(setting, PayslipPricingSetting.DefaultId);

            await _pricingRepository.AddAsync(setting);
        }
        else
        {
            setting.UpdatePrice(command.PricePerPayslip, command.UpdatedByUserId);
            await _pricingRepository.UpdateAsync(setting);
        }

        return Map(setting);
    }

    private static PayslipPricingSettingDto Map(PayslipPricingSetting setting)
    {
        return new PayslipPricingSettingDto
        {
            Id = setting.Id,
            PricePerPayslip = setting.PricePerPayslip,
            UpdatedByUserId = setting.UpdatedByUserId,
            UpdatedAt = setting.UpdatedAt,
        };
    }
}
