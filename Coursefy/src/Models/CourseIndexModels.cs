using System.Text.Json.Serialization;

namespace Coursefy.Models;

public sealed class CourseIndex
{
    [JsonPropertyName("root")]
    public string Root { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("videoCount")]
    public int VideoCount { get; set; }

    [JsonPropertyName("firstVideoRel")]
    public string? FirstVideoRel { get; set; }

    [JsonPropertyName("items")]
    public List<CourseSection> Items { get; set; } = new();
}

public sealed class LegacyIndex
{
    [JsonPropertyName("root")]
    public string Root { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<CourseSectionLegacy> Items { get; set; } = new();
}

public sealed class CourseSection
{
    [JsonPropertyName("folder")]
    public string Folder { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("videos")]
    public List<CourseVideo> Videos { get; set; } = new();
}

public sealed class CourseSectionLegacy
{
    [JsonPropertyName("folder")]
    public string Folder { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("videos")]
    public List<CourseVideoLegacy> Videos { get; set; } = new();
}

public sealed class CourseVideo
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("rel")]
    public string Rel { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public sealed class CourseVideoLegacy
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
