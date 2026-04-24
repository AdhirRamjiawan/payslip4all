namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

public sealed class DynamoDbTableNameProvider
{
    public DynamoDbTableNameProvider(DynamoDbConfigurationOptions options)
    {
        Prefix = options.TablePrefix;
    }

    public static DynamoDbTableNameProvider CreateDefault()
        => new(new DynamoDbConfigurationOptions());

    public string Prefix { get; }
    public string Users => Build("users");
    public string Companies => Build("companies");
    public string Employees => Build("employees");
    public string EmployeeLoans => Build("employee_loans");
    public string Payslips => Build("payslips");
    public string PayslipLoanDeductions => Build("payslip_loan_deductions");
    public string Wallets => Build("wallets");
    public string WalletActivities => Build("wallet_activities");
    public string PayslipPricing => Build("payslip_pricing");
    public string PaymentReturnEvidences => Build("payment_return_evidences");
    public string OutcomeNormalizationDecisions => Build("outcome_normalization_decisions");
    public string UnmatchedPaymentReturnRecords => Build("unmatched_payment_return_records");
    public string WalletTopUpAttempts => Build("wallet_topup_attempts");

    public IReadOnlyList<string> GetRequiredTableNames()
        => new[]
        {
            Users,
            Companies,
            Employees,
            EmployeeLoans,
            Payslips,
            PayslipLoanDeductions,
            Wallets,
            WalletActivities,
            PayslipPricing,
            PaymentReturnEvidences,
            OutcomeNormalizationDecisions,
            UnmatchedPaymentReturnRecords,
            WalletTopUpAttempts,
        };

    private string Build(string suffix) => $"{Prefix}_{suffix}";
}
