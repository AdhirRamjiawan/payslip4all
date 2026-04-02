namespace Payslip4All.Application.DTOs.Wallet;

public class FinalizeWalletTopUpReturnCommand
{
    public Guid UserId { get; set; }
    public Guid WalletTopUpAttemptId { get; set; }
    public IReadOnlyDictionary<string, string> ReturnPayload { get; set; } = new Dictionary<string, string>();
}
