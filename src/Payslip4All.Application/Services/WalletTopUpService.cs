using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Domain.Services;

namespace Payslip4All.Application.Services;

public class WalletTopUpService : IWalletTopUpService
{
    private readonly IWalletTopUpAttemptRepository _attemptRepository;
    private readonly IReadOnlyDictionary<string, IHostedPaymentProvider> _providers;
    private readonly IWalletRepository _walletRepository;

    public WalletTopUpService(
        IWalletTopUpAttemptRepository attemptRepository,
        IEnumerable<IHostedPaymentProvider> providers,
        IWalletRepository walletRepository)
    {
        _attemptRepository = attemptRepository;
        _walletRepository = walletRepository;
        _providers = providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<StartWalletTopUpResultDto> StartHostedTopUpAsync(StartWalletTopUpCommand command, CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(command.UserId));

        WalletCalculator.ValidateAmount(command.RequestedAmount);

        var resolvedReturnUrl = command.ReturnUrl.Replace("{attemptId}", Guid.Empty.ToString(), StringComparison.OrdinalIgnoreCase);
        if (!Uri.TryCreate(resolvedReturnUrl, UriKind.Absolute, out _))
            throw new ArgumentException("A valid absolute return URL is required.", nameof(command.ReturnUrl));

        Uri? cancelUrl = null;
        if (!string.IsNullOrWhiteSpace(command.CancelUrl))
        {
            if (!Uri.TryCreate(command.CancelUrl, UriKind.Absolute, out cancelUrl))
                throw new ArgumentException("A valid absolute cancel URL is required.", nameof(command.CancelUrl));
        }

        var provider = ResolveDefaultProvider();
        var attempt = WalletTopUpAttempt.CreatePending(command.UserId, command.RequestedAmount, provider.ProviderKey);
        await _attemptRepository.AddAsync(attempt, cancellationToken);

        try
        {
            var actualReturnUrlValue = command.ReturnUrl.Replace("{attemptId}", attempt.Id.ToString(), StringComparison.OrdinalIgnoreCase);
            var returnUrl = new Uri(actualReturnUrlValue, UriKind.Absolute);
            var hostedSession = await provider.StartHostedTopUpAsync(attempt, returnUrl, cancelUrl, cancellationToken);
            attempt.RegisterHostedSession(
                hostedSession.ProviderSessionReference,
                hostedSession.ReturnCorrelationToken,
                hostedSession.HostedPageDeadline);
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);

            return new StartWalletTopUpResultDto
            {
                WalletTopUpAttemptId = attempt.Id,
                RedirectUrl = hostedSession.RedirectUrl,
                Status = attempt.Status,
                HostedPageDeadline = hostedSession.HostedPageDeadline
            };
        }
        catch
        {
            attempt.MarkFailed("topup_initiation_failed", "The hosted payment page could not be opened right now. Please try again.", DateTimeOffset.UtcNow);
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            throw new InvalidOperationException("The hosted payment page could not be opened right now. Please try again.");
        }
    }

    public async Task<FinalizedWalletTopUpResultDto> FinalizeHostedReturnAsync(FinalizeWalletTopUpReturnCommand command, CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(command.UserId));

        var attempt = await _attemptRepository.GetByIdAsync(command.WalletTopUpAttemptId, command.UserId, cancellationToken);
        if (attempt == null)
            throw new InvalidOperationException("Top-up attempt could not be found.");

        if (attempt.Status == WalletTopUpAttemptStatus.Completed && attempt.CreditedWalletActivityId.HasValue)
            return await MapResultAsync(attempt, true, cancellationToken);

        if (attempt.Status is WalletTopUpAttemptStatus.Failed or WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Expired)
            return await MapResultAsync(attempt, false, cancellationToken);

        if (command.ReturnPayload.Count == 0)
            return await MapResultAsync(attempt, false, cancellationToken);

        var provider = ResolveProvider(attempt.ProviderKey);
        var validation = await provider.ValidateReturnAsync(attempt, command.ReturnPayload, cancellationToken);

        switch (validation.Outcome)
        {
            case HostedPaymentOutcome.Succeeded:
                if (!validation.ConfirmedChargedAmount.HasValue)
                    throw new InvalidOperationException("Validated successful payments must include a confirmed charged amount.");

                attempt.RecordValidatedSuccess(
                    validation.ConfirmedChargedAmount.Value,
                    validation.ProviderPaymentReference,
                    validation.ValidatedAt);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);

                var settlement = await _attemptRepository.SettleSuccessfulAsync(attempt, cancellationToken);
                attempt.CreditedWalletActivityId = settlement.WalletActivityId;
                attempt.Status = WalletTopUpAttemptStatus.Completed;
                attempt.CompletedAt ??= attempt.LastValidatedAt;
                return MapResult(attempt, settlement.WalletBalance, validation.DisplayMessage, settlement.CreditedNow || attempt.CreditedWalletActivityId.HasValue);

            case HostedPaymentOutcome.Failed:
                attempt.MarkFailed(validation.FailureCode, validation.FailureMessage, validation.ValidatedAt);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                return await MapResultAsync(attempt, false, cancellationToken, validation.DisplayMessage);

            case HostedPaymentOutcome.Cancelled:
                attempt.MarkCancelled(validation.FailureCode, validation.FailureMessage, validation.ValidatedAt);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                return await MapResultAsync(attempt, false, cancellationToken, validation.DisplayMessage);

            case HostedPaymentOutcome.Expired:
                attempt.MarkExpired(validation.FailureCode, validation.FailureMessage, validation.ValidatedAt);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                return await MapResultAsync(attempt, false, cancellationToken, validation.DisplayMessage);

            default:
                attempt.MarkPendingValidation(validation.ValidatedAt, validation.FailureCode, validation.FailureMessage);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                return await MapResultAsync(attempt, false, cancellationToken, validation.DisplayMessage);
        }
    }

    public async Task<IReadOnlyList<WalletTopUpAttemptDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var attempts = await _attemptRepository.GetByUserIdAsync(userId, cancellationToken);
        return attempts
            .Select(a => new WalletTopUpAttemptDto
            {
                Id = a.Id,
                RequestedAmount = a.RequestedAmount,
                ConfirmedChargedAmount = a.ConfirmedChargedAmount,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                RedirectedAt = a.RedirectedAt,
                CompletedAt = a.CompletedAt,
                HostedPageDeadline = a.HostedPageDeadline,
                CreditedWalletActivityId = a.CreditedWalletActivityId,
                FailureMessage = a.FailureMessage
            })
            .ToList();
    }

    private async Task<FinalizedWalletTopUpResultDto> MapResultAsync(
        WalletTopUpAttempt attempt,
        bool creditedWallet,
        CancellationToken cancellationToken,
        string? displayMessage = null)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(attempt.UserId);
        return MapResult(attempt, wallet?.CurrentBalance ?? 0m, displayMessage, creditedWallet);
    }

    private static FinalizedWalletTopUpResultDto MapResult(WalletTopUpAttempt attempt, decimal walletBalance, string? displayMessage, bool creditedWallet)
    {
        return new FinalizedWalletTopUpResultDto
        {
            WalletTopUpAttemptId = attempt.Id,
            Status = attempt.Status,
            RequestedAmount = attempt.RequestedAmount,
            ConfirmedChargedAmount = attempt.ConfirmedChargedAmount,
            WalletBalance = walletBalance,
            CreditedWalletActivityId = attempt.CreditedWalletActivityId,
            CreditedWallet = creditedWallet && attempt.Status == WalletTopUpAttemptStatus.Completed,
            FailureCode = attempt.FailureCode,
            FailureMessage = attempt.FailureMessage,
            DisplayMessage = string.IsNullOrWhiteSpace(displayMessage)
                ? GetDefaultDisplayMessage(attempt.Status)
                : displayMessage
        };
    }

    private static string GetDefaultDisplayMessage(WalletTopUpAttemptStatus status)
        => status switch
        {
            WalletTopUpAttemptStatus.Completed => "Wallet credited successfully.",
            WalletTopUpAttemptStatus.Cancelled => "Payment was cancelled. Your wallet was not credited.",
            WalletTopUpAttemptStatus.Expired => "Payment expired. Your wallet was not credited.",
            WalletTopUpAttemptStatus.Failed => "Payment failed. Your wallet was not credited.",
            _ => "Payment is still pending. Your wallet has not been credited yet."
        };

    private IHostedPaymentProvider ResolveDefaultProvider()
        => _providers.Values.FirstOrDefault()
           ?? throw new InvalidOperationException("No hosted payment providers are registered.");

    private IHostedPaymentProvider ResolveProvider(string providerKey)
        => _providers.TryGetValue(providerKey, out var provider)
            ? provider
            : throw new InvalidOperationException($"Hosted payment provider '{providerKey}' is not registered.");
}
