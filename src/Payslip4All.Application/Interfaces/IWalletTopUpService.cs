using Payslip4All.Application.DTOs.Wallet;

namespace Payslip4All.Application.Interfaces;

public interface IWalletTopUpService
{
    Task<StartWalletTopUpResultDto> StartHostedTopUpAsync(StartWalletTopUpCommand command, CancellationToken cancellationToken = default);
    Task<FinalizedWalletTopUpResultDto> FinalizeHostedReturnAsync(FinalizeWalletTopUpReturnCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTopUpAttemptDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
}
