using System.Text;
using System.Text.RegularExpressions;

namespace CompanyPlannerCsvPerser.Mhtml;

public sealed class MhtmlLoader : IMhtmlLoader
{
    private static readonly Regex HtmlPartRegex = new(
        @"Content-Type:\s*text/html.*?Content-Location:\s*(?<url>[^\r\n]+).*?\r?\n\r?\n(?<body>.*?)(?=\r?\n------)",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MhtmlDocument Load(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"MHTML file not found: {fullPath}", fullPath);
        }

        var raw = File.ReadAllText(fullPath);
        var match = HtmlPartRegex.Match(raw);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not locate HTML part in MHTML file: {fullPath}");
        }

        var baseUrl = match.Groups["url"].Value.Trim();
        var encodedBody = match.Groups["body"].Value;
        var html = DecodeQuotedPrintable(encodedBody);

        return new MhtmlDocument(html, baseUrl);
    }

    private static string DecodeQuotedPrintable(string input)
    {
        var normalized = input.Replace("=\r\n", string.Empty).Replace("=\n", string.Empty);
        var bytes = new List<byte>(normalized.Length);

        for (var i = 0; i < normalized.Length; i++)
        {
            if (normalized[i] == '=' && i + 2 < normalized.Length)
            {
                var hex = normalized.Substring(i + 1, 2);
                if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value))
                {
                    bytes.Add(value);
                    i += 2;
                    continue;
                }
            }

            bytes.Add((byte)normalized[i]);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }
}
