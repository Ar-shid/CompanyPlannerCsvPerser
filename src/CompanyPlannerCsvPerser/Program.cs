using CompanyPlannerCsvPerser;
using CompanyPlannerCsvPerser.BrowserAutomation;
using CompanyPlannerCsvPerser.Configuration;
using CompanyPlannerCsvPerser.CsvExporter;
using CompanyPlannerCsvPerser.HtmlParser;
using CompanyPlannerCsvPerser.Mhtml;
using CompanyPlannerCsvPerser.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

if (args.Contains("--install-browsers", StringComparer.OrdinalIgnoreCase))
{
    PlaywrightBrowserInstaller.InstallChromium();
    return 0;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<BrowserOptions>(builder.Configuration.GetSection(BrowserOptions.SectionName));
builder.Services.Configure<LocalMhtmlOptions>(builder.Configuration.GetSection(LocalMhtmlOptions.SectionName));
builder.Services.Configure<SelectorOptions>(builder.Configuration.GetSection(SelectorOptions.SectionName));

builder.Services.AddSingleton<IMhtmlLoader, MhtmlLoader>();
builder.Services.AddSingleton<IHtmlParser, FensterHtmlParser>();
builder.Services.AddSingleton<ICsvExporter, CsvExporterService>();
builder.Services.AddSingleton<IBrowserAutomationService, PlaywrightBrowserAutomationService>();
builder.Services.AddSingleton<IExportService, ExportService>();

var appOptions = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions();
var outputDirectory = PathResolver.ResolveOutput(appOptions.OutputDirectory);
Directory.CreateDirectory(outputDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(outputDirectory, appOptions.LogFileName))
    .CreateLogger();

builder.Services.AddSerilog();

using var host = builder.Build();

try
{
    Log.Information("Starting Company Planner CSV export in {Mode} mode", appOptions.Mode);
    var exportService = host.Services.GetRequiredService<IExportService>();
    await exportService.RunAsync();
    Log.Information("Export completed successfully");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Export failed");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
