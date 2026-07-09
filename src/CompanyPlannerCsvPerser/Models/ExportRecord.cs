namespace CompanyPlannerCsvPerser.Models;

public sealed class ExportRecord
{
    public string TitleForJob { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string SubscriptionStreet { get; set; } = string.Empty;
    public string SubscriptionCity { get; set; } = string.Empty;
    public string OutsideJobWeeklyIntervals { get; set; } = string.Empty;
    public string OutsideJobPrice { get; set; } = string.Empty;
    public string InsideJobWeeklyIntervals { get; set; } = string.Empty;
    public string InsideJobPrice { get; set; } = string.Empty;
    public string ExterirorJobWeeklyIntervals { get; set; } = string.Empty;
    public string ExterirorJobPrice { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string FirstOrderDate { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EstimatedTime { get; set; } = string.Empty;
    public string CustomerNote { get; set; } = string.Empty;
    public string SubscriptionNote { get; set; } = string.Empty;
    public string FirstVisitInside { get; set; } = string.Empty;
    public string FirstVisitOutside { get; set; } = string.Empty;
    public string FirstVisitExterior { get; set; } = string.Empty;
}
