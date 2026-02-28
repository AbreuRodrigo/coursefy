using Microsoft.Web.WebView2.WinForms;
using System.Windows.Forms;

namespace Coursefy.Desktop;

public sealed class CoursefyForm : Form
{
    private readonly string _url;
    private readonly WebView2 _web;

    public CoursefyForm(string url)
    {
        _url = url;
        Text = "Coursefy";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        _web = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_web);
        Load += OnLoadAsync;
    }

    private async void OnLoadAsync(object? sender, EventArgs e)
    {
        try
        {
            await _web.EnsureCoreWebView2Async();
            _web.Source = new Uri(_url);
        }
        catch
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // ignore
            }

            Close();
        }
    }
}
