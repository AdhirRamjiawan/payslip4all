namespace Payslip4All.Domain.Entities;

public class Wallet
{
    private DateTimeOffset _persistedUpdatedAt;

    public Guid Id { get; private set; }
    public Guid UserId { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<WalletActivity> Activities { get; set; } = new();

    public Wallet()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        _persistedUpdatedAt = UpdatedAt;
    }

    public void EnsureValid()
    {
        if (UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(UserId));

        if (CurrentBalance < 0m)
            throw new ArgumentException("Current balance cannot be negative.", nameof(CurrentBalance));
    }

    public DateTimeOffset GetPersistedUpdatedAt() => _persistedUpdatedAt;

    public void CapturePersistedState() => _persistedUpdatedAt = UpdatedAt;
}
