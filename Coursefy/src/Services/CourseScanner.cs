using System.Globalization;
using System.Text.RegularExpressions;
using Coursefy.Models;

namespace Coursefy.Services;

public static class CourseScanner
{
    private static readonly HashSet<string> VideoExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".m4v", ".mkv", ".webm", ".avi"
    };

    private static readonly Regex FolderOrder = new("^#?\\s*(\\d+)\\s*[-_.]?\\s*(.*)$", RegexOptions.Compiled);

    public static string RelVideoUrl(string courseId, string relPath)
    {
        var safe = relPath.Replace('\\', '/');
        return $"/api/courses/{courseId}/file?rel={Uri.EscapeDataString(safe)}";
    }

    public static CourseIndex ScanCourse(string root, string courseId)
    {
        var items = new List<CourseSection>();
        foreach (var dir in SafeEnumerateDirectories(root))
        {
            var folder = Path.GetFileName(dir);
            var (order, title) = ParseFolderName(folder);

            var videos = new List<CourseVideo>();
            foreach (var file in SafeEnumerateFiles(dir))
            {
                var ext = Path.GetExtension(file);
                if (!VideoExt.Contains(ext))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);
                var rel = Path.Combine(folder, fileName).Replace('\\', '/');
                videos.Add(new CourseVideo
                {
                    Title = Path.GetFileNameWithoutExtension(fileName).Trim(),
                    File = fileName,
                    Rel = rel,
                    Url = RelVideoUrl(courseId, rel)
                });
            }

            videos.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            if (videos.Count == 0)
            {
                continue;
            }

            items.Add(new CourseSection
            {
                Folder = folder,
                Order = order,
                Title = title,
                Videos = videos
            });
        }

        items.Sort((a, b) =>
        {
            var cmp = a.Order.CompareTo(b.Order);
            return cmp != 0 ? cmp : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });

        return new CourseIndex
        {
            Root = Path.GetFullPath(root),
            Count = items.Count,
            VideoCount = items.Sum(s => s.Videos.Count),
            FirstVideoRel = items.FirstOrDefault()?.Videos.FirstOrDefault()?.Rel,
            Items = items
        };
    }

    public static LegacyIndex ScanLegacyRoot(string root)
    {
        var items = new List<CourseSectionLegacy>();
        foreach (var dir in SafeEnumerateDirectories(root))
        {
            var folder = Path.GetFileName(dir);
            var (order, title) = ParseFolderName(folder);

            var videos = new List<CourseVideoLegacy>();
            foreach (var file in SafeEnumerateFiles(dir))
            {
                var ext = Path.GetExtension(file);
                if (!VideoExt.Contains(ext))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);
                videos.Add(new CourseVideoLegacy
                {
                    Title = Path.GetFileNameWithoutExtension(fileName).Trim(),
                    File = fileName,
                    Url = "/" + Uri.EscapeDataString(folder) + "/" + Uri.EscapeDataString(fileName)
                });
            }

            videos.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            if (videos.Count == 0)
            {
                continue;
            }

            items.Add(new CourseSectionLegacy
            {
                Folder = folder,
                Order = order,
                Title = title,
                Videos = videos
            });
        }

        items.Sort((a, b) =>
        {
            var cmp = a.Order.CompareTo(b.Order);
            return cmp != 0 ? cmp : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });

        return new LegacyIndex
        {
            Root = Path.GetFullPath(root),
            Count = items.Count,
            Items = items
        };
    }

    private static (int order, string title) ParseFolderName(string name)
    {
        var n = name.Trim();
        var m = FolderOrder.Match(n);
        if (!m.Success)
        {
            return (int.MaxValue, n);
        }

        var order = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var title = m.Groups[2].Value.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = n;
        }
        return (order, title);
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string root)
    {
        try
        {
            return Directory.EnumerateDirectories(root);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root)
    {
        try
        {
            return Directory.EnumerateFiles(root);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
