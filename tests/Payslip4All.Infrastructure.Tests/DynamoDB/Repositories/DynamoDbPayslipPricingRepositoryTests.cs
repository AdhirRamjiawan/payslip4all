using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Tests.DynamoDB.Repositories;

[Collection(DynamoDbTestCollection.Name)]
[Trait("Category", "Integration")]
public class DynamoDbPayslipPricingRepositoryTests : IClassFixture<DynamoDbTestFixture>
{
    private readonly DynamoDbTestFixture _fixture;
    private readonly DynamoDbPayslipPricingRepository _repo;

    public DynamoDbPayslipPricingRepositoryTests(DynamoDbTestFixture fixture)
    {
        _fixture = fixture;
        _repo = new DynamoDbPayslipPricingRepository(fixture.Client);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsStoredSetting()
    {
        var pricingId = PayslipPricingSetting.DefaultId;
        await _fixture.SeedPayslipPricingAsync(pricingId, 15m, "admin");

        var result = await _repo.GetCurrentAsync();

        Assert.NotNull(result);
        Assert.Equal(15m, result!.PricePerPayslip);
    }

    [Fact]
    public async Task UpdateAsync_PersistsNewPrice()
    {
        var setting = new PayslipPricingSetting();
        setting.UpdatePrice(5m, null);

        await _repo.AddAsync(setting);
        setting.UpdatePrice(9m, "admin");
        await _repo.UpdateAsync(setting);

        var result = await _repo.GetCurrentAsync();
        Assert.Equal(9m, result!.PricePerPayslip);
    }
}
