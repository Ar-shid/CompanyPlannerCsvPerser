using CompanyPlannerCsvPerser.Configuration;
using CompanyPlannerCsvPerser.CsvExporter;
using CompanyPlannerCsvPerser.HtmlParser;
using CompanyPlannerCsvPerser.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompanyPlannerCsvPerser.Services;

public interface IExportService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}

public sealed class ExportService : IExportService
{
    private readonly AppOptions _appOptions;
    private readonly BrowserOptions _browserOptions;
    private readonly LocalMhtmlOptions _localMhtmlOptions;
    private readonly SelectorOptions _selectorOptions;
    private readonly BrowserAutomation.IBrowserAutomationService _browserAutomation;
    private readonly IHtmlParser _htmlParser;
    private readonly ICsvExporter _csvExporter;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IOptions<AppOptions> appOptions,
        IOptions<BrowserOptions> browserOptions,
        IOptions<LocalMhtmlOptions> localMhtmlOptions,
        IOptions<SelectorOptions> selectorOptions,
        BrowserAutomation.IBrowserAutomationService browserAutomation,
        IHtmlParser htmlParser,
        ICsvExporter csvExporter,
        ILogger<ExportService> logger)
    {
        _appOptions = appOptions.Value;
        _browserOptions = browserOptions.Value;
        _localMhtmlOptions = localMhtmlOptions.Value;
        _selectorOptions = selectorOptions.Value;
        _browserAutomation = browserAutomation;
        _htmlParser = htmlParser;
        _csvExporter = csvExporter;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var outputDirectory = PathResolver.ResolveOutput(_appOptions.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var exportedRecords = new List<ExportRecord>();
        var failedRecords = new List<FailedRecord>();

        await _browserAutomation.InitializeAsync(cancellationToken);

        try
        {
            var listItems = await CollectAllListItemsAsync(cancellationToken);
            _logger.LogInformation("Found {Count} subscription records across all list pages", listItems.Count);

            foreach (var listItem in listItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var detail = await LoadDetailAsync(listItem, cancellationToken);
                    var exportRecord = _htmlParser.MapToExportRecord(listItem, detail);
                    exportedRecords.Add(exportRecord);
                    _logger.LogInformation(
                        "Exported subscription {SubscriptionId} ({DisplayNumber})",
                        listItem.SubscriptionId,
                        listItem.DisplayNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process subscription {SubscriptionId} ({DisplayNumber})",
                        listItem.SubscriptionId,
                        listItem.DisplayNumber);

                    failedRecords.Add(new FailedRecord
                    {
                        SubscriptionId = listItem.SubscriptionId,
                        DisplayNumber = listItem.DisplayNumber,
                        DetailUrl = listItem.DetailUrl,
                        ErrorMessage = ex.Message
                    });
                }
            }
        }
        finally
        {
            await _browserAutomation.DisposeAsync();
        }

        var exportPath = Path.Combine(outputDirectory, _appOptions.ExportedFileName);
        var failedPath = Path.Combine(outputDirectory, _appOptions.FailedRecordsFileName);

        await _csvExporter.WriteExportAsync(exportPath, exportedRecords, cancellationToken);
        await _csvExporter.WriteFailedRecordsAsync(failedPath, failedRecords, cancellationToken);

        _logger.LogInformation("Wrote {ExportedCount} records to {ExportPath}", exportedRecords.Count, exportPath);
        _logger.LogInformation("Wrote {FailedCount} failed records to {FailedPath}", failedRecords.Count, failedPath);
    }

    private async Task<IReadOnlyList<SubscriptionListItem>> CollectAllListItemsAsync(CancellationToken cancellationToken)
    {
        if (_appOptions.Mode == ExportMode.LocalMhtml)
        {
            _logger.LogInformation("Loading list page from local MHTML: {Path}", _localMhtmlOptions.ListPagePath);
            var listHtml = await _browserAutomation.LoadLocalMhtmlAsync(
                PathResolver.Resolve(_localMhtmlOptions.ListPagePath),
                cancellationToken);

            var items = _htmlParser.ParseListPage(listHtml);
            var nextPageUrl = _htmlParser.ParseNextPageUrl(listHtml);
            if (!string.IsNullOrWhiteSpace(nextPageUrl))
            {
                _logger.LogWarning(
                    "Local MHTML contains a next page link ({NextPageUrl}), but LocalMhtml mode only has one snapshot. Live mode will walk all pages.",
                    nextPageUrl);
            }

            return Deduplicate(items);
        }

        _logger.LogInformation("Opening live website for manual login");
        await _browserAutomation.NavigateToAsync(_browserOptions.LoginUrl, cancellationToken);

        if (_browserOptions.WaitForManualLogin)
        {
            await _browserAutomation.WaitForManualLoginAsync(_browserOptions.ManualLoginPrompt, cancellationToken);
        }
        else
        {
            await _browserAutomation.NavigateToAsync(_browserOptions.ListPageUrl, cancellationToken);
        }

        var allItems = new List<SubscriptionListItem>();
        var visitedPageUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pageNumber = 1;
        var maxPages = Math.Max(1, _browserOptions.MaxListPages);

        while (pageNumber <= maxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _browserAutomation.WaitForSelectorAsync(_selectorOptions.ListPage.TableRows, cancellationToken);
            var listHtml = await _browserAutomation.GetPageContentAsync(cancellationToken);
            var pageItems = _htmlParser.ParseListPage(listHtml);
            allItems.AddRange(pageItems);

            _logger.LogInformation(
                "List page {PageNumber}: found {Count} records (running total: {Total})",
                pageNumber,
                pageItems.Count,
                Deduplicate(allItems).Count);

            var nextPageUrl = _htmlParser.ParseNextPageUrl(listHtml);
            if (string.IsNullOrWhiteSpace(nextPageUrl))
            {
                break;
            }

            if (!visitedPageUrls.Add(nextPageUrl))
            {
                _logger.LogWarning("Detected repeated pagination URL '{NextPageUrl}'. Stopping pagination.", nextPageUrl);
                break;
            }

            _logger.LogInformation("Navigating to list page {NextPage}: {Url}", pageNumber + 1, nextPageUrl);
            await _browserAutomation.NavigateToAsync(nextPageUrl, cancellationToken);
            pageNumber++;
        }

        if (pageNumber > maxPages)
        {
            _logger.LogWarning(
                "Reached MaxListPages limit ({MaxListPages}). Increase Browser:MaxListPages if more pages remain.",
                maxPages);
        }

        return Deduplicate(allItems);
    }

    private static IReadOnlyList<SubscriptionListItem> Deduplicate(IEnumerable<SubscriptionListItem> items)
    {
        return items
            .GroupBy(item => item.SubscriptionId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    private async Task<ParsedSubscriptionDetail?> LoadDetailAsync(SubscriptionListItem listItem, CancellationToken cancellationToken)
    {
        if (_appOptions.Mode == ExportMode.LocalMhtml)
        {
            if (!string.Equals(listItem.SubscriptionId, _localMhtmlOptions.DetailPageSubscriptionId, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "No local detail MHTML available for subscription {SubscriptionId}; using list-page data only",
                    listItem.SubscriptionId);
                return null;
            }

            var detailHtml = await _browserAutomation.LoadLocalMhtmlAsync(
                PathResolver.Resolve(_localMhtmlOptions.DetailPagePath),
                cancellationToken);
            return _htmlParser.ParseDetailPage(detailHtml);
        }

        await _browserAutomation.NavigateToAsync(listItem.DetailUrl, cancellationToken);
        var liveDetailHtml = await _browserAutomation.GetPageContentAsync(cancellationToken);
        return _htmlParser.ParseDetailPage(liveDetailHtml);
    }
}
