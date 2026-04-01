using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Domain.Services;

namespace Payslip4All.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletActivityRepository _walletActivityRepository;
    private readonly IPayslipPricingRepository _pricingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WalletService(
        IWalletRepository walletRepository,
        IWalletActivityRepository walletActivityRepository,
        IPayslipPricingRepository pricingRepository,
        IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _walletActivityRepository = walletActivityRepository;
        _pricingRepository = pricingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WalletDto> GetWalletAsync(Guid userId)
    {
        var price = await GetCurrentPriceAsync();
        var wallet = await _walletRepository.GetByUserIdAsync(userId);

        if (wallet == null)
        {
            return new WalletDto
            {
                UserId = userId,
                CurrentBalance = 0m,
                CurrentPayslipPrice = price,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
        }

        var activities = await _walletActivityRepository.GetByWalletIdAsync(wallet.Id);
        return MapWallet(wallet, activities, price);
    }

    public async Task<WalletDto> TopUpAsync(AddWalletCreditCommand command)
    {
        if (command.UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(command.UserId));

        WalletCalculator.ValidateAmount(command.Amount);

        var wallet = await _walletRepository.GetByUserIdAsync(command.UserId);
        var isNewWallet = wallet == null;
        wallet ??= new Wallet
        {
            UserId = command.UserId,
            CurrentBalance = 0m,
        };

        wallet.CurrentBalance = WalletCalculator.CalculateBalanceAfterCredit(wallet.CurrentBalance, command.Amount);
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        wallet.EnsureValid();

        var activity = new WalletActivity
        {
            WalletId = wallet.Id,
            ActivityType = WalletActivityType.Credit,
            Amount = command.Amount,
            Description = string.IsNullOrWhiteSpace(command.Description) ? "Wallet top-up" : command.Description,
            ReferenceType = command.ReferenceType,
            ReferenceId = command.ReferenceId,
            BalanceAfterActivity = wallet.CurrentBalance,
        };
        activity.EnsureValid();

        await ExecuteAtomicallyAsync(async () =>
        {
            if (isNewWallet)
                await _walletRepository.AddAsync(wallet);
            else
                await _walletRepository.UpdateAsync(wallet);

            await _walletActivityRepository.AddAsync(activity);
        });

        return await GetWalletAsync(command.UserId);
    }

    public async Task<IReadOnlyList<WalletActivityDto>> GetActivitiesAsync(Guid userId)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet == null)
            return Array.Empty<WalletActivityDto>();

        var activities = await _walletActivityRepository.GetByWalletIdAsync(wallet.Id);
        return activities.Select(MapActivity).ToList();
    }

    public async Task<bool> TryDebitAsync(Guid userId, decimal amount, string description, string? referenceType = null, string? referenceId = null)
    {
        if (amount == 0m)
            return true;

        WalletCalculator.ValidateAmount(amount);

        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet == null || !WalletCalculator.CanDebit(wallet.CurrentBalance, amount))
            return false;

        wallet.CurrentBalance = WalletCalculator.CalculateBalanceAfterDebit(wallet.CurrentBalance, amount);
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        wallet.EnsureValid();

        var activity = new WalletActivity
        {
            WalletId = wallet.Id,
            ActivityType = WalletActivityType.Debit,
            Amount = amount,
            Description = description,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            BalanceAfterActivity = wallet.CurrentBalance,
        };
        activity.EnsureValid();

        try
        {
            await ExecuteAtomicallyAsync(async () =>
            {
                await _walletRepository.UpdateAsync(wallet);
                await _walletActivityRepository.AddAsync(activity);
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another process", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private async Task<decimal> GetCurrentPriceAsync()
    {
        var pricing = await _pricingRepository.GetCurrentAsync();
        return pricing?.PricePerPayslip ?? PayslipPricingSetting.DefaultPricePerPayslip;
    }

    private static WalletDto MapWallet(Wallet wallet, IReadOnlyList<WalletActivity> activities, decimal currentPrice)
    {
        return new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            CurrentBalance = wallet.CurrentBalance,
            CurrentPayslipPrice = currentPrice,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.UpdatedAt,
            Activities = activities
                .OrderByDescending(a => a.OccurredAt)
                .Select(MapActivity)
                .ToList(),
        };
    }

    private static WalletActivityDto MapActivity(WalletActivity activity)
    {
        return new WalletActivityDto
        {
            Id = activity.Id,
            WalletId = activity.WalletId,
            ActivityType = activity.ActivityType,
            Amount = activity.Amount,
            ReferenceType = activity.ReferenceType,
            ReferenceId = activity.ReferenceId,
            Description = activity.Description,
            BalanceAfterActivity = activity.BalanceAfterActivity,
            OccurredAt = activity.OccurredAt,
        };
    }

    private async Task ExecuteAtomicallyAsync(Func<Task> action)
    {
        var transactionStarted = await TryBeginTransactionAsync();

        try
        {
            await action();

            if (transactionStarted)
                await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            if (transactionStarted)
                await _unitOfWork.RollbackTransactionAsync();

            throw;
        }
    }

    private async Task<bool> TryBeginTransactionAsync()
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
