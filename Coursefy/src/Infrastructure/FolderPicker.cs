using System.Windows.Forms;

namespace Coursefy.Infrastructure;

public static class FolderPicker
{
    public static string? PickFolder()
    {
        string? selected = null;
        Exception? thrown = null;

        var t = new Thread(() =>
        {
            try
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "Select course folder",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = false
                };
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    selected = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        });

        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();

        if (thrown is not null)
        {
            throw thrown;
        }

        return selected;
    }
}
