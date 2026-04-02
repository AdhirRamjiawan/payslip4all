using Payslip4All.Application.Interfaces;

namespace Payslip4All.Infrastructure.Time;

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
