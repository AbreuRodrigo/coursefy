using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Coursefy.Infrastructure;
using Coursefy.Models;

namespace Coursefy.Services;

public sealed class AppState
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _coursesFile;
    private readonly object _gate = new();

    public AppState()
    {
        _coursesFile = AppPaths.GetCoursesFilePath();
        MigrateLegacyCoursesFileIfNeeded();
    }

    public string UpsertCourse(string rawPath)
    {
        var root = Path.GetFullPath(rawPath.Trim().Trim('"'));
        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException("Selected folder does not exist");
        }

        var id = HashPath(root);
        var title = Path.GetFileName(root.TrimEnd('\\', '/'));
        if (string.IsNullOrWhiteSpace(title))
        {
            title = root;
        }

        lock (_gate)
        {
            var courses = LoadCoursesUnsafe();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var idx = courses.FindIndex(c => c.Id == id);

            if (idx >= 0)
            {
                courses[idx] = courses[idx] with
                {
                    Path = root,
                    Title = title,
                    UpdatedAt = now
                };
            }
            else
            {
                courses.Add(new CourseEntry(id, root, title, now, now));
            }

            courses.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            SaveCoursesUnsafe(courses);
        }

        return id;
    }

    public bool RemoveCourse(string id)
    {
        lock (_gate)
        {
            var courses = LoadCoursesUnsafe();
            var before = courses.Count;
            courses.RemoveAll(c => c.Id == id);
            if (courses.Count == before)
            {
                return false;
            }

            SaveCoursesUnsafe(courses);
            return true;
        }
    }

    public CourseEntry? FindCourse(string id)
    {
        lock (_gate)
        {
            return LoadCoursesUnsafe().FirstOrDefault(c => c.Id == id);
        }
    }

    public List<CourseSummary> ListCourseSummaries()
    {
        var summaries = new List<CourseSummary>();

        lock (_gate)
        {
            var courses = LoadCoursesUnsafe();
            foreach (var c in courses)
            {
                var exists = Directory.Exists(c.Path);
                var sectionCount = 0;
                var videoCount = 0;
                string? firstVideoUrl = null;

                if (exists)
                {
                    var idx = CourseScanner.ScanCourse(c.Path, c.Id);
                    sectionCount = idx.Count;
                    videoCount = idx.VideoCount;
                    if (!string.IsNullOrWhiteSpace(idx.FirstVideoRel))
                    {
                        firstVideoUrl = CourseScanner.RelVideoUrl(c.Id, idx.FirstVideoRel);
                    }
                }

                summaries.Add(new CourseSummary
                {
                    Id = c.Id,
                    Title = c.Title,
                    Path = c.Path,
                    Exists = exists,
                    SectionCount = sectionCount,
                    VideoCount = videoCount,
                    FirstVideoUrl = firstVideoUrl,
                    UpdatedAt = c.UpdatedAt
                });
            }
        }

        return summaries;
    }

    private List<CourseEntry> LoadCoursesUnsafe()
    {
        if (!File.Exists(_coursesFile))
        {
            return new List<CourseEntry>();
        }

        try
        {
            var json = File.ReadAllText(_coursesFile, Encoding.UTF8);
            var data = JsonSerializer.Deserialize<CoursesFile>(json);
            return data?.Courses ?? new List<CourseEntry>();
        }
        catch
        {
            return new List<CourseEntry>();
        }
    }

    private void SaveCoursesUnsafe(List<CourseEntry> courses)
    {
        var payload = new CoursesFile { Courses = courses };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(_coursesFile, json, Encoding.UTF8);
    }

    private void MigrateLegacyCoursesFileIfNeeded()
    {
        if (File.Exists(_coursesFile))
        {
            return;
        }

        foreach (var legacy in AppPaths.GetLegacyCoursesFileCandidates())
        {
            if (!File.Exists(legacy))
            {
                continue;
            }

            File.Copy(legacy, _coursesFile, overwrite: false);
            break;
        }
    }

    private static string HashPath(string path)
    {
        var normalized = Path.GetFullPath(path).ToLowerInvariant();
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
