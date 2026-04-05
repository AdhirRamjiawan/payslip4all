namespace Payslip4All.Domain.Enums;

public enum WalletTopUpAttemptStatus
{
    Pending = 0,
    Completed = 1,
    /// <summary>Legacy value retained for pre-008 database compatibility. Do not assign to new attempts.</summary>
    [System.Obsolete("Legacy status. Use NotConfirmed, Cancelled, or Expired for new top-up attempts.")]
    Failed = 2,
    Cancelled = 3,
    Expired = 4,
    Abandoned = 5,
    NotConfirmed = 6,
    /// <summary>Legacy alias retained for backward compatibility with older code/tests.</summary>
    Unverified = NotConfirmed
}
