namespace Payslip4All.Application.Interfaces;

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}
