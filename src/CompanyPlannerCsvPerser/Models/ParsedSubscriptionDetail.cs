namespace CompanyPlannerCsvPerser.Models;

public sealed class ParsedSubscriptionDetail
{
    public string BaseInterval { get; set; } = string.Empty;
    public string BaseStartWeek { get; set; } = string.Empty;
    public string FixedTimeOfDay { get; set; } = string.Empty;
    public string AddressComment { get; set; } = string.Empty;
    public string BillingContactText { get; set; } = string.Empty;
    public IReadOnlyList<TaskFormRecord> Tasks { get; set; } = Array.Empty<TaskFormRecord>();
}
