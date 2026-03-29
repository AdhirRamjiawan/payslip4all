using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

/// <summary>
/// DynamoDB implementation of <see cref="ICompanyRepository"/>.
/// </summary>
public sealed class DynamoDbCompanyRepository : ICompanyRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly string _employeeTableName;

    public DynamoDbCompanyRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_companies";
        _employeeTableName = $"{prefix}_employees";
    }

    public async Task<IReadOnlyList<Company>> GetAllByUserIdAsync(Guid userId)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "userId-index",
            KeyConditionExpression = "userId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":userId"] = new AttributeValue { S = userId.ToString() },
            },
        });

        return response.Items.Select(MapToCompany).ToList();
    }

    public async Task<Company?> GetByIdAsync(Guid id, Guid userId)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = id.ToString() },
            },
        });

        if (!response.IsItemSet) return null;
        var company = MapToCompany(response.Item);
        if (company.UserId != userId) return null;
        return company;
    }

    public async Task<Company?> GetByIdWithEmployeesAsync(Guid id, Guid userId)
    {
        var company = await GetByIdAsync(id, userId);
        if (company == null) return null;

        // Hydrate employees via companyId-index
        var empResponse = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _employeeTableName,
            IndexName = "companyId-index",
            KeyConditionExpression = "companyId = :companyId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":companyId"] = new AttributeValue { S = id.ToString() },
            },
        });

        company.Employees = empResponse.Items
            .Where(item => item.TryGetValue("userId", out var u) && u.S == userId.ToString())
            .Select(MapToEmployee)
            .ToList();

        return company;
    }

    public async Task AddAsync(Company company)
    {
        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(company),
        });
    }

    public async Task UpdateAsync(Company company)
    {
        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(company),
        });
    }

    public async Task DeleteAsync(Company company)
    {
        await _dynamoDb.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = company.Id.ToString() },
            },
        });
    }

    public async Task<bool> HasEmployeesAsync(Guid id)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _employeeTableName,
            IndexName = "companyId-index",
            KeyConditionExpression = "companyId = :companyId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":companyId"] = new AttributeValue { S = id.ToString() },
            },
            Select = Select.COUNT,
            Limit = 1,
        });

        return response.Count > 0;
    }

    private static Dictionary<string, AttributeValue> ToItem(Company company)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = company.Id.ToString() },
            ["name"] = new AttributeValue { S = company.Name },
            ["userId"] = new AttributeValue { S = company.UserId.ToString() },
            ["createdAt"] = new AttributeValue { S = company.CreatedAt.ToString("O") },
        };

        if (!string.IsNullOrEmpty(company.Address))
            item["address"] = new AttributeValue { S = company.Address };
        if (!string.IsNullOrEmpty(company.UifNumber))
            item["uifNumber"] = new AttributeValue { S = company.UifNumber };
        if (!string.IsNullOrEmpty(company.SarsPayeNumber))
            item["sarsPayeNumber"] = new AttributeValue { S = company.SarsPayeNumber };

        return item;
    }

    private static Company MapToCompany(Dictionary<string, AttributeValue> item)
    {
        var company = new Company();
        SetProperty(company, "Id", Guid.Parse(item["id"].S));
        company.Name = item["name"].S;
        company.UserId = Guid.Parse(item["userId"].S);
        SetProperty(company, "CreatedAt", DateTimeOffset.Parse(item["createdAt"].S));

        if (item.TryGetValue("address", out var address)) company.Address = address.S;
        if (item.TryGetValue("uifNumber", out var uif)) company.UifNumber = uif.S;
        if (item.TryGetValue("sarsPayeNumber", out var sars)) company.SarsPayeNumber = sars.S;

        return company;
    }

    private static Employee MapToEmployee(Dictionary<string, AttributeValue> item)
    {
        var employee = new Employee();
        SetProperty(employee, "Id", Guid.Parse(item["id"].S));
        employee.FirstName = item["firstName"].S;
        employee.LastName = item["lastName"].S;
        employee.IdNumber = item["idNumber"].S;
        employee.EmployeeNumber = item["employeeNumber"].S;
        employee.StartDate = DateOnly.Parse(item["startDate"].S);
        employee.Occupation = item["occupation"].S;
        employee.MonthlyGrossSalary = decimal.Parse(item["monthlyGrossSalary"].S, System.Globalization.CultureInfo.InvariantCulture);
        employee.CompanyId = Guid.Parse(item["companyId"].S);
        SetProperty(employee, "CreatedAt", DateTimeOffset.Parse(item["createdAt"].S));

        if (item.TryGetValue("uifReference", out var uif)) employee.UifReference = uif.S;

        return employee;
    }

    private static void SetProperty<T>(T obj, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName)!;
        prop.SetValue(obj, value);
    }
}
