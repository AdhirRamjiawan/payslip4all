using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

/// <summary>
/// DynamoDB implementation of <see cref="ILoanRepository"/>.
/// Uses conditional UpdateItem for termsCompleted to enforce optimistic concurrency.
/// </summary>
public sealed class DynamoDbLoanRepository : ILoanRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly string _employeeTableName;
    private readonly string _companyTableName;

    public DynamoDbLoanRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_employee_loans";
        _employeeTableName = $"{prefix}_employees";
        _companyTableName = $"{prefix}_companies";
    }

    public async Task<IReadOnlyList<EmployeeLoan>> GetAllByEmployeeIdAsync(Guid employeeId, Guid userId)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "employeeId-index",
            KeyConditionExpression = "employeeId = :employeeId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":employeeId"] = new AttributeValue { S = employeeId.ToString() },
            },
        });

        return response.Items
            .Where(item => item.TryGetValue("userId", out var u) && u.S == userId.ToString())
            .Select(MapToLoan)
            .ToList();
    }

    public async Task<EmployeeLoan?> GetByIdAsync(Guid id, Guid userId)
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
        if (!response.Item.TryGetValue("userId", out var u) || u.S != userId.ToString())
            return null;

        return MapToLoan(response.Item);
    }

    public async Task AddAsync(EmployeeLoan loan)
    {
        // Fetch employee to get userId for denormalization
        var empResponse = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _employeeTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = loan.EmployeeId.ToString() },
            },
        });

        var userId = empResponse.IsItemSet && empResponse.Item.TryGetValue("userId", out var u)
            ? u.S
            : string.Empty;

        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToItem(loan, userId),
        });
    }

    public async Task UpdateAsync(EmployeeLoan loan)
    {
        // Get the current termsCompleted to use as optimistic concurrency condition
        var existing = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = loan.Id.ToString() },
            },
        });

        if (!existing.IsItemSet)
            throw new InvalidOperationException($"Loan {loan.Id} not found.");

        var expectedTermsCompleted = existing.Item["termsCompleted"].N;

        try
        {
            await _dynamoDb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["id"] = new AttributeValue { S = loan.Id.ToString() },
                },
                UpdateExpression = "SET termsCompleted = :newTerms, #st = :newStatus, description = :desc, " +
                                   "totalLoanAmount = :tla, numberOfTerms = :not, monthlyDeductionAmount = :mda, " +
                                   "paymentStartDate = :psd",
                ConditionExpression = "termsCompleted = :expectedTerms",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#st"] = "status",
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":newTerms"] = new AttributeValue { N = loan.TermsCompleted.ToString() },
                    [":newStatus"] = new AttributeValue { N = ((int)loan.Status).ToString() },
                    [":expectedTerms"] = new AttributeValue { N = expectedTermsCompleted },
                    [":desc"] = new AttributeValue { S = loan.Description },
                    [":tla"] = new AttributeValue { S = loan.TotalLoanAmount.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
                    [":not"] = new AttributeValue { N = loan.NumberOfTerms.ToString() },
                    [":mda"] = new AttributeValue { S = loan.MonthlyDeductionAmount.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
                    [":psd"] = new AttributeValue { S = loan.PaymentStartDate.ToString("yyyy-MM-dd") },
                },
            });
        }
        catch (ConditionalCheckFailedException ex)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict updating loan {loan.Id}: termsCompleted was modified by another process.", ex);
        }
    }

    public async Task DeleteAsync(EmployeeLoan loan)
    {
        await _dynamoDb.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = loan.Id.ToString() },
            },
        });
    }

    private static Dictionary<string, AttributeValue> ToItem(EmployeeLoan loan, string userId)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = loan.Id.ToString() },
            ["description"] = new AttributeValue { S = loan.Description },
            ["totalLoanAmount"] = new AttributeValue { S = loan.TotalLoanAmount.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["numberOfTerms"] = new AttributeValue { N = loan.NumberOfTerms.ToString() },
            ["monthlyDeductionAmount"] = new AttributeValue { S = loan.MonthlyDeductionAmount.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["paymentStartDate"] = new AttributeValue { S = loan.PaymentStartDate.ToString("yyyy-MM-dd") },
            ["termsCompleted"] = new AttributeValue { N = loan.TermsCompleted.ToString() },
            ["status"] = new AttributeValue { N = ((int)loan.Status).ToString() },
            ["employeeId"] = new AttributeValue { S = loan.EmployeeId.ToString() },
            ["userId"] = new AttributeValue { S = userId },
            ["createdAt"] = new AttributeValue { S = loan.CreatedAt.ToString("O") },
        };
    }

    internal static EmployeeLoan MapToLoan(Dictionary<string, AttributeValue> item)
    {
        var loan = new EmployeeLoan();
        SetProperty(loan, "Id", Guid.Parse(item["id"].S));
        loan.Description = item["description"].S;
        loan.TotalLoanAmount = decimal.Parse(item["totalLoanAmount"].S, System.Globalization.CultureInfo.InvariantCulture);
        loan.NumberOfTerms = int.Parse(item["numberOfTerms"].N);
        loan.MonthlyDeductionAmount = decimal.Parse(item["monthlyDeductionAmount"].S, System.Globalization.CultureInfo.InvariantCulture);
        loan.PaymentStartDate = DateOnly.Parse(item["paymentStartDate"].S);
        SetProperty(loan, "TermsCompleted", int.Parse(item["termsCompleted"].N));
        SetProperty(loan, "Status", (LoanStatus)int.Parse(item["status"].N));
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
