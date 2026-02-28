using Coursefy.Api;
using Coursefy.Desktop;
using Coursefy.Infrastructure;
using Coursefy.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(urls))
{
    builder.WebHost.UseUrls("http://127.0.0.1:8787");
}

var app = builder.Build();
var state = new AppState();
var contentRoot = AppPaths.ResolveContentRoot();

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(contentRoot),
    RequestPath = "/player",
    DefaultFileNames = { "index.html" }
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "player")),
    RequestPath = "/player"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "assets")),
    RequestPath = "/assets"
});

CourseEndpoints.Map(app, state, contentRoot);
await DesktopHost.RunAsync(app, args);
