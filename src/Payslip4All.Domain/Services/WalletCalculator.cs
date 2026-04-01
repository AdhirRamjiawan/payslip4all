namespace Payslip4All.Domain.Services;

public static class WalletCalculator
{
    public static decimal CalculateBalanceAfterCredit(decimal currentBalance, decimal amount)
    {
        ValidateAmount(amount);
        ValidateBalance(currentBalance);
        return currentBalance + amount;
    }

    public static decimal CalculateBalanceAfterDebit(decimal currentBalance, decimal amount)
    {
        ValidateAmount(amount);
        ValidateBalance(currentBalance);

        var balanceAfterDebit = currentBalance - amount;
        if (balanceAfterDebit < 0m)
            throw new InvalidOperationException("Insufficient wallet balance.");

        return balanceAfterDebit;
    }

    public static bool CanDebit(decimal currentBalance, decimal amount)
    {
        ValidateBalance(currentBalance);
        ValidateAmount(amount);
        return currentBalance >= amount;
    }

    public static void ValidateAmount(decimal amount)
    {
        if (amount <= 0m)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
    }

    public static void ValidateBalance(decimal currentBalance)
    {
        if (currentBalance < 0m)
            throw new ArgumentException("Current balance cannot be negative.", nameof(currentBalance));
    }
}
