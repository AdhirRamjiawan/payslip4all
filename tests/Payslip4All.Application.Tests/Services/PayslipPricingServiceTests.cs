using Moq;
using Payslip4All.Application.DTOs.Pricing;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Tests.Services;

public class PayslipPricingServiceTests
{
    private readonly Mock<IPayslipPricingRepository> _repository = new();

    private PayslipPricingService CreateService() => new(_repository.Object);

    [Fact]
    public async Task GetCurrentPriceAsync_ReturnsDefault_WhenNoSettingExists()
    {
        _repository.Setup(r => r.GetCurrentAsync()).ReturnsAsync((PayslipPricingSetting?)null);

        var result = await CreateService().GetCurrentPriceAsync();

        Assert.Equal(0m, result.PricePerPayslip);
    }

    [Fact]
    public async Task UpdatePriceAsync_WithExistingSetting_UpdatesRepository()
    {
        var setting = new PayslipPricingSetting { PricePerPayslip = 0m };
        _repository.Setup(r => r.GetCurrentAsync()).ReturnsAsync(setting);

        var result = await CreateService().UpdatePriceAsync(new UpdatePayslipPriceCommand
        {
            PricePerPayslip = 12m,
            UpdatedByUserId = "admin-1",
        });

        Assert.Equal(12m, result.PricePerPayslip);
        Assert.Equal("admin-1", result.UpdatedByUserId);
        _repository.Verify(r => r.UpdateAsync(setting), Times.Once);
    }

    [Fact]
    public async Task UpdatePriceAsync_WithNegativeValue_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService().UpdatePriceAsync(new UpdatePayslipPriceCommand { PricePerPayslip = -1m }));
    }
}
