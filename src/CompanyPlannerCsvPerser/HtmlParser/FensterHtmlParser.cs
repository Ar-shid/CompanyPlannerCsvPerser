using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CompanyPlannerCsvPerser.Configuration;
using CompanyPlannerCsvPerser.Models;
using Microsoft.Extensions.Options;

namespace CompanyPlannerCsvPerser.HtmlParser;

public sealed class FensterHtmlParser : IHtmlParser
{
    private static readonly Regex PhoneRegex = new(@"\+?\d[\d\s\-()]{6,}\d", RegexOptions.Compiled);

    private readonly SelectorOptions _selectors;
    private readonly IBrowsingContext _context;

    public FensterHtmlParser(IOptions<SelectorOptions> selectors)
    {
        _selectors = selectors.Value;
        _context = BrowsingContext.New(global::AngleSharp.Configuration.Default);
    }

    public IReadOnlyList<SubscriptionListItem> ParseListPage(string html)
    {
        var document = _context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();
        var rows = document.QuerySelectorAll(_selectors.ListPage.TableRows);
        var items = new List<SubscriptionListItem>();

        foreach (var row in rows)
        {
            var link = row.QuerySelector(_selectors.ListPage.DetailLink);
            if (link is null)
            {
                continue;
            }

            var href = link.GetAttribute("href") ?? string.Empty;
            var subscriptionId = ExtractSubscriptionId(href);
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                continue;
            }

            var cells = row.QuerySelectorAll("td").ToList();
            var customerIndex = _selectors.ListPage.CustomerColumnIndex;
            var customerCell = customerIndex < cells.Count ? cells[customerIndex] : null;
            var contact = ParseContactBlock(customerCell?.InnerHtml ?? string.Empty, customerCell?.TextContent ?? string.Empty);

            items.Add(new SubscriptionListItem
            {
                SubscriptionId = subscriptionId,
                DisplayNumber = link.TextContent.Trim(),
                DetailUrl = NormalizeUrl(href),
                Name = contact.Name,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                SubscriptionStreet = contact.Street,
                SubscriptionCity = contact.City,
                RawCustomerColumnHtml = customerCell?.InnerHtml ?? string.Empty
            });
        }

        return items;
    }

    public string? ParseNextPageUrl(string html)
    {
        var document = _context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();
        var links = document.QuerySelectorAll(_selectors.ListPage.PaginationLinks);
        var nextText = _selectors.ListPage.NextPageLinkText;

        var candidates = links
            .Select(link => new
            {
                Href = link.GetAttribute("href") ?? string.Empty,
                Text = link.TextContent.Trim()
            })
            .Where(link =>
                !string.IsNullOrWhiteSpace(link.Href) &&
                !link.Href.TrimEnd().EndsWith('#') &&
                link.Href.Contains("subscription_list", StringComparison.OrdinalIgnoreCase) &&
                TryGetPageNumber(link.Href).HasValue)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var nextLink = candidates.FirstOrDefault(link =>
            link.Text.Contains(nextText, StringComparison.OrdinalIgnoreCase) ||
            link.Text.Equals("next", StringComparison.OrdinalIgnoreCase));

        if (nextLink is not null)
        {
            return NormalizeUrl(nextLink.Href);
        }

        var currentPage = GetCurrentPageNumber(document);
        var sequential = candidates
            .Select(link => new { link.Href, Page = TryGetPageNumber(link.Href)!.Value })
            .Where(link => link.Page == currentPage + 1)
            .Select(link => link.Href)
            .FirstOrDefault();

        return sequential is null ? null : NormalizeUrl(sequential);
    }

    private int GetCurrentPageNumber(IDocument document)
    {
        var disabledPages = document.QuerySelectorAll("nav ul.pagination li.page-item.disabled a.page-link");
        foreach (var link in disabledPages)
        {
            if (int.TryParse(link.TextContent.Trim(), out var page) && page > 0)
            {
                return page;
            }
        }

        return 1;
    }

    public ParsedSubscriptionDetail ParseDetailPage(string html)
    {
        var document = _context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();
        var detailSelectors = _selectors.DetailPage;

        var tasks = document.QuerySelectorAll(detailSelectors.TaskFormRow)
            .Select(row => ParseTaskRow(row, detailSelectors))
            .Where(task => !string.IsNullOrWhiteSpace(task.SemanticMeaning) || !string.IsNullOrWhiteSpace(task.Category))
            .ToList();

        return new ParsedSubscriptionDetail
        {
            BaseInterval = GetSelectedValue(document, detailSelectors.BaseInterval),
            BaseStartWeek = GetSelectedValue(document, detailSelectors.BaseStartWeek),
            FixedTimeOfDay = GetInputValue(document, detailSelectors.FixedTimeOfDay),
            AddressComment = GetTextAreaValue(document, detailSelectors.AddressComment),
            BillingContactText = GetTextAreaValue(document, detailSelectors.BillingTextarea),
            Tasks = tasks
        };
    }

    public ExportRecord MapToExportRecord(SubscriptionListItem listItem, ParsedSubscriptionDetail? detail)
    {
        var record = new ExportRecord
        {
            Name = listItem.Name,
            Email = listItem.Email,
            PhoneNumber = listItem.PhoneNumber,
            SubscriptionStreet = listItem.SubscriptionStreet,
            SubscriptionCity = listItem.SubscriptionCity
        };

        if (detail is null)
        {
            return record;
        }

        var billingContact = ParseContactBlock(detail.BillingContactText, detail.BillingContactText);
        if (!string.IsNullOrWhiteSpace(billingContact.Name))
        {
            record.Name = billingContact.Name;
        }

        if (!string.IsNullOrWhiteSpace(billingContact.Email))
        {
            record.Email = billingContact.Email;
        }

        if (!string.IsNullOrWhiteSpace(billingContact.PhoneNumber))
        {
            record.PhoneNumber = billingContact.PhoneNumber;
        }

        if (!string.IsNullOrWhiteSpace(billingContact.Street))
        {
            record.SubscriptionStreet = billingContact.Street;
        }

        if (!string.IsNullOrWhiteSpace(billingContact.City))
        {
            record.SubscriptionCity = billingContact.City;
        }

        record.StartDate = detail.BaseStartWeek;
        record.FirstOrderDate = detail.BaseStartWeek;
        record.StartTime = detail.FixedTimeOfDay;
        record.SubscriptionNote = detail.AddressComment;
        record.CustomerNote = detail.AddressComment;

        var totalDuration = 0;
        foreach (var task in detail.Tasks)
        {
            if (int.TryParse(task.Duration, out var duration))
            {
                totalDuration += duration;
            }

            if (!string.IsNullOrWhiteSpace(task.Category) && string.IsNullOrWhiteSpace(record.TitleForJob))
            {
                record.TitleForJob = task.Category;
            }

            ApplyTaskMapping(record, task, detail.BaseInterval);
        }

        if (totalDuration > 0)
        {
            record.EstimatedTime = totalDuration.ToString(CultureInfo.InvariantCulture);
        }

        return record;
    }

    private static void ApplyTaskMapping(ExportRecord record, TaskFormRecord task, string baseInterval)
    {
        var weeklyInterval = ResolveWeeklyInterval(baseInterval, task.IntervalMultiplier);
        var firstVisit = task.Staged ? "true" : "false";

        switch (task.SemanticMeaning.ToUpperInvariant())
        {
            case "WINDOW_CLEANING_OUTSIDE":
                record.OutsideJobWeeklyIntervals = weeklyInterval;
                record.OutsideJobPrice = task.Price;
                record.FirstVisitOutside = firstVisit;
                break;
            case "WINDOW_CLEANING_INSIDE":
                record.InsideJobWeeklyIntervals = weeklyInterval;
                record.InsideJobPrice = task.Price;
                record.FirstVisitInside = firstVisit;
                break;
            default:
                if (task.SemanticMeaning.Contains("EXTERIOR", StringComparison.OrdinalIgnoreCase) ||
                    task.Category.Contains("exterior", StringComparison.OrdinalIgnoreCase))
                {
                    record.ExterirorJobWeeklyIntervals = weeklyInterval;
                    record.ExterirorJobPrice = task.Price;
                    record.FirstVisitExterior = firstVisit;
                }

                break;
        }
    }

    private static string ResolveWeeklyInterval(string baseInterval, string intervalMultiplier)
    {
        if (int.TryParse(baseInterval, out var baseWeeks) && int.TryParse(intervalMultiplier, out var multiplier) && multiplier > 0)
        {
            return (baseWeeks * multiplier).ToString(CultureInfo.InvariantCulture);
        }

        return baseInterval;
    }

    private static TaskFormRecord ParseTaskRow(IElement row, DetailPageSelectors selectors)
    {
        return new TaskFormRecord
        {
            SemanticMeaning = GetInputValue(row, selectors.TaskSemanticMeaning),
            Category = GetInputValue(row, selectors.TaskCategory),
            Price = GetInputValue(row, selectors.TaskPrice),
            Duration = GetInputValue(row, selectors.TaskDuration),
            IntervalMultiplier = GetSelectedValue(row, selectors.TaskIntervalMultiplier),
            Staged = IsChecked(row, selectors.TaskStaged),
            CustomerPresenceRequired = IsChecked(row, selectors.TaskCustomerPresence)
        };
    }

    private static string GetInputValue(IParentNode root, string selector)
    {
        var element = root.QuerySelector(selector) as IHtmlInputElement;
        return element?.Value?.Trim() ?? string.Empty;
    }

    private static string GetTextAreaValue(IParentNode root, string selector)
    {
        var element = root.QuerySelector(selector) as IHtmlTextAreaElement;
        return element?.Value?.Trim() ?? string.Empty;
    }

    private static string GetSelectedValue(IParentNode root, string selector)
    {
        var select = root.QuerySelector(selector) as IHtmlSelectElement;
        if (select is null)
        {
            return string.Empty;
        }

        return select.SelectedOptions.FirstOrDefault()?.Value?.Trim() ?? string.Empty;
    }

    private static bool IsChecked(IParentNode root, string selector)
    {
        var element = root.QuerySelector(selector) as IHtmlInputElement;
        return element?.IsChecked ?? false;
    }

    private static string ExtractSubscriptionId(string href)
    {
        var match = Regex.Match(href, @"subscription_edit/(\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static int? TryGetPageNumber(string href)
    {
        var match = Regex.Match(href, @"[?&]page=(\d+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups[1].Value, out var page) ? page : null;
    }

    private static string NormalizeUrl(string href)
    {
        if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return href;
        }

        return $"https://www.fenster.dk{href.TrimStart('.')}";
    }

    private static (string Name, string Email, string PhoneNumber, string Street, string City) ParseContactBlock(string html, string fallbackText)
    {
        var lines = ExtractLines(html, fallbackText);
        var text = string.Join("\n", lines);

        var emailMatch = Regex.Match(text, @"[A-Za-z][A-Za-z0-9._%+\-]*@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}");
        var email = emailMatch.Success ? emailMatch.Value : string.Empty;

        var phoneMatch = Regex.Match(text, @"\+?\d[\d\s\-()]{6,}\d");
        var phone = phoneMatch.Success ? phoneMatch.Value.Replace(" ", string.Empty) : string.Empty;

        var name = lines.FirstOrDefault(line =>
            !string.IsNullOrWhiteSpace(line) &&
            !line.Contains('@', StringComparison.Ordinal) &&
            !PhoneRegex.IsMatch(line) &&
            !LooksLikeAddress(line)) ?? string.Empty;

        var addressLines = lines.Where(LooksLikeAddress).ToList();

        string street = string.Empty;
        string city = string.Empty;

        if (addressLines.Count >= 2)
        {
            street = addressLines[0];
            city = addressLines[1];
        }
        else if (addressLines.Count == 1)
        {
            var parts = addressLines[0].Split(',', 2, StringSplitOptions.TrimEntries);
            street = parts[0];
            city = parts.Length > 1 ? parts[1] : string.Empty;
        }

        return (name, email, phone, street, city);
    }

    private static List<string> ExtractLines(string html, string fallbackText)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return fallbackText
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        var withBreaks = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        var text = Regex.Replace(withBreaks, "<[^>]+>", string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static bool LooksLikeAddress(string line)
    {
        return line.Any(char.IsDigit) && !line.Contains('@') && !line.StartsWith('+');
    }
}
