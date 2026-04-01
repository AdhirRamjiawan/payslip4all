namespace Payslip4All.Domain.Entities;

public class PayslipPricingSetting
{
    public static readonly Guid DefaultId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public const decimal DefaultPricePerPayslip = 15.00m;

    public Guid Id { get; private set; }
    public decimal PricePerPayslip { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public PayslipPricingSetting()
    {
        Id = DefaultId;
        PricePerPayslip = DefaultPricePerPayslip;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EnsureValid()
    {
        ValidatePrice(PricePerPayslip, nameof(PricePerPayslip));
    }

    public void UpdatePrice(decimal pricePerPayslip, string? updatedByUserId)
    {
        ValidatePrice(pricePerPayslip, nameof(pricePerPayslip));

        PricePerPayslip = pricePerPayslip;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidatePrice(decimal pricePerPayslip, string paramName)
    {
        if (pricePerPayslip < 0m)
            throw new ArgumentException("Price per payslip cannot be negative.", paramName);

        if (decimal.Round(pricePerPayslip, 2) != pricePerPayslip)
            throw new ArgumentException("Price per payslip must use no more than two decimal places.", paramName);
    }
}
