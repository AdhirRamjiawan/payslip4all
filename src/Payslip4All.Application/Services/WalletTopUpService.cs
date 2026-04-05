using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
using Payslip4All.Domain.Services;

namespace Payslip4All.Application.Services;

public class WalletTopUpService : IWalletTopUpService
{
    private const string GenericNotConfirmedMessage = "Top-up not confirmed";
    private const string PendingConfirmationMessage = "Payment is still pending. Your wallet has not been credited yet.";

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

        var provider = !string.IsNullOrWhiteSpace(command.ProviderKey)
            ? ResolveProvider(command.ProviderKey)
            : ResolveDefaultProvider();
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
                MerchantPaymentReference = attempt.MerchantPaymentReference,
                RedirectUrl = hostedSession.RedirectUrl,
                Status = attempt.Status,
                HostedPageDeadline = hostedSession.HostedPageDeadline,
                NextReconciliationDueAt = attempt.NextReconciliationDueAt,
                AbandonAfterUtc = attempt.AbandonAfterUtc
            };
        }
        catch (Exception ex)
        {
            attempt.MarkNotConfirmed("payment_start_failed", GenericNotConfirmedMessage, _timeProvider.UtcNow);
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            throw new InvalidOperationException("Payment could not be started", ex);
        }
    }

    public async Task<FinalizedWalletTopUpResultDto> FinalizeHostedReturnAsync(FinalizeWalletTopUpReturnCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ReturnPayload.Count > 0)
            await ProcessGenericReturnAsync(command.UserId, command.ReturnPayload, cancellationToken);

        var result = await GetAttemptResultAsync(command.WalletTopUpAttemptId, command.UserId, cancellationToken);
        return result ?? throw new InvalidOperationException("Top-up attempt could not be found.");
    }

    public async Task<GenericHostedReturnResultDto> ProcessGenericReturnAsync(Guid userId, IReadOnlyDictionary<string, string> returnPayload, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var provider = ResolveProviderFromPayload(returnPayload);
        var parsedEvidence = await provider.ParseReturnEvidenceAsync(returnPayload, cancellationToken);
        var correlatedAttempt = await CorrelateBrowserAttemptAsync(parsedEvidence, cancellationToken);
        var matchedAttempt = correlatedAttempt?.UserId == userId ? correlatedAttempt : null;

        var evidence = MapEvidence(parsedEvidence, matchedAttempt);
        evidence.CorrelationDisposition = matchedAttempt == null
            ? correlatedAttempt == null ? evidence.CorrelationDisposition : PaymentReturnCorrelationDisposition.ForeignOwner
            : PaymentReturnCorrelationDisposition.ExactMatch;
        evidence.TrustLevel = PaymentReturnTrustLevel.Untrusted;
        await _evidenceRepository.AddAsync(evidence, cancellationToken);

        var resolution = await _normalizer.NormalizeAsync(evidence, matchedAttempt, cancellationToken);

        if (matchedAttempt == null || evidence.CorrelationDisposition != PaymentReturnCorrelationDisposition.ExactMatch)
        {
            var unmatched = await PersistUnmatchedAsync(evidence, cancellationToken);
            await _decisionRepository.AddAsync(BuildDecision(resolution, matchedAttempt, unmatched.Id), cancellationToken);

            return new GenericHostedReturnResultDto
            {
                IsMatched = false,
                GenericResultCode = unmatched.GenericResultCode,
                DisplayMessage = unmatched.DisplayMessage,
                UnmatchedRecordId = unmatched.Id
            };
        }

        if (!matchedAttempt.IsFinalForSettlement)
        {
            matchedAttempt.LastEvidenceReceivedAt = evidence.ReceivedAt;
            matchedAttempt.MarkPendingValidation(evidence.ValidatedAt, "browser_return_informational", PendingConfirmationMessage);
            await _attemptRepository.UpdateAsync(matchedAttempt, cancellationToken);
        }

        await _decisionRepository.AddAsync(BuildDecision(resolution, matchedAttempt, null, matchedAttempt.Status), cancellationToken);

        return new GenericHostedReturnResultDto
        {
            IsMatched = true,
            MatchedAttemptId = matchedAttempt.Id,
            GenericResultCode = "matched",
            DisplayMessage = GenericNotConfirmedMessage
        };
    }

    public async Task ProcessAuthoritativeCallbackAsync(string providerKey, IReadOnlyDictionary<string, string> payload, CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(providerKey);
        var parsedEvidence = await provider.ParseAuthoritativeEvidenceAsync(payload, cancellationToken);
        var correlatedAttempts = !string.IsNullOrWhiteSpace(parsedEvidence.MerchantPaymentReference)
            ? await _attemptRepository.GetByMerchantPaymentReferenceAsync(parsedEvidence.MerchantPaymentReference, cancellationToken)
            : Array.Empty<WalletTopUpAttempt>();

        var auditAttempt = correlatedAttempts.Count == 1 ? correlatedAttempts[0] : null;
        var matchedAttempt = DetermineAuthoritativeMatch(parsedEvidence, correlatedAttempts);
        var evidence = MapEvidence(parsedEvidence, matchedAttempt);
        evidence.CorrelationDisposition = DetermineCorrelationDisposition(parsedEvidence, correlatedAttempts, matchedAttempt);
        await _evidenceRepository.AddAsync(evidence, cancellationToken);

        var normalizationAttempt = matchedAttempt ?? (auditAttempt?.IsFinalForSettlement == true ? auditAttempt : null);
        var resolution = await _normalizer.NormalizeAsync(evidence, normalizationAttempt, cancellationToken);
        if (matchedAttempt == null || evidence.CorrelationDisposition != PaymentReturnCorrelationDisposition.ExactMatch)
        {
            var unmatched = await PersistUnmatchedAsync(evidence, cancellationToken);
            await _decisionRepository.AddAsync(BuildDecision(resolution, normalizationAttempt, unmatched.Id, normalizationAttempt?.Status), cancellationToken);
            return;
        }

        matchedAttempt.LastEvidenceReceivedAt = evidence.ReceivedAt;
        matchedAttempt.LastEvaluatedAt = evidence.ValidatedAt;
        matchedAttempt.AuthoritativeEvidenceId = evidence.Id;

        WalletTopUpSettlementResult? settlement = null;
        var beforeStatus = matchedAttempt.Status;

        if (resolution.IsAuthoritative && resolution.NormalizedOutcome == PaymentReturnClaimedOutcome.Completed)
        {
            if (!resolution.ConfirmedChargedAmount.HasValue || !IsAmountInAllowedBounds(resolution.ConfirmedChargedAmount.Value))
            {
                matchedAttempt.MarkNotConfirmed("invalid_confirmed_amount", GenericNotConfirmedMessage, evidence.ValidatedAt);
                await _attemptRepository.UpdateAsync(matchedAttempt, cancellationToken);
            }
            else
            {
                matchedAttempt.RecordValidatedSuccess(
                    resolution.ConfirmedChargedAmount.Value,
                    evidence.ProviderPaymentReference,
                    evidence.ValidatedAt);
                matchedAttempt.AuthoritativeOutcomeAcceptedAt = evidence.ValidatedAt;
                settlement = await _attemptRepository.SettleSuccessfulAsync(matchedAttempt, cancellationToken);
                matchedAttempt.CreditedWalletActivityId = settlement.WalletActivityId;
                matchedAttempt.Status = WalletTopUpAttemptStatus.Completed;
                matchedAttempt.CompletedAt = evidence.ValidatedAt;
            }
        }
        else if (resolution.IsAuthoritative && resolution.NormalizedOutcome == PaymentReturnClaimedOutcome.Cancelled)
        {
            matchedAttempt.MarkCancelled("cancelled", "Payment was cancelled.", evidence.ValidatedAt);
            await _attemptRepository.UpdateAsync(matchedAttempt, cancellationToken);
        }
        else
        {
            matchedAttempt.MarkNotConfirmed(resolution.ReasonCode, GenericNotConfirmedMessage, evidence.ValidatedAt);
            await _attemptRepository.UpdateAsync(matchedAttempt, cancellationToken);
        }

        resolution.WalletActivityId = settlement?.WalletActivityId;
        await _decisionRepository.AddAsync(BuildDecision(resolution, matchedAttempt, null, beforeStatus), cancellationToken);
    }

    public async Task<FinalizedWalletTopUpResultDto?> GetAttemptResultAsync(Guid attemptId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken);
        if (attempt == null)
            return null;

        await _abandonmentService.ReconcileAttemptIfDueAsync(attempt, "ReadThroughResult", cancellationToken);
        attempt = await _attemptRepository.GetByIdAsync(attemptId, userId, cancellationToken) ?? attempt;
        return await MapResultAsync(attempt, attempt.Status == WalletTopUpAttemptStatus.Completed, cancellationToken);
    }

    public Task AbandonExpiredAttemptsAsync(CancellationToken cancellationToken = default)
        => _abandonmentService.AbandonExpiredAttemptsAsync(cancellationToken);

    public async Task<IReadOnlyList<WalletTopUpAttemptDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var attempts = await _attemptRepository.GetByUserIdAsync(userId, cancellationToken);
        foreach (var attempt in attempts.Where(a => a.NextReconciliationDueAt.HasValue && a.NextReconciliationDueAt.Value <= _timeProvider.UtcNow))
            await _abandonmentService.ReconcileAttemptIfDueAsync(attempt, "ReadThroughHistory", cancellationToken);

        attempts = await _attemptRepository.GetByUserIdAsync(userId, cancellationToken);
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
                CancelledAt = a.CancelledAt,
                ExpiredAt = a.ExpiredAt,
                AbandonedAt = a.AbandonedAt,
                HostedPageDeadline = a.HostedPageDeadline,
                NextReconciliationDueAt = a.NextReconciliationDueAt,
                AbandonAfterUtc = a.AbandonAfterUtc,
                AuthoritativeOutcomeAcceptedAt = a.AuthoritativeOutcomeAcceptedAt,
                CreditedWalletActivityId = a.CreditedWalletActivityId,
                AuthoritativeEvidenceId = a.AuthoritativeEvidenceId,
                FailureMessage = a.FailureMessage,
                OutcomeMessage = a.OutcomeMessage
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SiteAdministratorPaymentReviewDto>> GetAdminReviewAsync(SiteAdministratorPaymentReviewQueryDto query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!query.RequestingUserIsSiteAdministrator)
            throw new UnauthorizedAccessException("Only SiteAdministrator users may review wallet top-up audit records.");

        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc > query.ToUtc)
            throw new ArgumentException("FromUtc must be earlier than or equal to ToUtc.", nameof(query));

        var results = new List<SiteAdministratorPaymentReviewDto>();
        var specificEvidence = query.PaymentConfirmationRecordId.HasValue
            ? await _evidenceRepository.GetByIdAsync(query.PaymentConfirmationRecordId.Value, cancellationToken)
            : null;

        Guid? attemptIdFilter = query.AttemptId;
        if (!attemptIdFilter.HasValue && specificEvidence?.MatchedAttemptId is Guid evidenceAttemptId)
            attemptIdFilter = evidenceAttemptId;

        if (!query.UnmatchedOnly)
        {
            var attempts = await _attemptRepository.GetForAdminReviewAsync(
                attemptIdFilter,
                query.FromUtc,
                query.ToUtc,
                query.Outcome,
                cancellationToken);

            foreach (var attempt in attempts)
            {
                var evidences = await _evidenceRepository.GetByAttemptIdAsync(attempt.Id, cancellationToken);
                if (specificEvidence != null)
                    evidences = evidences.Where(e => e.Id == specificEvidence.Id).ToList();

                var decisions = await _decisionRepository.GetByAttemptIdAsync(attempt.Id, cancellationToken);
                var latestDecision = decisions.OrderByDescending(d => d.DecidedAt).FirstOrDefault();
                var authoritativeEvidence = specificEvidence?.MatchedAttemptId == attempt.Id
                    ? specificEvidence
                    : evidences
                        .Where(IsPaymentConfirmationRecord)
                        .OrderByDescending(e => e.ValidatedAt)
                        .FirstOrDefault();
                var latestEvidence = authoritativeEvidence
                    ?? evidences.OrderByDescending(e => e.ValidatedAt).FirstOrDefault();

                if (query.ConflictsOnly && latestDecision?.ConflictWithAcceptedFinalOutcome != true)
                    continue;

                results.Add(new SiteAdministratorPaymentReviewDto
                {
                    WalletTopUpAttemptId = attempt.Id,
                    OwnerUserId = attempt.UserId,
                    RequestedAmount = attempt.RequestedAmount,
                    ConfirmedChargedAmount = attempt.ConfirmedChargedAmount,
                    Status = attempt.Status,
                    CreditedWallet = attempt.CreditedWalletActivityId.HasValue,
                    WalletActivityId = attempt.CreditedWalletActivityId,
                    PaymentConfirmationRecordId = authoritativeEvidence?.Id,
                    EvidenceSourceChannel = latestEvidence?.SourceChannel,
                    DecisionType = latestDecision?.DecisionType,
                    DecisionReasonCode = latestDecision?.DecisionReasonCode,
                    DecisionSummary = latestDecision?.DecisionSummary,
                    ConflictWithAcceptedFinalOutcome = latestDecision?.ConflictWithAcceptedFinalOutcome == true,
                    CorrelationDisposition = latestEvidence?.CorrelationDisposition.ToString(),
                    MerchantPaymentReference = attempt.MerchantPaymentReference,
                    ProviderPaymentReference = latestEvidence?.ProviderPaymentReference ?? attempt.ProviderPaymentReference,
                    AttemptCreatedAt = attempt.CreatedAt,
                    EvidenceReceivedAt = latestEvidence?.ReceivedAt,
                    DecisionAt = latestDecision?.DecidedAt,
                    SafePayloadSnapshot = null
                });
            }
        }

        if (query.UnmatchedOnly || (query.PaymentConfirmationRecordId.HasValue && specificEvidence?.MatchedAttemptId == null))
        {
            var unmatchedRecords = await _unmatchedRepository.GetForAdminReviewAsync(
                null,
                query.FromUtc,
                query.ToUtc,
                cancellationToken);

            foreach (var unmatched in unmatchedRecords)
            {
                if (specificEvidence != null && unmatched.PrimaryEvidenceId != specificEvidence.Id)
                    continue;

                var evidence = specificEvidence?.Id == unmatched.PrimaryEvidenceId
                    ? specificEvidence
                    : await _evidenceRepository.GetByIdAsync(unmatched.PrimaryEvidenceId, cancellationToken);
                var decisions = await _decisionRepository.GetByUnmatchedRecordIdAsync(unmatched.Id, cancellationToken);
                var latestDecision = decisions.OrderByDescending(d => d.DecidedAt).FirstOrDefault();

                if (query.ConflictsOnly && latestDecision?.ConflictWithAcceptedFinalOutcome != true)
                    continue;

                results.Add(new SiteAdministratorPaymentReviewDto
                {
                    UnmatchedPaymentReturnRecordId = unmatched.Id,
                    IsUnmatchedReturn = true,
                    PaymentConfirmationRecordId = evidence != null && IsPaymentConfirmationRecord(evidence) ? evidence.Id : null,
                    EvidenceSourceChannel = evidence?.SourceChannel,
                    DecisionType = latestDecision?.DecisionType ?? "UnmatchedReturn",
                    DecisionReasonCode = latestDecision?.DecisionReasonCode ?? unmatched.CorrelationDisposition,
                    DecisionSummary = latestDecision?.DecisionSummary ?? unmatched.DisplayMessage,
                    ConflictWithAcceptedFinalOutcome = latestDecision?.ConflictWithAcceptedFinalOutcome == true,
                    CorrelationDisposition = unmatched.CorrelationDisposition,
                    MerchantPaymentReference = evidence?.MerchantPaymentReference,
                    ProviderPaymentReference = evidence?.ProviderPaymentReference,
                    EvidenceReceivedAt = unmatched.ReceivedAt,
                    DecisionAt = latestDecision?.DecidedAt,
                    SafePayloadSnapshot = unmatched.SafePayloadSnapshot
                });
            }
        }

        return results
            .OrderByDescending(r => r.DecisionAt ?? r.EvidenceReceivedAt ?? r.AttemptCreatedAt)
            .ToList();
    }

    private async Task<WalletTopUpAttempt?> CorrelateBrowserAttemptAsync(HostedPaymentReturnEvidenceDto parsedEvidence, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(parsedEvidence.ReturnCorrelationToken))
            return await _attemptRepository.GetByCorrelationTokenAsync(parsedEvidence.ReturnCorrelationToken, cancellationToken);

        if (!string.IsNullOrWhiteSpace(parsedEvidence.MerchantPaymentReference))
            return (await _attemptRepository.GetByMerchantPaymentReferenceAsync(parsedEvidence.MerchantPaymentReference, cancellationToken)).FirstOrDefault();

        return null;
    }

    private static WalletTopUpAttempt? DetermineAuthoritativeMatch(
        HostedPaymentReturnEvidenceDto parsedEvidence,
        IReadOnlyList<WalletTopUpAttempt> candidates)
    {
        if (candidates.Count != 1)
            return null;

        var candidate = candidates[0];
        if (candidate.Status != WalletTopUpAttemptStatus.Pending && candidate.Status != WalletTopUpAttemptStatus.NotConfirmed)
            return null;

        if (parsedEvidence.OwnerUserId.HasValue && parsedEvidence.OwnerUserId.Value != candidate.UserId)
            return null;

        if (!string.Equals(parsedEvidence.ConfirmedCurrencyCode, candidate.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            return null;

        return candidate;
    }

    private static PaymentReturnCorrelationDisposition DetermineCorrelationDisposition(
        HostedPaymentReturnEvidenceDto parsedEvidence,
        IReadOnlyList<WalletTopUpAttempt> candidates,
        WalletTopUpAttempt? matchedAttempt)
    {
        if (string.IsNullOrWhiteSpace(parsedEvidence.MerchantPaymentReference))
            return PaymentReturnCorrelationDisposition.MissingData;

        if (candidates.Count == 0)
            return PaymentReturnCorrelationDisposition.NoMatch;

        if (candidates.Count > 1)
            return PaymentReturnCorrelationDisposition.MultipleMatches;

        if (matchedAttempt == null)
        {
            var candidate = candidates[0];
            if (parsedEvidence.OwnerUserId.HasValue && parsedEvidence.OwnerUserId.Value != candidate.UserId)
                return PaymentReturnCorrelationDisposition.ForeignOwner;

            return candidate.IsFinalForSettlement
                ? PaymentReturnCorrelationDisposition.DuplicateFinalized
                : PaymentReturnCorrelationDisposition.InvalidData;
        }

        return PaymentReturnCorrelationDisposition.ExactMatch;
    }

    private async Task<UnmatchedPaymentReturnRecord> PersistUnmatchedAsync(PaymentReturnEvidence evidence, CancellationToken cancellationToken)
    {
        var unmatched = new UnmatchedPaymentReturnRecord
        {
            PrimaryEvidenceId = evidence.Id,
            ProviderKey = evidence.ProviderKey,
            CorrelationDisposition = evidence.CorrelationDisposition.ToString(),
            GenericResultCode = "not_confirmed",
            DisplayMessage = GenericNotConfirmedMessage,
            SafePayloadSnapshot = evidence.SafePayloadSnapshot,
            ReceivedAt = evidence.ReceivedAt
        };
        await _unmatchedRepository.AddAsync(unmatched, cancellationToken);
        return unmatched;
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
            MerchantPaymentReference = dto.MerchantPaymentReference,
            ReturnCorrelationToken = dto.ReturnCorrelationToken,
            OwnerUserId = dto.OwnerUserId,
            MatchedAttemptId = matchedAttempt?.Id,
            ClaimedOutcome = dto.ClaimedOutcome,
            TrustLevel = dto.TrustLevel,
            PaymentMethodCode = dto.PaymentMethodCode,
            EnvironmentMode = dto.EnvironmentMode,
            SignatureVerified = dto.SignatureVerified,
            SourceVerified = dto.SourceVerified,
            ServerConfirmed = dto.ServerConfirmed,
            ConfirmedChargedAmount = dto.ConfirmedChargedAmount,
            ConfirmedCurrencyCode = dto.ConfirmedCurrencyCode,
            EvidenceOccurredAt = dto.EvidenceOccurredAt,
            ValidatedAt = validatedAt,
            IsAtOrAfterAbandonmentThreshold = matchedAttempt != null && matchedAttempt.NextReconciliationDueAt.HasValue && receivedAt >= matchedAttempt.NextReconciliationDueAt.Value,
            SafePayloadSnapshot = dto.SafePayloadSnapshot,
            ValidationMessage = dto.ValidationMessage
        };

        if (dto.Id != Guid.Empty)
            typeof(PaymentReturnEvidence).GetProperty(nameof(PaymentReturnEvidence.Id))!.SetValue(evidence, dto.Id);
        typeof(PaymentReturnEvidence).GetProperty(nameof(PaymentReturnEvidence.ReceivedAt))!.SetValue(evidence, receivedAt);

        if (dto.CorrelationDisposition != default || matchedAttempt != null)
            evidence.CorrelationDisposition = matchedAttempt != null ? PaymentReturnCorrelationDisposition.ExactMatch : dto.CorrelationDisposition;
        else
            evidence.CorrelationDisposition = PaymentReturnCorrelationDisposition.MissingData;

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
            TriggerSource = resolution.TriggerSource,
            AppliedPrecedence = resolution.CorrelationDisposition.ToString(),
            NormalizedOutcome = resolution.NormalizedOutcome?.ToString() ?? "Unknown",
            AuthoritativeOutcomeBefore = beforeStatus?.ToString() ?? attempt?.Status.ToString(),
            AuthoritativeOutcomeAfter = attempt?.Status.ToString(),
            DecisionReasonCode = resolution.ReasonCode ?? "processed",
            DecisionSummary = resolution.ResolutionSummary ?? GenericNotConfirmedMessage,
            SupersededAbandonment = resolution.SupersededAbandonment,
            SupersededNotConfirmed = resolution.SupersededNotConfirmed,
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
            HostedPageDeadline = attempt.HostedPageDeadline,
            NextReconciliationDueAt = attempt.NextReconciliationDueAt,
            CancelledAt = attempt.CancelledAt,
            ExpiredAt = attempt.ExpiredAt,
            AbandonedAt = attempt.AbandonedAt,
            AuthoritativeEvidenceId = attempt.AuthoritativeEvidenceId,
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
            WalletTopUpAttemptStatus.Abandoned => "Top-up not confirmed",
            WalletTopUpAttemptStatus.NotConfirmed => "Top-up not confirmed",
            _ => "Payment is still pending. Your wallet has not been credited yet."
        };

    private static bool IsPaymentConfirmationRecord(PaymentReturnEvidence evidence)
        => string.Equals(evidence.SourceChannel, "PayFastNotify", StringComparison.OrdinalIgnoreCase)
            && evidence.SignatureVerified
            && evidence.SourceVerified
            && evidence.ServerConfirmed;

    private static bool IsAmountInAllowedBounds(decimal amount)
    {
        try
        {
            WalletCalculator.ValidateAmount(amount);
            return true;
        }
        catch
        {
            return false;
        }
    }

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
