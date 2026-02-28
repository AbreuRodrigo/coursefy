using System.Text.Json.Serialization;

namespace Coursefy.Models;

public record CourseEntry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("addedAt")] long AddedAt,
    [property: JsonPropertyName("updatedAt")] long UpdatedAt
);

public sealed class CoursesFile
{
    [JsonPropertyName("courses")]
    public List<CourseEntry> Courses { get; set; } = new();
}

public sealed class CourseSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("sectionCount")]
    public int SectionCount { get; set; }

    [JsonPropertyName("videoCount")]
    public int VideoCount { get; set; }

    [JsonPropertyName("firstVideoUrl")]
    public string? FirstVideoUrl { get; set; }

    [JsonPropertyName("updatedAt")]
    public long UpdatedAt { get; set; }
}
