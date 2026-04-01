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

    public DynamoDbPayslipPricingRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_payslip_pricing";
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

        var fallbackResponse = await _dynamoDb.ScanAsync(new ScanRequest
        {
            TableName = _tableName,
            Limit = 1,
        });

        return fallbackResponse.Items.Count == 0 ? null : Map(fallbackResponse.Items[0]);
    }

    public async Task AddAsync(PayslipPricingSetting setting)
    {
        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(setting),
        });
    }

    public async Task UpdateAsync(PayslipPricingSetting setting)
    {
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
        setting.PricePerPayslip = decimal.Parse(item["pricePerPayslip"].S, CultureInfo.InvariantCulture);
        if (item.TryGetValue("updatedByUserId", out var updatedByUserId)) setting.UpdatedByUserId = updatedByUserId.S;
        SetProperty(setting, nameof(PayslipPricingSetting.UpdatedAt), DateTimeOffset.Parse(item["updatedAt"].S, CultureInfo.InvariantCulture));
        return setting;
    }

    private static void SetProperty<T>(T obj, string propertyName, object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(obj, value);
    }
}
