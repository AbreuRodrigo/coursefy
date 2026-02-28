using System.Text.Json;
using System.Text.RegularExpressions;
using Coursefy.Infrastructure;
using Coursefy.Models;
using Coursefy.Services;
using Microsoft.AspNetCore.StaticFiles;

namespace Coursefy.Api;

public static class CourseEndpoints
{
    private static readonly Regex CourseIdPattern = new("^[0-9a-f]{40}$", RegexOptions.Compiled);

    public static void Map(WebApplication app, AppState state, string contentRoot)
    {
        app.MapGet("/api/courses", () =>
        {
            return Results.Json(new { courses = state.ListCourseSummaries() });
        });

        app.MapMethods("/api/courses/pick", new[] { "GET", "POST" }, () =>
        {
            try
            {
                var selected = FolderPicker.PickFolder();
                if (string.IsNullOrWhiteSpace(selected))
                {
                    return Results.Json(new { ok = true, cancelled = true });
                }

                var courseId = state.UpsertCourse(selected);
                return Results.Json(new { ok = true, cancelled = false, courseId });
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, error = ex.Message }, statusCode: 500);
            }
        });

        app.MapPost("/api/courses/remove", async (HttpContext ctx) =>
        {
            try
            {
                var req = await JsonSerializer.DeserializeAsync<RemoveCourseRequest>(ctx.Request.Body);
                var courseId = (req?.Id ?? string.Empty).Trim();
                if (!CourseIdPattern.IsMatch(courseId))
                {
                    return Results.Json(new { ok = false, error = "Invalid course id" }, statusCode: 400);
                }

                var removed = state.RemoveCourse(courseId);
                return Results.Json(new { ok = true, removed });
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, error = ex.Message }, statusCode: 500);
            }
        });

        app.MapGet("/api/courses/remove", (string? id) =>
        {
            try
            {
                var courseId = (id ?? string.Empty).Trim();
                if (!CourseIdPattern.IsMatch(courseId))
                {
                    return Results.Json(new { ok = false, error = "Invalid course id" }, statusCode: 400);
                }

                var removed = state.RemoveCourse(courseId);
                return Results.Json(new { ok = true, removed });
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, error = ex.Message }, statusCode: 500);
            }
        });

        app.MapGet("/api/courses/{id}/index.json", (string id) =>
        {
            var course = state.FindCourse(id);
            if (course is null)
            {
                return Results.NotFound("Course not found");
            }

            if (!Directory.Exists(course.Path))
            {
                return Results.NotFound("Course folder missing");
            }

            var index = CourseScanner.ScanCourse(course.Path, id);
            return Results.Json(index);
        });

        app.MapGet("/api/courses/{id}/file", (string id, string? rel) =>
        {
            var course = state.FindCourse(id);
            if (course is null)
            {
                return Results.NotFound("Course not found");
            }

            if (!Directory.Exists(course.Path))
            {
                return Results.NotFound("Course folder missing");
            }

            var relPath = (rel ?? string.Empty).Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(relPath))
            {
                return Results.BadRequest("Missing rel query");
            }

            var fullPath = Path.GetFullPath(Path.Combine(course.Path, relPath));
            var courseRoot = Path.GetFullPath(course.Path);
            if (!PathSafety.IsInsideRoot(courseRoot, fullPath))
            {
                return Results.StatusCode(403);
            }

            if (!File.Exists(fullPath))
            {
                return Results.NotFound("File not found");
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fullPath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return Results.File(fullPath, contentType, enableRangeProcessing: true);
        });

        app.MapGet("/index.json", () =>
        {
            var index = CourseScanner.ScanLegacyRoot(contentRoot);
            return Results.Json(index);
        });
    }
}
