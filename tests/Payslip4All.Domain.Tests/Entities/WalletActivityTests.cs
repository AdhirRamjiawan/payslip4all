using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Domain.Tests.Entities;

public class WalletActivityTests
{
    [Fact]
    public void WalletActivity_EnsureValid_RequiresPositiveAmount()
    {
        var activity = new WalletActivity
        {
            WalletId = Guid.NewGuid(),
            ActivityType = WalletActivityType.Credit,
            Amount = 0m,
            BalanceAfterActivity = 0m,
        };

        Assert.Throws<ArgumentException>(() => activity.EnsureValid());
    }

    [Fact]
    public void WalletActivity_EnsureValid_RejectsNegativeBalanceAfterActivity()
    {
        var activity = new WalletActivity
        {
            WalletId = Guid.NewGuid(),
            ActivityType = WalletActivityType.Debit,
            Amount = 10m,
            BalanceAfterActivity = -1m,
        };

        Assert.Throws<ArgumentException>(() => activity.EnsureValid());
    }

    [Fact]
    public void WalletActivity_EnsureValid_RejectsMoreThanTwoDecimalPlaces()
    {
        var activity = new WalletActivity
        {
            WalletId = Guid.NewGuid(),
            ActivityType = WalletActivityType.Credit,
            Amount = 10.123m,
            BalanceAfterActivity = 10.123m,
        };

        Assert.Throws<ArgumentException>(() => activity.EnsureValid());
    }
}
