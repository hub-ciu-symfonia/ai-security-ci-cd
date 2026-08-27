using System.Text.RegularExpressions;

namespace Defense;

public record ScanResult(bool Suspicious, IReadOnlyList<string> Reasons);

public static class BackdoorScanner
{
    private static readonly Regex OutboundCallPattern = new(
        @"\b(HttpClient|WebClient|PostAsync|GetAsync|SendAsync)\b",
        RegexOptions.IgnoreCase);

    private static readonly Regex EnvironmentAccessPattern = new(
        @"Environment\.GetEnvironmentVariable(s)?\b",
        RegexOptions.IgnoreCase);

    public static ScanResult Scan(string diffText)
    {
        var addedLines = diffText
            .Split('\n')
            .Where(line => line.StartsWith("+") && !line.StartsWith("+++"));

        var addedText = string.Join("\n", addedLines);
        var reasons = new List<string>();

        var hasOutboundCall = OutboundCallPattern.IsMatch(addedText);
        var hasEnvironmentAccess = EnvironmentAccessPattern.IsMatch(addedText);

        if (hasOutboundCall && hasEnvironmentAccess)
        {
            reasons.Add(
                "Diff dodaje wywolanie sieciowe (HttpClient/WebClient) w tym samym zestawie zmian, " +
                "ktory odwoluje sie do zmiennych srodowiskowych (Environment.GetEnvironmentVariable) " +
                "- mozliwa eksfiltracja secrets.");
        }

        return new ScanResult(reasons.Count > 0, reasons);
    }
}
