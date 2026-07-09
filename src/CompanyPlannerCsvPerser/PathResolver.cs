namespace CompanyPlannerCsvPerser;

public static class PathResolver
{
    public static string Resolve(string path) => ResolveExisting(path);

    public static string ResolveExisting(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var searchRoots = BuildSearchRoots();

        foreach (var root in searchRoots)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, path));
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.GetFullPath(path);
    }

    public static string ResolveOutput(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var solutionRoot = FindSolutionRoot();
        return Path.GetFullPath(Path.Combine(solutionRoot, path));
    }

    private static string FindSolutionRoot()
    {
        foreach (var root in BuildSearchRoots())
        {
            if (Directory.GetFiles(root, "*.sln").Any())
            {
                return root;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static IEnumerable<string> BuildSearchRoots()
    {
        var searchRoots = new List<string>
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 6; i++)
        {
            current = Path.GetFullPath(Path.Combine(current, ".."));
            searchRoots.Add(current);
        }

        return searchRoots.Distinct();
    }
}
