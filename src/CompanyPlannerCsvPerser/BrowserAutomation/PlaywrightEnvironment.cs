using System.Runtime.InteropServices;

namespace CompanyPlannerCsvPerser.BrowserAutomation;

public static class PlaywrightEnvironment
{
    public static void Configure()
    {
        var appDirectory = AppContext.BaseDirectory;
        Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", appDirectory);
    }

    public static bool IsDriverPresent()
    {
        var nodeExecutable = GetNodeExecutablePath();
        return File.Exists(nodeExecutable);
    }

    public static string GetNodeExecutablePath()
    {
        var platformFolder = GetPlatformFolder();
        var nodeName = OperatingSystem.IsWindows() ? "node.exe" : "node";
        return Path.Combine(AppContext.BaseDirectory, ".playwright", "node", platformFolder, nodeName);
    }

    private static string GetPlatformFolder()
    {
        if (OperatingSystem.IsWindows())
        {
            return Environment.Is64BitProcess ? "win32_x64" : "win32";
        }

        if (OperatingSystem.IsMacOS())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => "darwin-arm64",
                _ => "darwin"
            };
        }

        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "linux-arm64",
            _ => "linux"
        };
    }
}
