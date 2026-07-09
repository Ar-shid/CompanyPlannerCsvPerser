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
    private readonly BrowserAutomation.IBrowserAutomationService _browserAutomation;
    private readonly IHtmlParser _htmlParser;
    private readonly ICsvExporter _csvExporter;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IOptions<AppOptions> appOptions,
        IOptions<BrowserOptions> browserOptions,
        IOptions<LocalMhtmlOptions> localMhtmlOptions,
        BrowserAutomation.IBrowserAutomationService browserAutomation,
        IHtmlParser htmlParser,
        ICsvExporter csvExporter,
        ILogger<ExportService> logger)
    {
        _appOptions = appOptions.Value;
        _browserOptions = browserOptions.Value;
        _localMhtmlOptions = localMhtmlOptions.Value;
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
            string listHtml;
            if (_appOptions.Mode == ExportMode.LocalMhtml)
            {
                _logger.LogInformation("Loading list page from local MHTML: {Path}", _localMhtmlOptions.ListPagePath);
                listHtml = await _browserAutomation.LoadLocalMhtmlAsync(
                    PathResolver.Resolve(_localMhtmlOptions.ListPagePath),
                    cancellationToken);
            }
            else
            {
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

                listHtml = await _browserAutomation.GetPageContentAsync(cancellationToken);
            }

            var listItems = _htmlParser.ParseListPage(listHtml);
            _logger.LogInformation("Found {Count} subscription records on list page", listItems.Count);

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
