namespace CompanyPlannerCsvPerser.Configuration;

public sealed class LocalMhtmlOptions
{
    public const string SectionName = "LocalMhtml";

    public string ListPagePath { get; set; } = "./samples/subscription-list.mhtml";
    public string DetailPagePath { get; set; } = "./samples/subscription-detail.mhtml";

    /// <summary>
    /// Subscription ID that the bundled detail MHTML snapshot represents.
    /// Other list records will be exported using list-page data only.
    /// </summary>
    public string DetailPageSubscriptionId { get; set; } = "405683";
}
