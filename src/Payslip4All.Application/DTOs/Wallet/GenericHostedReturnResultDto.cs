namespace Payslip4All.Application.DTOs.Wallet;

public class GenericHostedReturnResultDto
{
    public bool IsMatched { get; set; }
    public Guid? MatchedAttemptId { get; set; }
    public string GenericResultCode { get; set; } = "unmatched";
    public string DisplayMessage { get; set; } = string.Empty;
    public Guid? UnmatchedRecordId { get; set; }
}
