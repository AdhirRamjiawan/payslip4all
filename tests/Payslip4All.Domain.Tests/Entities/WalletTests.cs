using Payslip4All.Domain.Entities;

namespace Payslip4All.Domain.Tests.Entities;

public class WalletTests
{
    [Fact]
    public void Wallet_InitializesWithZeroBalance()
    {
        var wallet = new Wallet();

        Assert.NotEqual(Guid.Empty, wallet.Id);
        Assert.Equal(0m, wallet.CurrentBalance);
        Assert.True(wallet.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Wallet_EnsureValid_RequiresUserId()
    {
        var wallet = new Wallet();

        Assert.Throws<ArgumentException>(() => wallet.EnsureValid());
    }

    [Fact]
    public void Wallet_AllowsOneUserIdAssignmentForOneWalletInstance()
    {
        var userId = Guid.NewGuid();
        var wallet = new Wallet { UserId = userId };

        wallet.EnsureValid();

        Assert.Equal(userId, wallet.UserId);
    }
}
