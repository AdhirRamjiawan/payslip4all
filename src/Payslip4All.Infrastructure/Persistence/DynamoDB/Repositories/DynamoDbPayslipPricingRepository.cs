using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using System.Globalization;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

public sealed class DynamoDbPayslipPricingRepository : IPayslipPricingRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbPayslipPricingRepository(IAmazonDynamoDB dynamoDb, DynamoDbTableNameProvider? tableNames = null)
    {
        _dynamoDb = dynamoDb;
        tableNames ??= DynamoDbTableNameProvider.CreateDefault();
        _tableName = tableNames.PayslipPricing;
    }

    public async Task<PayslipPricingSetting?> GetCurrentAsync()
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = PayslipPricingSetting.DefaultId.ToString() },
            },
        });

        if (response.IsItemSet)
            return Map(response.Item);

        return null;
    }

    public async Task AddAsync(PayslipPricingSetting setting)
    {
        SetProperty(setting, nameof(PayslipPricingSetting.Id), PayslipPricingSetting.DefaultId);

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(setting),
        });
    }

    public async Task UpdateAsync(PayslipPricingSetting setting)
    {
        SetProperty(setting, nameof(PayslipPricingSetting.Id), PayslipPricingSetting.DefaultId);

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(setting),
        });
    }

    private static Dictionary<string, AttributeValue> ToItem(PayslipPricingSetting setting)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = setting.Id.ToString() },
            ["pricePerPayslip"] = new() { S = setting.PricePerPayslip.ToString("G", CultureInfo.InvariantCulture) },
            ["updatedAt"] = new() { S = setting.UpdatedAt.ToString("O") },
        };

        if (!string.IsNullOrWhiteSpace(setting.UpdatedByUserId))
            item["updatedByUserId"] = new() { S = setting.UpdatedByUserId };

        return item;
    }

    private static PayslipPricingSetting Map(Dictionary<string, AttributeValue> item)
    {
        var setting = new PayslipPricingSetting();
        SetProperty(setting, nameof(PayslipPricingSetting.Id), Guid.Parse(item["id"].S));
        SetProperty(setting, nameof(PayslipPricingSetting.PricePerPayslip), decimal.Parse(item["pricePerPayslip"].S, CultureInfo.InvariantCulture));
        if (item.TryGetValue("updatedByUserId", out var updatedByUserId)) SetProperty(setting, nameof(PayslipPricingSetting.UpdatedByUserId), updatedByUserId.S);
        SetProperty(setting, nameof(PayslipPricingSetting.UpdatedAt), DateTimeOffset.Parse(item["updatedAt"].S, CultureInfo.InvariantCulture));
        return setting;
    }

    private static void SetProperty<T>(T obj, string propertyName, object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(obj, value);
    }
}
