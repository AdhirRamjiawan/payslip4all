using Payslip4All.Application.DTOs.Wallet;

namespace Payslip4All.Application.Interfaces;

public interface IWalletTopUpService
{
    Task<StartWalletTopUpResultDto> StartHostedTopUpAsync(StartWalletTopUpCommand command, CancellationToken cancellationToken = default);
    Task<FinalizedWalletTopUpResultDto> FinalizeHostedReturnAsync(FinalizeWalletTopUpReturnCommand command, CancellationToken cancellationToken = default);
    Task<GenericHostedReturnResultDto> ProcessGenericReturnAsync(Guid userId, IReadOnlyDictionary<string, string> returnPayload, CancellationToken cancellationToken = default);
    Task<FinalizedWalletTopUpResultDto?> GetAttemptResultAsync(Guid attemptId, Guid userId, CancellationToken cancellationToken = default);
    Task AbandonExpiredAttemptsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTopUpAttemptDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
}
