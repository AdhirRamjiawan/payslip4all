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
    private readonly IPaymentReturnEvidenceRepository _evidenceRepository;
    private readonly IOutcomeNormalizationDecisionRepository _decisionRepository;
    private readonly IUnmatchedPaymentReturnRecordRepository _unmatchedRepository;
    private readonly IReadOnlyDictionary<string, IHostedPaymentProvider> _providers;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTopUpOutcomeNormalizer _normalizer;
    private readonly IWalletTopUpAbandonmentService _abandonmentService;
    private readonly ITimeProvider _timeProvider;

    public WalletTopUpService(
        IWalletTopUpAttemptRepository attemptRepository,
        IEnumerable<IHostedPaymentProvider> providers,
        IWalletRepository walletRepository,
        IPaymentReturnEvidenceRepository evidenceRepository,
        IOutcomeNormalizationDecisionRepository decisionRepository,
        IUnmatchedPaymentReturnRecordRepository unmatchedRepository,
        IWalletTopUpOutcomeNormalizer normalizer,
        IWalletTopUpAbandonmentService abandonmentService,
        ITimeProvider timeProvider)
    {
        _attemptRepository = attemptRepository;
        _walletRepository = walletRepository;
        _evidenceRepository = evidenceRepository;
        _decisionRepository = decisionRepository;
        _unmatchedRepository = unmatchedRepository;
        _normalizer = normalizer;
        _abandonmentService = abandonmentService;
        _timeProvider = timeProvider;
        _providers = providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<StartWalletTopUpResultDto> StartHostedTopUpAsync(StartWalletTopUpCommand command, CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(command.UserId));

        WalletCalculator.ValidateAmount(command.RequestedAmount);

        if (!Uri.TryCreate(command.ReturnUrl, UriKind.Absolute, out var returnUrl))
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
                HostedPageDeadline = hostedSession.HostedPageDeadline,
                AbandonAfterUtc = attempt.AbandonAfterUtc
            };
        }
        catch
        {
            attempt.MarkUnverified("topup_initiation_failed", "The hosted payment page could not be opened right now. Please try again.", _timeProvider.UtcNow);
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

        if (attempt.Status is WalletTopUpAttemptStatus.Completed or WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Expired or WalletTopUpAttemptStatus.Abandoned or WalletTopUpAttemptStatus.Unverified)
            return await MapResultAsync(attempt, attempt.Status == WalletTopUpAttemptStatus.Completed, cancellationToken);

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
                attempt.AuthoritativeOutcomeAcceptedAt ??= attempt.LastValidatedAt;
                return MapResult(attempt, settlement.WalletBalance, validation.DisplayMessage, settlement.CreditedNow || attempt.CreditedWalletActivityId.HasValue);

            case HostedPaymentOutcome.Cancelled:
                attempt.MarkCancelled(validation.FailureCode, validation.FailureMessage, validation.ValidatedAt);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                return await MapResultAsync(attempt, false, cancellationToken, validation.DisplayMessage);

            case HostedPaymentOutcome.Expired:
                attempt.MarkExpired(validation.FailureCode, validation.FailureMessage, validation.ValidatedAt);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                return await MapResultAsync(attempt, false, cancellationToken, validation.DisplayMessage);

            case HostedPaymentOutcome.Failed:
            case HostedPaymentOutcome.Unverified:
                attempt.MarkUnverified(validation.FailureCode, validation.FailureMessage, validation.ValidatedAt);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                return await MapResultAsync(attempt, false, cancellationToken, validation.DisplayMessage);

            default:
                attempt.MarkPendingValidation(validation.ValidatedAt, validation.FailureCode, validation.FailureMessage);
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                return await MapResultAsync(attempt, false, cancellationToken, validation.DisplayMessage);
        }
    }

    public async Task<GenericHostedReturnResultDto> ProcessGenericReturnAsync(Guid userId, IReadOnlyDictionary<string, string> returnPayload, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var provider = ResolveProviderFromPayload(returnPayload);
        var parsedEvidence = await provider.ParseReturnEvidenceAsync(returnPayload, cancellationToken);
        var correlatedAttempt = !string.IsNullOrWhiteSpace(parsedEvidence.ReturnCorrelationToken)
            ? await _attemptRepository.GetByCorrelationTokenAsync(parsedEvidence.ReturnCorrelationToken, cancellationToken)
            : null;
        var matchedAttempt = correlatedAttempt?.UserId == userId ? correlatedAttempt : null;

        var evidence = MapEvidence(parsedEvidence, matchedAttempt);
        await _evidenceRepository.AddAsync(evidence, cancellationToken);

        var resolution = await _normalizer.NormalizeAsync(evidence, matchedAttempt, cancellationToken);

        if (matchedAttempt == null || resolution.CorrelationDisposition != PaymentReturnCorrelationDisposition.ExactMatch)
        {
            var unmatched = new UnmatchedPaymentReturnRecord
            {
                PrimaryEvidenceId = evidence.Id,
                ProviderKey = evidence.ProviderKey,
                CorrelationDisposition = evidence.CorrelationDisposition.ToString(),
                GenericResultCode = "unmatched",
                DisplayMessage = "We could not confirm this payment return. If your card was charged, please contact support.",
                SafePayloadSnapshot = evidence.SafePayloadSnapshot,
                ReceivedAt = evidence.ReceivedAt
            };
            await _unmatchedRepository.AddAsync(unmatched, cancellationToken);
            await _decisionRepository.AddAsync(BuildDecision(resolution, matchedAttempt, unmatched.Id), cancellationToken);

            return new GenericHostedReturnResultDto
            {
                IsMatched = false,
                GenericResultCode = unmatched.GenericResultCode,
                DisplayMessage = unmatched.DisplayMessage,
                UnmatchedRecordId = unmatched.Id
            };
        }

        var beforeStatus = matchedAttempt.Status;
        WalletTopUpSettlementResult? settlement = null;

        matchedAttempt.LastEvidenceReceivedAt = evidence.ReceivedAt;
        matchedAttempt.LastEvaluatedAt = evidence.ValidatedAt;

        if (!resolution.ConflictWithAcceptedFinal)
        {
            switch (resolution.NormalizedOutcome)
            {
                case PaymentReturnClaimedOutcome.Completed when resolution.IsAuthoritative:
                    matchedAttempt.RecordValidatedSuccess(
                        resolution.ConfirmedChargedAmount ?? matchedAttempt.RequestedAmount,
                        evidence.ProviderPaymentReference,
                        evidence.ValidatedAt);
                    matchedAttempt.AuthoritativeEvidenceId = evidence.Id;
                    matchedAttempt.AuthoritativeOutcomeAcceptedAt = evidence.ValidatedAt;
                    settlement = await _attemptRepository.SettleSuccessfulAsync(matchedAttempt, cancellationToken);
                    matchedAttempt.CreditedWalletActivityId = settlement.WalletActivityId;
                    matchedAttempt.Status = WalletTopUpAttemptStatus.Completed;
                    matchedAttempt.CompletedAt = matchedAttempt.AuthoritativeOutcomeAcceptedAt;
                    break;

                case PaymentReturnClaimedOutcome.Cancelled when resolution.IsAuthoritative:
                    matchedAttempt.AcceptTrustworthyEvidence(evidence.Id, PaymentReturnClaimedOutcome.Cancelled, null, evidence.ProviderPaymentReference, evidence.ValidatedAt, null);
                    await _attemptRepository.UpdateAsync(matchedAttempt, cancellationToken);
                    break;

                case PaymentReturnClaimedOutcome.Expired when resolution.IsAuthoritative:
                    matchedAttempt.AcceptTrustworthyEvidence(evidence.Id, PaymentReturnClaimedOutcome.Expired, null, evidence.ProviderPaymentReference, evidence.ValidatedAt, null);
                    await _attemptRepository.UpdateAsync(matchedAttempt, cancellationToken);
                    break;

                default:
                    if (matchedAttempt.Status == WalletTopUpAttemptStatus.Pending || matchedAttempt.Status == WalletTopUpAttemptStatus.Unverified)
                    {
                        matchedAttempt.MarkUnverified(resolution.ReasonCode, resolution.ResolutionSummary, evidence.ValidatedAt);
                        matchedAttempt.AuthoritativeEvidenceId = null;
                        await _attemptRepository.UpdateAsync(matchedAttempt, cancellationToken);
                    }
                    break;
            }
        }

        resolution.WalletActivityId = settlement?.WalletActivityId;
        await _decisionRepository.AddAsync(BuildDecision(resolution, matchedAttempt, null, beforeStatus), cancellationToken);

        return new GenericHostedReturnResultDto
        {
            IsMatched = true,
            MatchedAttemptId = matchedAttempt.Id,
            GenericResultCode = resolution.IsAuthoritative ? "matched" : "matched-review",
            DisplayMessage = resolution.ResolutionSummary ?? "Payment return processed."
        };
    }

    public async Task<FinalizedWalletTopUpResultDto?> GetAttemptResultAsync(Guid attemptId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
            return null;

        return await MapResultAsync(attempt, attempt.Status == WalletTopUpAttemptStatus.Completed, cancellationToken);
    }

    public Task AbandonExpiredAttemptsAsync(CancellationToken cancellationToken = default)
        => _abandonmentService.AbandonExpiredAttemptsAsync(cancellationToken);

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
                AbandonAfterUtc = a.AbandonAfterUtc,
                AuthoritativeOutcomeAcceptedAt = a.AuthoritativeOutcomeAcceptedAt,
                CreditedWalletActivityId = a.CreditedWalletActivityId,
                FailureMessage = a.FailureMessage,
                OutcomeMessage = a.OutcomeMessage
            })
            .ToList();
    }

    private PaymentReturnEvidence MapEvidence(HostedPaymentReturnEvidenceDto dto, WalletTopUpAttempt? matchedAttempt)
    {
        var receivedAt = dto.ReceivedAt == default ? _timeProvider.UtcNow : dto.ReceivedAt;
        var validatedAt = dto.ValidatedAt == default ? receivedAt : dto.ValidatedAt;
        var evidence = new PaymentReturnEvidence
        {
            ProviderKey = string.IsNullOrWhiteSpace(dto.ProviderKey) ? ResolveDefaultProvider().ProviderKey : dto.ProviderKey,
            SourceChannel = dto.SourceChannel,
            ProviderSessionReference = dto.ProviderSessionReference,
            ProviderPaymentReference = dto.ProviderPaymentReference,
            ReturnCorrelationToken = dto.ReturnCorrelationToken,
            MatchedAttemptId = matchedAttempt?.Id,
            ClaimedOutcome = dto.ClaimedOutcome,
            TrustLevel = dto.TrustLevel,
            ConfirmedChargedAmount = dto.ConfirmedChargedAmount,
            EvidenceOccurredAt = dto.EvidenceOccurredAt,
            ValidatedAt = validatedAt,
            IsAtOrAfterAbandonmentThreshold = matchedAttempt != null && receivedAt >= matchedAttempt.AbandonAfterUtc,
            SafePayloadSnapshot = dto.SafePayloadSnapshot,
            ValidationMessage = dto.ValidationMessage
        };

        if (dto.Id != Guid.Empty)
            typeof(PaymentReturnEvidence).GetProperty(nameof(PaymentReturnEvidence.Id))!.SetValue(evidence, dto.Id);
        typeof(PaymentReturnEvidence).GetProperty(nameof(PaymentReturnEvidence.ReceivedAt))!.SetValue(evidence, receivedAt);

        if (matchedAttempt == null)
        {
            evidence.CorrelationDisposition = string.IsNullOrWhiteSpace(dto.ReturnCorrelationToken)
                ? PaymentReturnCorrelationDisposition.MissingData
                : PaymentReturnCorrelationDisposition.NoMatch;
            evidence.TrustLevel = PaymentReturnTrustLevel.Untrusted;
        }
        else if (!string.Equals(matchedAttempt.ProviderSessionReference, dto.ProviderSessionReference, StringComparison.Ordinal))
        {
            evidence.CorrelationDisposition = PaymentReturnCorrelationDisposition.ConflictingData;
            evidence.TrustLevel = PaymentReturnTrustLevel.Untrusted;
        }
        else
        {
            evidence.CorrelationDisposition = PaymentReturnCorrelationDisposition.ExactMatch;
            if (dto.ClaimedOutcome == PaymentReturnClaimedOutcome.Completed)
            {
                evidence.TrustLevel = dto.ConfirmedChargedAmount.HasValue
                    ? PaymentReturnTrustLevel.Trustworthy
                    : PaymentReturnTrustLevel.LowConfidence;
            }
            else if (dto.ClaimedOutcome is PaymentReturnClaimedOutcome.Cancelled or PaymentReturnClaimedOutcome.Expired)
            {
                evidence.TrustLevel = PaymentReturnTrustLevel.Trustworthy;
            }
        }

        return evidence;
    }

    private OutcomeNormalizationDecision BuildDecision(
        HostedPaymentReturnResolutionDto resolution,
        WalletTopUpAttempt? attempt,
        Guid? unmatchedRecordId,
        WalletTopUpAttemptStatus? beforeStatus = null)
    {
        return new OutcomeNormalizationDecision
        {
            AttemptId = attempt?.Id,
            PaymentReturnEvidenceId = resolution.EvidenceId,
            UnmatchedPaymentReturnRecordId = unmatchedRecordId,
            DecisionType = unmatchedRecordId.HasValue ? "UnmatchedReturn" : "EvidenceEvaluation",
            AppliedPrecedence = resolution.CorrelationDisposition.ToString(),
            NormalizedOutcome = resolution.NormalizedOutcome?.ToString() ?? "Unknown",
            AuthoritativeOutcomeBefore = beforeStatus?.ToString() ?? attempt?.Status.ToString(),
            AuthoritativeOutcomeAfter = attempt?.Status.ToString(),
            DecisionReasonCode = resolution.ReasonCode ?? "processed",
            DecisionSummary = resolution.ResolutionSummary ?? "Payment return processed.",
            SupersededAbandonment = resolution.SupersededAbandonment,
            ConflictWithAcceptedFinalOutcome = resolution.ConflictWithAcceptedFinal,
            WalletEffect = resolution.WalletEffect,
            WalletActivityId = resolution.WalletActivityId
        };
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
            OutcomeReasonCode = attempt.OutcomeReasonCode,
            OutcomeMessage = attempt.OutcomeMessage,
            AuthoritativeOutcomeAcceptedAt = attempt.AuthoritativeOutcomeAcceptedAt,
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
            WalletTopUpAttemptStatus.Abandoned => "The hosted payment session was abandoned before trustworthy evidence was received.",
            WalletTopUpAttemptStatus.Unverified => "We could not verify this payment return yet. Your wallet was not credited.",
            _ => "Payment is still pending. Your wallet has not been credited yet."
        };

    private IHostedPaymentProvider ResolveDefaultProvider()
        => _providers.Values.FirstOrDefault()
           ?? throw new InvalidOperationException("No hosted payment providers are registered.");

    private IHostedPaymentProvider ResolveProvider(string providerKey)
        => _providers.TryGetValue(providerKey, out var provider)
            ? provider
            : throw new InvalidOperationException($"Hosted payment provider '{providerKey}' is not registered.");

    private IHostedPaymentProvider ResolveProviderFromPayload(IReadOnlyDictionary<string, string> payload)
    {
        if (payload.TryGetValue("provider", out var providerKey) && !string.IsNullOrWhiteSpace(providerKey))
            return ResolveProvider(providerKey);

        return ResolveDefaultProvider();
    }
}
