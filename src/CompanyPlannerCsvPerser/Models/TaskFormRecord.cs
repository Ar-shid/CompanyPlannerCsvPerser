namespace CompanyPlannerCsvPerser.Models;

public sealed class TaskFormRecord
{
    public string SemanticMeaning { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string IntervalMultiplier { get; set; } = string.Empty;
    public bool Staged { get; set; }
    public bool CustomerPresenceRequired { get; set; }
}
