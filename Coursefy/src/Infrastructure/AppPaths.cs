namespace Coursefy.Infrastructure;

public static class AppPaths
{
    public static string ResolveContentRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        if (Directory.Exists(Path.Combine(baseDir, "player")) && Directory.Exists(Path.Combine(baseDir, "assets")))
        {
            return baseDir;
        }

        var cwd = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(cwd, "player")) && Directory.Exists(Path.Combine(cwd, "assets")))
        {
            return cwd;
        }

        return baseDir;
    }

    public static string GetAppDataRoot()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = Path.Combine(local, "Coursefy");
        Directory.CreateDirectory(root);
        return root;
    }

    public static string GetCoursesFilePath()
    {
        return Path.Combine(GetAppDataRoot(), ".courses.json");
    }

    public static IEnumerable<string> GetLegacyCoursesFileCandidates()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(local, "CoursePlayer", ".courses.json");
        yield return Path.Combine(Directory.GetCurrentDirectory(), ".courses.json");
    }
}
