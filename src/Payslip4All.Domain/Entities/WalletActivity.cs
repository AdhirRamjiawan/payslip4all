using Payslip4All.Domain.Enums;

namespace Payslip4All.Domain.Entities;

public class WalletActivity
{
    public const string WalletTopUpReferenceType = "WalletTopUpAttempt";
    public const string HostedCardTopUpDescription = "Hosted card wallet top-up";

    public Guid Id { get; private set; }
    public Guid WalletId { get; set; }
    public WalletActivityType ActivityType { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Description { get; set; }
    public decimal BalanceAfterActivity { get; set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Wallet Wallet { get; set; } = null!;

    public WalletActivity()
    {
        Id = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public void EnsureValid()
    {
        if (WalletId == Guid.Empty)
            throw new ArgumentException("WalletId is required.", nameof(WalletId));

        if (Amount <= 0m)
            throw new ArgumentException("Amount must be positive.", nameof(Amount));

        if (BalanceAfterActivity < 0m)
            throw new ArgumentException("Balance after activity cannot be negative.", nameof(BalanceAfterActivity));

        if (decimal.Round(Amount, 2) != Amount)
            throw new ArgumentException("Amount must use no more than two decimal places.", nameof(Amount));

        if (decimal.Round(BalanceAfterActivity, 2) != BalanceAfterActivity)
            throw new ArgumentException("Balance after activity must use no more than two decimal places.", nameof(BalanceAfterActivity));
    }
}
