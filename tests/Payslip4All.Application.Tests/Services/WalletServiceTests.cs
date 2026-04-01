using Moq;
using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.Tests.Services;

public class WalletServiceTests
{
    private readonly Mock<IWalletRepository> _walletRepository = new();
    private readonly Mock<IWalletActivityRepository> _activityRepository = new();
    private readonly Mock<IPayslipPricingRepository> _pricingRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private WalletService CreateService() => new(
        _walletRepository.Object,
        _activityRepository.Object,
        _pricingRepository.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task TopUpAsync_WithPositiveAmount_CreatesCreditAndUpdatesBalance()
    {
        var userId = Guid.NewGuid();
        var wallet = new Wallet { UserId = userId, CurrentBalance = 10m };
        _walletRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(wallet);
        _pricingRepository.Setup(r => r.GetCurrentAsync()).ReturnsAsync(new PayslipPricingSetting { PricePerPayslip = 5m });
        _activityRepository.Setup(r => r.GetByWalletIdAsync(wallet.Id)).ReturnsAsync(new List<WalletActivity>());

        var result = await CreateService().TopUpAsync(new AddWalletCreditCommand { UserId = userId, Amount = 15m });

        Assert.Equal(25m, wallet.CurrentBalance);
        Assert.Equal(25m, result.CurrentBalance);
        _walletRepository.Verify(r => r.UpdateAsync(wallet), Times.Once);
        _activityRepository.Verify(r => r.AddAsync(It.Is<WalletActivity>(a =>
            a.ActivityType == WalletActivityType.Credit &&
            a.Amount == 15m &&
            a.BalanceAfterActivity == 25m)), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task TopUpAsync_WithInvalidAmount_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService().TopUpAsync(new AddWalletCreditCommand { UserId = Guid.NewGuid(), Amount = 0m }));
    }

    [Fact]
    public async Task GetWalletAsync_ReturnsZeroBalance_WhenWalletDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _pricingRepository.Setup(r => r.GetCurrentAsync()).ReturnsAsync(new PayslipPricingSetting { PricePerPayslip = 12m });
        _walletRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Wallet?)null);

        var result = await CreateService().GetWalletAsync(userId);

        Assert.Equal(0m, result.CurrentBalance);
        Assert.Equal(12m, result.CurrentPayslipPrice);
        Assert.Empty(result.Activities);
    }

    [Fact]
    public async Task GetWalletAsync_ReturnsActivitiesNewestFirst()
    {
        var userId = Guid.NewGuid();
        var wallet = new Wallet { UserId = userId, CurrentBalance = 50m };
        var older = new WalletActivity
        {
            WalletId = wallet.Id,
            ActivityType = WalletActivityType.Credit,
            Amount = 20m,
            BalanceAfterActivity = 20m,
        };
        var newer = new WalletActivity
        {
            WalletId = wallet.Id,
            ActivityType = WalletActivityType.Debit,
            Amount = 5m,
            BalanceAfterActivity = 15m,
        };

        typeof(WalletActivity).GetProperty(nameof(WalletActivity.OccurredAt))!.SetValue(older, DateTimeOffset.UtcNow.AddMinutes(-5));
        typeof(WalletActivity).GetProperty(nameof(WalletActivity.OccurredAt))!.SetValue(newer, DateTimeOffset.UtcNow);

        _walletRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(wallet);
        _pricingRepository.Setup(r => r.GetCurrentAsync()).ReturnsAsync(new PayslipPricingSetting { PricePerPayslip = 5m });
        _activityRepository.Setup(r => r.GetByWalletIdAsync(wallet.Id)).ReturnsAsync(new List<WalletActivity> { older, newer });

        var result = await CreateService().GetWalletAsync(userId);

        Assert.Equal(newer.Id, result.Activities[0].Id);
        Assert.Equal(older.Id, result.Activities[1].Id);
    }

    [Fact]
    public async Task TryDebitAsync_ReturnsFalse_WhenFundsAreInsufficient()
    {
        var userId = Guid.NewGuid();
        var wallet = new Wallet { UserId = userId, CurrentBalance = 2m };
        _walletRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(wallet);

        var result = await CreateService().TryDebitAsync(userId, 5m, "Payslip charge");

        Assert.False(result);
        _walletRepository.Verify(r => r.UpdateAsync(It.IsAny<Wallet>()), Times.Never);
        _activityRepository.Verify(r => r.AddAsync(It.IsAny<WalletActivity>()), Times.Never);
    }

    [Fact]
    public async Task TryDebitAsync_WithZeroAmount_ReturnsTrueWithoutPersistingChanges()
    {
        var result = await CreateService().TryDebitAsync(Guid.NewGuid(), 0m, "Free payslip");

        Assert.True(result);
        _walletRepository.Verify(r => r.UpdateAsync(It.IsAny<Wallet>()), Times.Never);
        _activityRepository.Verify(r => r.AddAsync(It.IsAny<WalletActivity>()), Times.Never);
    }
}
