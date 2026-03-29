using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

/// <summary>
/// DynamoDB implementation of <see cref="IEmployeeRepository"/>.
/// </summary>
public sealed class DynamoDbEmployeeRepository : IEmployeeRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly string _companyTableName;
    private readonly string _loanTableName;
    private readonly string _payslipTableName;

    public DynamoDbEmployeeRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_employees";
        _companyTableName = $"{prefix}_companies";
        _loanTableName = $"{prefix}_employee_loans";
        _payslipTableName = $"{prefix}_payslips";
    }

    public async Task<IReadOnlyList<Employee>> GetAllByCompanyIdAsync(Guid companyId, Guid userId)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "companyId-index",
            KeyConditionExpression = "companyId = :companyId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":companyId"] = new AttributeValue { S = companyId.ToString() },
            },
        });

        return response.Items
            .Where(item => item.TryGetValue("userId", out var u) && u.S == userId.ToString())
            .Select(MapToEmployee)
            .ToList();
    }

    public async Task<Employee?> GetByIdAsync(Guid id, Guid userId)
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
        var employee = MapToEmployee(response.Item);
        if (!response.Item.TryGetValue("userId", out var u) || u.S != userId.ToString())
            return null;

        // Hydrate Company navigation property
        var companyResponse = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _companyTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = employee.CompanyId.ToString() },
            },
        });

        if (companyResponse.IsItemSet)
            employee.Company = MapToCompany(companyResponse.Item);

        return employee;
    }

    public async Task<Employee?> GetByIdWithLoansAsync(Guid id, Guid userId)
    {
        var employee = await GetByIdAsync(id, userId);
        if (employee == null) return null;

        // Hydrate Loans
        var loansResponse = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _loanTableName,
            IndexName = "employeeId-index",
            KeyConditionExpression = "employeeId = :employeeId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":employeeId"] = new AttributeValue { S = id.ToString() },
            },
        });

        employee.Loans = loansResponse.Items.Select(MapToLoan).ToList();

        return employee;
    }

    public async Task AddAsync(Employee employee)
    {
        // Fetch company to get userId for denormalization
        var companyResponse = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _companyTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = employee.CompanyId.ToString() },
            },
        });

        var userId = companyResponse.IsItemSet && companyResponse.Item.TryGetValue("userId", out var u)
            ? u.S
            : string.Empty;

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(employee, userId),
        });
    }

    public async Task UpdateAsync(Employee employee)
    {
        // Preserve existing userId from stored item
        var existing = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = employee.Id.ToString() },
            },
        });

        var userId = existing.IsItemSet && existing.Item.TryGetValue("userId", out var u)
            ? u.S
            : string.Empty;

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(employee, userId),
        });
    }

    public async Task DeleteAsync(Employee employee)
    {
        await _dynamoDb.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = employee.Id.ToString() },
            },
        });
    }

    public async Task<bool> HasPayslipsAsync(Guid id)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _payslipTableName,
            IndexName = "employeeId-index",
            KeyConditionExpression = "employeeId = :employeeId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":employeeId"] = new AttributeValue { S = id.ToString() },
            },
            Select = Select.COUNT,
            Limit = 1,
        });

        return response.Count > 0;
    }

    private static Dictionary<string, AttributeValue> ToItem(Employee employee, string userId)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = employee.Id.ToString() },
            ["firstName"] = new AttributeValue { S = employee.FirstName },
            ["lastName"] = new AttributeValue { S = employee.LastName },
            ["idNumber"] = new AttributeValue { S = employee.IdNumber },
            ["employeeNumber"] = new AttributeValue { S = employee.EmployeeNumber },
            ["startDate"] = new AttributeValue { S = employee.StartDate.ToString("yyyy-MM-dd") },
            ["occupation"] = new AttributeValue { S = employee.Occupation },
            ["monthlyGrossSalary"] = new AttributeValue { S = employee.MonthlyGrossSalary.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["companyId"] = new AttributeValue { S = employee.CompanyId.ToString() },
            ["userId"] = new AttributeValue { S = userId },
            ["createdAt"] = new AttributeValue { S = employee.CreatedAt.ToString("O") },
        };

        if (!string.IsNullOrEmpty(employee.UifReference))
            item["uifReference"] = new AttributeValue { S = employee.UifReference };

        return item;
    }

    internal static Employee MapToEmployee(Dictionary<string, AttributeValue> item)
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

    internal static Company MapToCompany(Dictionary<string, AttributeValue> item)
    {
        var company = new Company();
        SetProperty(company, "Id", Guid.Parse(item["id"].S));
        company.Name = item["name"].S;
        company.UserId = Guid.Parse(item["userId"].S);
        SetProperty(company, "CreatedAt", DateTimeOffset.Parse(item["createdAt"].S));

        if (item.TryGetValue("address", out var addr)) company.Address = addr.S;
        if (item.TryGetValue("uifNumber", out var uif)) company.UifNumber = uif.S;
        if (item.TryGetValue("sarsPayeNumber", out var sars)) company.SarsPayeNumber = sars.S;

        return company;
    }

    private static EmployeeLoan MapToLoan(Dictionary<string, AttributeValue> item)
    {
        var loan = new EmployeeLoan();
        SetProperty(loan, "Id", Guid.Parse(item["id"].S));
        loan.Description = item["description"].S;
        loan.TotalLoanAmount = decimal.Parse(item["totalLoanAmount"].S, System.Globalization.CultureInfo.InvariantCulture);
        loan.NumberOfTerms = int.Parse(item["numberOfTerms"].N);
        loan.MonthlyDeductionAmount = decimal.Parse(item["monthlyDeductionAmount"].S, System.Globalization.CultureInfo.InvariantCulture);
        loan.PaymentStartDate = DateOnly.Parse(item["paymentStartDate"].S);
        SetProperty(loan, "TermsCompleted", int.Parse(item["termsCompleted"].N));
        SetProperty(loan, "Status", (Payslip4All.Domain.Enums.LoanStatus)int.Parse(item["status"].N));
        loan.EmployeeId = Guid.Parse(item["employeeId"].S);
        SetProperty(loan, "CreatedAt", DateTimeOffset.Parse(item["createdAt"].S));
        return loan;
    }

    private static void SetProperty<T>(T obj, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName)!;
        prop.SetValue(obj, value);
    }
}
