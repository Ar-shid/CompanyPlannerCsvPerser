using System.Globalization;
using CompanyPlannerCsvPerser.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace CompanyPlannerCsvPerser.CsvExporter;

public sealed class CsvExporterService : ICsvExporter
{
    public async Task WriteExportAsync(string filePath, IReadOnlyList<ExportRecord> records, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);

        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        csv.WriteHeader<ExportRecord>();
        await csv.NextRecordAsync();
        await csv.WriteRecordsAsync(records, cancellationToken);
    }

    public async Task WriteFailedRecordsAsync(string filePath, IReadOnlyList<FailedRecord> records, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);

        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        csv.WriteHeader<FailedRecord>();
        await csv.NextRecordAsync();
        await csv.WriteRecordsAsync(records, cancellationToken);
    }
}
