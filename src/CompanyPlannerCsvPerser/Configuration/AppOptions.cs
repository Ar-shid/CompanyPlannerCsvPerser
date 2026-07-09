namespace CompanyPlannerCsvPerser.Configuration;

public sealed class AppOptions
{
    public const string SectionName = "Export";

    public ExportMode Mode { get; set; } = ExportMode.LocalMhtml;
    public string OutputDirectory { get; set; } = "./output";
    public string ExportedFileName { get; set; } = "exported-import.csv";
    public string FailedRecordsFileName { get; set; } = "failed-records.csv";
    public string LogFileName { get; set; } = "export.log";
}

public enum ExportMode
{
    LocalMhtml,
    Live
}
