namespace CompanyPlannerCsvPerser.Models;

public sealed class FailedRecord
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string DisplayNumber { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
}
