using System.Windows.Forms;

namespace Coursefy.Desktop;

public static class DesktopHost
{
    public static async Task RunAsync(WebApplication app, string[] args)
    {
        var baseUrl = app.Urls.FirstOrDefault() ?? "http://127.0.0.1:8787";
        var pageUrl = $"{baseUrl.TrimEnd('/')}/player/index.html";
        var openBrowserOnly = args.Any(a => string.Equals(a, "--browser", StringComparison.OrdinalIgnoreCase));

        var hostTask = app.RunAsync();
        await WaitForServerAsync(baseUrl);

        if (openBrowserOnly)
        {
            OpenExternalBrowser(pageUrl);
            await hostTask;
            return;
        }

        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var uiThread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using var form = new CoursefyForm(pageUrl);
            form.FormClosed += (_, _) => closed.TrySetResult();
            Application.Run(form);
        });
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.IsBackground = false;
        uiThread.Start();

        await closed.Task;
        await app.StopAsync();
        await hostTask;
    }

    private static void OpenExternalBrowser(string pageUrl)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = pageUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore launch failures
        }
    }

    private static async Task WaitForServerAsync(string baseUrl)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var healthUrl = $"{baseUrl.TrimEnd('/')}/api/courses";
        const int maxTries = 25;

        for (var i = 0; i < maxTries; i++)
        {
            try
            {
                using var res = await http.GetAsync(healthUrl);
                if ((int)res.StatusCode < 500)
                {
                    return;
                }
            }
            catch
            {
                // retry
            }

            await Task.Delay(150);
        }
    }
}
