namespace Payslip4All.Domain.Entities;

public class PayslipPricingSetting
{
    public static readonly Guid DefaultId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public const decimal DefaultPricePerPayslip = 0.00m;

    public Guid Id { get; private set; }
    public decimal PricePerPayslip { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public PayslipPricingSetting()
    {
        Id = Guid.NewGuid();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EnsureValid()
    {
        if (PricePerPayslip < 0m)
            throw new ArgumentException("Price per payslip cannot be negative.", nameof(PricePerPayslip));
    }

    public void UpdatePrice(decimal pricePerPayslip, string? updatedByUserId)
    {
        if (pricePerPayslip < 0m)
            throw new ArgumentException("Price per payslip cannot be negative.", nameof(pricePerPayslip));

        PricePerPayslip = pricePerPayslip;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
