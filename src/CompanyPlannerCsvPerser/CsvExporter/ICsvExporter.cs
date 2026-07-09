using CompanyPlannerCsvPerser.Models;

namespace CompanyPlannerCsvPerser.CsvExporter;

public interface ICsvExporter
{
    Task WriteExportAsync(string filePath, IReadOnlyList<ExportRecord> records, CancellationToken cancellationToken = default);
    Task WriteFailedRecordsAsync(string filePath, IReadOnlyList<FailedRecord> records, CancellationToken cancellationToken = default);
}
