namespace CompanyPlannerCsvPerser.Mhtml;

public interface IMhtmlLoader
{
    MhtmlDocument Load(string filePath);
}

public sealed record MhtmlDocument(string Html, string BaseUrl);
