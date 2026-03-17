namespace Payslip4All.Domain.Entities;

/// <summary>
/// Application-layer guard helpers for <see cref="Company"/> field validation.
/// Enforces max-length constraints defined in the data model before values
/// reach the persistence layer.
/// </summary>
public static class CompanyGuard
{
    private const int UifNumberMaxLength = 50;
    private const int SarsPayeNumberMaxLength = 30;

    /// <summary>Throws <see cref="ArgumentException"/> if <paramref name="value"/> exceeds 50 characters.</summary>
    public static void ValidateUifNumber(string? value)
    {
        if (value is not null && value.Length > UifNumberMaxLength)
            throw new ArgumentException(
                $"UIF number must not exceed {UifNumberMaxLength} characters (got {value.Length}).",
                nameof(value));
    }

    /// <summary>Throws <see cref="ArgumentException"/> if <paramref name="value"/> exceeds 30 characters.</summary>
    public static void ValidateSarsPayeNumber(string? value)
    {
        if (value is not null && value.Length > SarsPayeNumberMaxLength)
            throw new ArgumentException(
                $"SARS PAYE number must not exceed {SarsPayeNumberMaxLength} characters (got {value.Length}).",
                nameof(value));
    }
}
