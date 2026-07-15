namespace CompanyPlannerCsvPerser.Configuration;

public sealed class BrowserOptions
{
    public const string SectionName = "Browser";

    public bool Headless { get; set; }
    public int SlowMo { get; set; }
    public string LoginUrl { get; set; } = "https://www.fenster.dk/";
    public string ListPageUrl { get; set; } = "https://www.fenster.dk/subscription_list";
    public int NavigationTimeoutMs { get; set; } = 60_000;
    public bool WaitForManualLogin { get; set; } = true;
    public string ManualLoginPrompt { get; set; } =
        "Log in to the website, navigate to the subscription list page, then press ENTER to continue.";
    public int MaxListPages { get; set; } = 500;
}
