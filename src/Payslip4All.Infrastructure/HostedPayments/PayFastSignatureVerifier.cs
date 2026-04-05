using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace Payslip4All.Infrastructure.HostedPayments;

public sealed class PayFastSignatureVerifier
{
    public string ComputeSignature(IReadOnlyDictionary<string, string> payload, string? passphrase)
        => ComputeSignature(payload.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)), passphrase);

    public string ComputeSignature(IEnumerable<KeyValuePair<string, string?>> payload, string? passphrase)
    {
        var filtered = NormalizeCheckoutEntries(payload)
            .Select(kvp => $"{kvp.Key}={Encode(kvp.Value)}")
            .ToList();

        var input = string.Join("&", filtered);
        if (!string.IsNullOrWhiteSpace(passphrase))
            input = string.IsNullOrEmpty(input)
                ? $"passphrase={Encode(passphrase.Trim())}"
                : $"{input}&passphrase={Encode(passphrase.Trim())}";

        var bytes = MD5.HashData(Encoding.ASCII.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower(CultureInfo.InvariantCulture);
    }

    public string BuildQueryString(IEnumerable<KeyValuePair<string, string?>> payload, string? passphrase)
    {
        var materialized = NormalizeCheckoutEntries(payload);

        var serialized = string.Join("&", materialized.Select(kvp => $"{kvp.Key}={Encode(kvp.Value)}"));
        var signature = ComputeSignature(
            materialized.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)),
            passphrase);
        return string.IsNullOrEmpty(serialized)
            ? $"signature={Encode(signature)}"
            : $"{serialized}&signature={Encode(signature)}";
    }

    public bool Verify(IReadOnlyDictionary<string, string> payload, string? passphrase)
    {
        if (!payload.TryGetValue("signature", out var signature) || string.IsNullOrWhiteSpace(signature))
            return false;

        var expected = ComputeSignature(payload, passphrase);
        return string.Equals(expected, signature.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public string ComputeNotificationSignature(IReadOnlyDictionary<string, string> payload, string? passphrase)
        => ComputeNotificationSignature(payload.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)), passphrase);

    public string ComputeNotificationSignature(IEnumerable<KeyValuePair<string, string?>> payload, string? passphrase)
    {
        var input = BuildNotificationParameterString(payload);
        if (!string.IsNullOrWhiteSpace(passphrase))
            input = string.IsNullOrEmpty(input)
                ? $"passphrase={Encode(passphrase.Trim())}"
                : $"{input}&passphrase={Encode(passphrase.Trim())}";

        var bytes = MD5.HashData(Encoding.ASCII.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower(CultureInfo.InvariantCulture);
    }

    public string BuildNotificationParameterString(IReadOnlyDictionary<string, string> payload)
        => BuildNotificationParameterString(payload.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)));

    public string BuildNotificationParameterString(IEnumerable<KeyValuePair<string, string?>> payload)
        => string.Join("&", NormalizeNotificationEntries(payload).Select(kvp => $"{kvp.Key}={Encode(kvp.Value)}"));

    public bool VerifyNotification(IReadOnlyDictionary<string, string> payload, string? passphrase)
    {
        if (!payload.TryGetValue("signature", out var signature) || string.IsNullOrWhiteSpace(signature))
            return false;

        var expected = ComputeNotificationSignature(payload, passphrase);
        return string.Equals(expected, signature.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Encode(string value)
    {
        var replacements = new Dictionary<string, string>
        {
            ["%"] = "%25",
            ["!"] = "%21",
            ["#"] = "%23",
            [" "] = "+",
            ["$"] = "%24",
            ["&"] = "%26",
            ["'"] = "%27",
            ["("] = "%28",
            [")"] = "%29",
            ["*"] = "%2A",
            ["+"] = "%2B",
            [","] = "%2C",
            ["/"] = "%2F",
            [":"] = "%3A",
            [";"] = "%3B",
            ["="] = "%3D",
            ["?"] = "%3F",
            ["@"] = "%40",
            ["["] = "%5B",
            ["]"] = "%5D"
        };

        return Regex.Replace(value, @"[%!# $&'()*+,/:;=?@\[\]]", match => replacements[match.Value]);
    }

    private static List<KeyValuePair<string, string>> NormalizeCheckoutEntries(IEnumerable<KeyValuePair<string, string?>> payload)
        => payload
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key)
                          && !string.Equals(kvp.Key, "signature", StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value?.Trim() ?? string.Empty))
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .ToList();

    private static List<KeyValuePair<string, string>> NormalizeNotificationEntries(IEnumerable<KeyValuePair<string, string?>> payload)
        => payload
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key)
                          && !string.Equals(kvp.Key, "signature", StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value?.Trim() ?? string.Empty))
            .ToList();
}
