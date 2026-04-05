namespace Payslip4All.Domain.Enums;

public enum PaymentReturnCorrelationDisposition
{
    ExactMatch = 0,
    NoMatch = 1,
    MultipleMatches = 2,
    MissingData = 3,
    InvalidData = 4,
    ConflictingData = 5,
    ForeignOwner = 6,
    DuplicateFinalized = 7
}
