namespace Payslip4All.Application.Interfaces;

public interface IWalletTopUpAbandonmentService
{
    Task AbandonExpiredAttemptsAsync(CancellationToken cancellationToken = default);
}
