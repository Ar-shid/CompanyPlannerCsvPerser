namespace CompanyPlannerCsvPerser.Configuration;

public sealed class SelectorOptions
{
    public const string SectionName = "Selectors";

    public ListPageSelectors ListPage { get; set; } = new();
    public DetailPageSelectors DetailPage { get; set; } = new();
}

public sealed class ListPageSelectors
{
    public string TableRows { get; set; } = "#subscription_list_table tbody tr";
    public string DetailLink { get; set; } = "a[href*='subscription_edit']";
    public int CustomerColumnIndex { get; set; } = 1;
}

public sealed class DetailPageSelectors
{
    public string TaskFormRow { get; set; } = ".task_form_row";
    public string TaskSemanticMeaning { get; set; } = "input[name$='-semantic_meaning']";
    public string TaskPrice { get; set; } = "input[name$='-price']";
    public string TaskDuration { get; set; } = "input[name$='-duration']";
    public string TaskIntervalMultiplier { get; set; } = "select[name$='-interval_multiplier']";
    public string TaskStaged { get; set; } = "input[name$='-staged']";
    public string TaskCategory { get; set; } = "input[name$='-category']";
    public string TaskCustomerPresence { get; set; } = "input[name$='-customer_presence_required']";
    public string BaseInterval { get; set; } = "#id_base_interval";
    public string BaseStartWeek { get; set; } = "#id_base_start_week";
    public string FixedTimeOfDay { get; set; } = "#id_fixed_time_of_day";
    public string AddressComment { get; set; } = "#id_address_comment";
    public string BillingTextarea { get; set; } = "textarea[name='billing_textarea']";
}
