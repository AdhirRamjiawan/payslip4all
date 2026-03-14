namespace Payslip4All.Domain.Services;

public static class PayslipCalculator
{
    public const decimal UifEarningsCeiling = 17_712.00m;
    public const decimal UifContributionRate = 0.01m;

    public static decimal CalculateUifDeduction(decimal monthlyGrossSalary)
    {
        if (monthlyGrossSalary <= 0)
            throw new ArgumentException("Gross salary must be positive.", nameof(monthlyGrossSalary));
        return Math.Round(
            Math.Min(monthlyGrossSalary, UifEarningsCeiling) * UifContributionRate,
            2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateTotalDeductions(decimal uifDeduction, IEnumerable<decimal> loanDeductions)
        => uifDeduction + loanDeductions.Sum();

    public static decimal CalculateNetPay(decimal grossEarnings, decimal uifDeduction, IEnumerable<decimal> loanDeductions)
        => grossEarnings - CalculateTotalDeductions(uifDeduction, loanDeductions);
}
