using Payslip4All.Infrastructure.Persistence.Repositories;

namespace Payslip4All.Infrastructure.Tests.Repositories;

public class PayslipPricingRepositoryTests : RepositoryTestBase
{
    private readonly PayslipPricingRepository _repo;

    public PayslipPricingRepositoryTests() => _repo = new PayslipPricingRepository(Db);

    [Fact]
    public async Task GetCurrentAsync_ReturnsSeededDefaultPrice()
    {
        var result = await _repo.GetCurrentAsync();

        Assert.NotNull(result);
        Assert.Equal(15m, result!.PricePerPayslip);
    }

    [Fact]
    public async Task UpdateAsync_PersistsNewPrice()
    {
        var pricing = await _repo.GetCurrentAsync();
        Assert.NotNull(pricing);
        pricing!.UpdatePrice(12m, "admin");

        await _repo.UpdateAsync(pricing);

        var reloaded = await _repo.GetCurrentAsync();
        Assert.Equal(12m, reloaded!.PricePerPayslip);
        Assert.Equal("admin", reloaded.UpdatedByUserId);
    }
}
