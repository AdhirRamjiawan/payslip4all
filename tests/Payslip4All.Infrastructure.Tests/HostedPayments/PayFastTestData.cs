namespace Payslip4All.Infrastructure.Tests.HostedPayments;

public static class PayFastTestData
{
    public static Dictionary<string, string> CompletedNotifyPayload(Guid userId, string merchantPaymentReference = "attempt-001")
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["m_payment_id"] = merchantPaymentReference,
            ["pf_payment_id"] = "pf-123",
            ["payment_status"] = "COMPLETE",
            ["amount_gross"] = "100.00",
            ["payment_method"] = "cc",
            ["custom_str1"] = userId.ToString(),
            ["custom_str2"] = "token-123",
            ["custom_str3"] = "session-123",
            ["custom_str4"] = "ZAR"
        };

    public static Dictionary<string, string> CancelledNotifyPayload(Guid userId, string merchantPaymentReference = "attempt-001")
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["m_payment_id"] = merchantPaymentReference,
            ["pf_payment_id"] = "pf-124",
            ["payment_status"] = "CANCELLED",
            ["amount_gross"] = "100.00",
            ["payment_method"] = "cc",
            ["custom_str1"] = userId.ToString(),
            ["custom_str2"] = "token-124",
            ["custom_str3"] = "session-124",
            ["custom_str4"] = "ZAR"
        };

    public static Dictionary<string, string> BrowserReturnPayload(Guid userId, string merchantPaymentReference = "attempt-001")
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = "payfast",
            ["m_payment_id"] = merchantPaymentReference,
            ["pf_payment_id"] = "pf-browser-001",
            ["amount"] = "100.00",
            ["payment_status"] = "COMPLETE",
            ["custom_str1"] = userId.ToString(),
            ["custom_str2"] = "token-123",
            ["custom_str3"] = "session-123",
            ["custom_str4"] = "ZAR"
        };
}
