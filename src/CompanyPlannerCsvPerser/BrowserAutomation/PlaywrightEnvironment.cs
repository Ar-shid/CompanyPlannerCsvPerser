using System.Runtime.InteropServices;

namespace CompanyPlannerCsvPerser.BrowserAutomation;

public static class PlaywrightEnvironment
{
    public const string BundleFolderName = "playwright-bundle";

    public static void Prepare()
    {
        EnsurePlaywrightFolder();
        Configure();
    }

    public static void Configure()
    {
        var appDirectory = AppContext.BaseDirectory;
        var playwrightAssembly = Path.Combine(appDirectory, "Microsoft.Playwright.dll");

        // Single-file bundles need an explicit driver search path.
        if (!File.Exists(playwrightAssembly))
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", appDirectory);
        }
    }

    public static void EnsurePlaywrightFolder()
    {
        var appDirectory = AppContext.BaseDirectory;
        var targetRoot = Path.Combine(appDirectory, ".playwright");
        var bundleRoot = Path.Combine(appDirectory, BundleFolderName);
        var nodeExecutable = GetNodeExecutablePath();

        if (File.Exists(nodeExecutable))
        {
            EnsureExecutable(nodeExecutable);
            return;
        }

        if (!Directory.Exists(bundleRoot))
        {
            return;
        }

        CopyDirectory(bundleRoot, targetRoot);
        EnsureExecutable(nodeExecutable);
    }

    public static bool IsDriverPresent()
    {
        return File.Exists(GetNodeExecutablePath());
    }

    public static string GetNodeExecutablePath()
    {
        var platformFolder = GetPlatformFolder();
        var nodeName = OperatingSystem.IsWindows() ? "node.exe" : "node";
        return Path.Combine(AppContext.BaseDirectory, ".playwright", "node", platformFolder, nodeName);
    }

    public static PackageValidationResult ValidatePackage()
    {
        EnsurePlaywrightFolder();

        var errors = new List<string>();
        var appDirectory = AppContext.BaseDirectory;

        if (!File.Exists(Path.Combine(appDirectory, "appsettings.json")))
        {
            errors.Add("Missing appsettings.json");
        }

        if (!File.Exists(Path.Combine(appDirectory, "Microsoft.Playwright.dll")))
        {
            errors.Add("Missing Microsoft.Playwright.dll");
        }

        var bundleRoot = Path.Combine(appDirectory, BundleFolderName);
        if (!Directory.Exists(bundleRoot) && !Directory.Exists(Path.Combine(appDirectory, ".playwright")))
        {
            errors.Add($"Missing {BundleFolderName}/ or .playwright/");
        }

        if (!IsDriverPresent())
        {
            errors.Add($"Missing Playwright node driver at {GetNodeExecutablePath()}");
        }

        return new PackageValidationResult(errors.Count == 0, errors);
    }

    private static void EnsureExecutable(string filePath)
    {
        if (!OperatingSystem.IsWindows() && File.Exists(filePath))
        {
            try
            {
                File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch
            {
                // Best effort only.
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, recursive: true);
        }

        Directory.CreateDirectory(targetDir);

        foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourceDir, targetDir, StringComparison.Ordinal));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var targetFile = file.Replace(sourceDir, targetDir, StringComparison.Ordinal);
            var targetParent = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetParent))
            {
                Directory.CreateDirectory(targetParent);
            }

            File.Copy(file, targetFile, overwrite: true);
            EnsureExecutable(targetFile);
        }
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
                _ => "darwin-x64"
            };
        }

        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "linux-arm64",
            _ => "linux-x64"
        };
    }
}

public sealed record PackageValidationResult(bool IsValid, IReadOnlyList<string> Errors);
