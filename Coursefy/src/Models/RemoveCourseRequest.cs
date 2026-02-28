using System.Text.Json.Serialization;

namespace Coursefy.Models;

public sealed class RemoveCourseRequest
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
