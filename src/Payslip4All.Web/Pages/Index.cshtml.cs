using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Payslip4All.Application.Interfaces;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private const string FallbackPublicPriceSummary =
        "Public payslip pricing is temporarily unavailable. Create an account or sign in to review the latest wallet pricing once it becomes available again.";

    private readonly IPayslipPricingService _payslipPricingService;

    public IndexModel(IPayslipPricingService payslipPricingService)
    {
        _payslipPricingService = payslipPricingService;
    }

    public decimal? CurrentPayslipPrice { get; private set; }

    public bool IsPublicPriceAvailable { get; private set; }

    public string PublicPriceSummary { get; private set; } = FallbackPublicPriceSummary;

    public async Task OnGetAsync()
    {
        try
        {
            var pricing = await _payslipPricingService.GetCurrentPriceAsync();
            CurrentPayslipPrice = pricing.PricePerPayslip;
            IsPublicPriceAvailable = true;
            PublicPriceSummary = $"Current public price: R {pricing.PricePerPayslip:N2} per payslip.";
        }
        catch
        {
            CurrentPayslipPrice = null;
            IsPublicPriceAvailable = false;
            PublicPriceSummary = FallbackPublicPriceSummary;
        }
    }
}
