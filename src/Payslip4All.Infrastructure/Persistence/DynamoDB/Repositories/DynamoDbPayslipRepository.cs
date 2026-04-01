using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

/// <summary>
/// DynamoDB implementation of <see cref="IPayslipRepository"/>.
/// Stores PayslipLoanDeductions in a separate table and hydrates navigation properties.
/// </summary>
public sealed class DynamoDbPayslipRepository : IPayslipRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly string _deductionTableName;
    private readonly string _employeeTableName;
    private readonly string _companyTableName;
    private readonly string _loanTableName;

    public DynamoDbPayslipRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        var prefix = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_PREFIX")?.Trim() ?? "payslip4all";
        _tableName = $"{prefix}_payslips";
        _deductionTableName = $"{prefix}_payslip_loan_deductions";
        _employeeTableName = $"{prefix}_employees";
        _companyTableName = $"{prefix}_companies";
        _loanTableName = $"{prefix}_employee_loans";
    }

    public async Task<IReadOnlyList<Payslip>> GetAllByEmployeeIdAsync(Guid employeeId, Guid userId)
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
            ScanIndexForward = false, // descending by generatedAt
        });

        var payslips = response.Items
            .Where(item => item.TryGetValue("userId", out var u) && u.S == userId.ToString())
            .Select(MapToPayslip)
            .ToList();

        // Hydrate loan deductions for each payslip
        foreach (var payslip in payslips)
        {
            payslip.LoanDeductions = await GetDeductionsAsync(payslip.Id);
        }

        return payslips;
    }

    public async Task<Payslip?> GetByIdAsync(Guid id, Guid userId)
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

        var payslip = MapToPayslip(response.Item);

        // Hydrate loan deductions
        payslip.LoanDeductions = await GetDeductionsAsync(payslip.Id);

        // Hydrate Employee navigation
        var empResponse = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _employeeTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = payslip.EmployeeId.ToString() },
            },
        });

        if (empResponse.IsItemSet)
        {
            var employee = MapToEmployee(empResponse.Item);

            // Hydrate Employee.Company navigation
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

            payslip.Employee = employee;
        }

        return payslip;
    }

    public async Task<bool> ExistsAsync(Guid employeeId, int month, int year)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "employeeId-index",
            KeyConditionExpression = "employeeId = :employeeId",
            FilterExpression = "payPeriodMonth = :month AND payPeriodYear = :year",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":employeeId"] = new AttributeValue { S = employeeId.ToString() },
                [":month"] = new AttributeValue { N = month.ToString() },
                [":year"] = new AttributeValue { N = year.ToString() },
            },
            Select = Select.COUNT,
        });

        return response.Count > 0;
    }

    public async Task AddAsync(Payslip payslip)
    {
        // Fetch employee to get userId for denormalization
        var empResponse = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _employeeTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = payslip.EmployeeId.ToString() },
            },
        });

        var userId = DynamoDbOwnership.GetRequiredUserId(empResponse, "Employee", payslip.EmployeeId);

        // PutItem payslip
        await _dynamoDb.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = ToPayslipItem(payslip, userId),
        });

        // PutItem each loan deduction
        foreach (var deduction in payslip.LoanDeductions)
        {
            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = _deductionTableName,
                Item = ToDeductionItem(deduction),
            });
        }
    }

    public async Task DeleteAsync(Payslip payslip)
    {
        // Delete payslip
        await _dynamoDb.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = payslip.Id.ToString() },
            },
        });

        // Delete all associated deductions
        var deductions = await GetDeductionsAsync(payslip.Id);
        foreach (var deduction in deductions)
        {
            await _dynamoDb.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = _deductionTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["id"] = new AttributeValue { S = deduction.Id.ToString() },
                },
            });
        }
    }

    private async Task<List<PayslipLoanDeduction>> GetDeductionsAsync(Guid payslipId)
    {
        var response = await _dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _deductionTableName,
            IndexName = "payslipId-index",
            KeyConditionExpression = "payslipId = :payslipId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":payslipId"] = new AttributeValue { S = payslipId.ToString() },
            },
        });

        return response.Items.Select(MapToDeduction).ToList();
    }

    private static Dictionary<string, AttributeValue> ToPayslipItem(Payslip payslip, string userId)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = payslip.Id.ToString() },
            ["payPeriodMonth"] = new AttributeValue { N = payslip.PayPeriodMonth.ToString() },
            ["payPeriodYear"] = new AttributeValue { N = payslip.PayPeriodYear.ToString() },
            ["grossEarnings"] = new AttributeValue { S = payslip.GrossEarnings.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["uifDeduction"] = new AttributeValue { S = payslip.UifDeduction.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["totalLoanDeductions"] = new AttributeValue { S = payslip.TotalLoanDeductions.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["totalDeductions"] = new AttributeValue { S = payslip.TotalDeductions.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["netPay"] = new AttributeValue { S = payslip.NetPay.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["chargedAmount"] = new AttributeValue { S = payslip.ChargedAmount.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
            ["employeeId"] = new AttributeValue { S = payslip.EmployeeId.ToString() },
            ["userId"] = new AttributeValue { S = userId },
            ["generatedAt"] = new AttributeValue { S = payslip.GeneratedAt.ToString("O") },
        };
    }

    private static Dictionary<string, AttributeValue> ToDeductionItem(PayslipLoanDeduction deduction)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["id"] = new AttributeValue { S = deduction.Id.ToString() },
            ["payslipId"] = new AttributeValue { S = deduction.PayslipId.ToString() },
            ["employeeLoanId"] = new AttributeValue { S = deduction.EmployeeLoanId.ToString() },
            ["description"] = new AttributeValue { S = deduction.Description },
            ["amount"] = new AttributeValue { S = deduction.Amount.ToString("G", System.Globalization.CultureInfo.InvariantCulture) },
        };
    }

    private static Payslip MapToPayslip(Dictionary<string, AttributeValue> item)
    {
        var payslip = new Payslip();
        SetProperty(payslip, "Id", Guid.Parse(item["id"].S));
        payslip.PayPeriodMonth = int.Parse(item["payPeriodMonth"].N);
        payslip.PayPeriodYear = int.Parse(item["payPeriodYear"].N);
        payslip.GrossEarnings = decimal.Parse(item["grossEarnings"].S, System.Globalization.CultureInfo.InvariantCulture);
        payslip.UifDeduction = decimal.Parse(item["uifDeduction"].S, System.Globalization.CultureInfo.InvariantCulture);
        payslip.TotalLoanDeductions = decimal.Parse(item["totalLoanDeductions"].S, System.Globalization.CultureInfo.InvariantCulture);
        payslip.TotalDeductions = decimal.Parse(item["totalDeductions"].S, System.Globalization.CultureInfo.InvariantCulture);
        payslip.NetPay = decimal.Parse(item["netPay"].S, System.Globalization.CultureInfo.InvariantCulture);
        if (item.TryGetValue("chargedAmount", out var chargedAmount))
            payslip.ChargedAmount = decimal.Parse(chargedAmount.S, System.Globalization.CultureInfo.InvariantCulture);
        payslip.EmployeeId = Guid.Parse(item["employeeId"].S);
        SetProperty(payslip, "GeneratedAt", DateTimeOffset.Parse(item["generatedAt"].S));
        return payslip;
    }

    private static PayslipLoanDeduction MapToDeduction(Dictionary<string, AttributeValue> item)
    {
        var deduction = new PayslipLoanDeduction();
        SetProperty(deduction, "Id", Guid.Parse(item["id"].S));
        deduction.PayslipId = Guid.Parse(item["payslipId"].S);
        deduction.EmployeeLoanId = Guid.Parse(item["employeeLoanId"].S);
        deduction.Description = item["description"].S;
        deduction.Amount = decimal.Parse(item["amount"].S, System.Globalization.CultureInfo.InvariantCulture);
        return deduction;
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

    private static Company MapToCompany(Dictionary<string, AttributeValue> item)
    {
        var company = new Company();
        SetProperty(company, "Id", Guid.Parse(item["id"].S));
        company.Name = item["name"].S;
        company.UserId = Guid.Parse(item["userId"].S);
        SetProperty(company, "CreatedAt", DateTimeOffset.Parse(item["createdAt"].S));
        if (item.TryGetValue("address", out var addr)) company.Address = addr.S;
        if (item.TryGetValue("uifNumber", out var uifN)) company.UifNumber = uifN.S;
        if (item.TryGetValue("sarsPayeNumber", out var sars)) company.SarsPayeNumber = sars.S;
        return company;
    }

    private static void SetProperty<T>(T obj, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName)!;
        prop.SetValue(obj, value);
    }
}
