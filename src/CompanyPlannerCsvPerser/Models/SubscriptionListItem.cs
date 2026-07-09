namespace CompanyPlannerCsvPerser.Models;

public sealed class SubscriptionListItem
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string DisplayNumber { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string SubscriptionStreet { get; set; } = string.Empty;
    public string SubscriptionCity { get; set; } = string.Empty;
    public string RawCustomerColumnHtml { get; set; } = string.Empty;
}
