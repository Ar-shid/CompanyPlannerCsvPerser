using CompanyPlannerCsvPerser;
using CompanyPlannerCsvPerser.Configuration;
using CompanyPlannerCsvPerser.HtmlParser;
using CompanyPlannerCsvPerser.Mhtml;
using Microsoft.Extensions.Options;

namespace CompanyPlannerCsvPerser.Tests;

public class FensterHtmlParserTests
{
    private readonly FensterHtmlParser _parser;
    private readonly MhtmlLoader _mhtmlLoader;

    public FensterHtmlParserTests()
    {
        _mhtmlLoader = new MhtmlLoader();
        _parser = new FensterHtmlParser(Options.Create(new SelectorOptions()));
    }

    [Fact]
    public void ParseListPage_FindsAllSubscriptions()
    {
        var listDocument = _mhtmlLoader.Load(GetSamplePath("subscription-list.mhtml"));
        var items = _parser.ParseListPage(listDocument.Html);

        Assert.Equal(15, items.Count);
        Assert.Contains(items, item => item.SubscriptionId == "405683");
        Assert.Contains(items, item => item.Name.Contains("Anne-Sophie", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseNextPageUrl_FindsPageTwoLink()
    {
        var listDocument = _mhtmlLoader.Load(GetSamplePath("subscription-list.mhtml"));
        var nextPageUrl = _parser.ParseNextPageUrl(listDocument.Html);

        Assert.NotNull(nextPageUrl);
        Assert.Contains("page=2", nextPageUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subscription_list", nextPageUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseDetailPage_ExtractsTaskFields()
    {
        var detailDocument = _mhtmlLoader.Load(GetSamplePath("subscription-detail.mhtml"));
        var detail = _parser.ParseDetailPage(detailDocument.Html);

        Assert.Equal("8", detail.BaseInterval);
        Assert.Equal("2026-08-31", detail.BaseStartWeek);
        Assert.Contains(detail.Tasks, task => task.SemanticMeaning == "WINDOW_CLEANING_OUTSIDE");
        Assert.Contains(detail.Tasks, task => task.SemanticMeaning == "WINDOW_CLEANING_INSIDE");
    }

    [Fact]
    public void MapToExportRecord_MergesListAndDetailData()
    {
        var listDocument = _mhtmlLoader.Load(GetSamplePath("subscription-list.mhtml"));
        var detailDocument = _mhtmlLoader.Load(GetSamplePath("subscription-detail.mhtml"));

        var listItem = _parser.ParseListPage(listDocument.Html).First(item => item.SubscriptionId == "405683");
        var detail = _parser.ParseDetailPage(detailDocument.Html);
        var record = _parser.MapToExportRecord(listItem, detail);

        Assert.Equal("Anne-Sophie", record.Name);
        Assert.Equal("anmobr@outlook.dk", record.Email);
        Assert.Equal("299.00", record.OutsideJobPrice);
        Assert.Equal("488.00", record.InsideJobPrice);
        Assert.Equal("Vinduespudsning", record.TitleForJob);
        Assert.Equal("47", record.EstimatedTime);
    }

    private static string GetSamplePath(string fileName)
    {
        return PathResolver.Resolve(Path.Combine("samples", fileName));
    }
}
