using Payslip4All.Application.Interfaces;

namespace Payslip4All.Application.Tests.Services;

public sealed class WalletTopUpTestClock : ITimeProvider
{
    public WalletTopUpTestClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan by)
    {
        UtcNow = UtcNow.Add(by);
    }
}
