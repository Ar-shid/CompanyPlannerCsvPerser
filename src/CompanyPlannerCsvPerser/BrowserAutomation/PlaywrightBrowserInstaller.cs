using Microsoft.Playwright;

namespace CompanyPlannerCsvPerser.BrowserAutomation;

public static class PlaywrightBrowserInstaller
{
    public static async Task<IPlaywright> CreatePlaywrightAsync()
    {
        PlaywrightEnvironment.Prepare();

        try
        {
            return await Playwright.CreateAsync();
        }
        catch (PlaywrightException ex) when (IsDriverMissingError(ex))
        {
            InstallPlaywrightAssets();
            PlaywrightEnvironment.Configure();
            return await Playwright.CreateAsync();
        }
    }

    public static async Task<IBrowser> LaunchChromiumAsync(
        IPlaywright playwright,
        BrowserTypeLaunchOptions options)
    {
        try
        {
            return await playwright.Chromium.LaunchAsync(options);
        }
        catch (PlaywrightException ex) when (IsMissingBrowserError(ex))
        {
            InstallPlaywrightAssets();
            return await playwright.Chromium.LaunchAsync(options);
        }
    }

    public static void InstallPlaywrightAssets()
    {
        Console.WriteLine();
        Console.WriteLine("Setting up Playwright (one-time, may download ~250 MB)...");
        Console.WriteLine();

        if (!PlaywrightEnvironment.IsDriverPresent())
        {
            var expectedPath = PlaywrightEnvironment.GetNodeExecutablePath();
            throw new InvalidOperationException(
                $"Playwright driver not found at '{expectedPath}'. " +
                "Ensure playwright-bundle/ exists next to the executable.");
        }

        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Playwright setup failed with exit code {exitCode}. " +
                "Run './CompanyPlannerCsvExporter --install-browsers' from the unzipped folder.");
        }

        Console.WriteLine();
        Console.WriteLine("Playwright setup completed successfully.");
        Console.WriteLine();
    }

    private static bool IsDriverMissingError(PlaywrightException ex)
    {
        return ex.Message.Contains("Driver not found", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMissingBrowserError(PlaywrightException ex)
    {
        return ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("playwright.ps1 install", StringComparison.OrdinalIgnoreCase);
    }
}
