using CompanyPlannerCsvPerser.Models;

namespace CompanyPlannerCsvPerser.HtmlParser;

public interface IHtmlParser
{
    IReadOnlyList<SubscriptionListItem> ParseListPage(string html);
    string? ParseNextPageUrl(string html);
    string? ParseNextPageUrl(string html, string currentUrl);
    ParsedSubscriptionDetail ParseDetailPage(string html);
    ExportRecord MapToExportRecord(SubscriptionListItem listItem, ParsedSubscriptionDetail? detail);
}
