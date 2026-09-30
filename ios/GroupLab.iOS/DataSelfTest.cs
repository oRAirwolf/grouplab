using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Foundation;
using GroupLab.Mobile;
using UIKit;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 307 on iOS: Your data, round trip. Settings' Export all my data writes the file and hands it to iOS's share
/// sheet; the same file, opened in GroupLab as Files' "Open in GroupLab" hands one over, brings Settings back with what importing it would
/// do, which for the data just exported is nothing new. Nothing is imported: the plan is the proof.
/// </summary>
internal static class DataSelfTest
{
    private static Button? ButtonOf(string words) =>
        Shell.Current!.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b =>
            b.IsEffectivelyVisible && ((b.Content as string) ?? b.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault()?.Text) is { } w
            && w.StartsWith(words, StringComparison.Ordinal));

    /// <summary>The import plan's own button: Import, or Nothing to import; not Import data, which is always there.</summary>
    private static Button? Plan() =>
        Shell.Current!.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b =>
            b.IsEffectivelyVisible && ((b.Content as string) ?? b.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault()?.Text) is "Import" or "Nothing to import");

    internal static async Task<SelfTestCheck> RoundTrip()
    {
        var check = new SelfTestCheck("data round trip");
        try
        {
            await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Settings));
            await Task.Delay(TimeSpan.FromSeconds(1));
            var before = await SelfTest.OnUi(IosPhone.Top);
            DateTime started = DateTime.UtcNow.AddSeconds(-1);
            bool pressed = await SelfTest.OnUi(() =>
            {
                if (ButtonOf("Export all my data") is not { } export)
                {
                    return false;
                }

                export.BringIntoView();
                export.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return true;
            });
            if (!pressed)
            {
                check.Detail = "Export all my data was not found in Settings";
                return check;
            }

            bool sheet = await SelfTest.WaitFor(() => IosPhone.Top() is UIActivityViewController, TimeSpan.FromSeconds(30));
            await SelfTest.Photographed("95-data-export");
            await SelfTest.OnUi(() =>
            {
                if (IosPhone.Top() is UIActivityViewController share)
                {
                    share.DismissViewController(false, null);
                }
            });
            await SelfTest.WaitFor(() => ReferenceEquals(IosPhone.Top(), before), TimeSpan.FromSeconds(10));

            var file = new DirectoryInfo(IosPhone.Caches).EnumerateFiles("grouplab-data-*" + GroupLab.Core.Records.DataExport.Extension)
                .Where(f => f.LastWriteTimeUtc >= started).OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            check.Numbers["shareSheet"] = sheet ? 1 : 0;
            check.Numbers["kb"] = file is null ? 0 : file.Length / 1024;
            if (file is null)
            {
                check.Detail = "no data file was written";
                return check;
            }

            // Handed back as Files' "Open in GroupLab" hands a file over: copied into Documents/Inbox, its address to the application delegate.
            string inbox = Directory.CreateDirectory(Path.Combine(IosPhone.Documents, "Inbox")).FullName;
            string handed = Path.Combine(inbox, "round-trip" + GroupLab.Core.Records.DataExport.Extension);
            File.Copy(file.FullName, handed, overwrite: true);
            bool taken = await SelfTest.OnUi(() =>
            {
                var application = UIApplication.SharedApplication;
                using var options = new NSDictionary();
                return application.Delegate is AppDelegate app && app.OpenUrl(application, NSUrl.FromFilename(handed), options);
            });
            bool planned = await SelfTest.WaitFor(() => Shell.Current!.Showing == Shell.Place.Settings
                && Plan() is not null, TimeSpan.FromSeconds(30));
            string? said = await SelfTest.OnUi(() =>
            {
                var button = Plan();
                button?.BringIntoView();
                return button is null ? null : (button.Content as string) ?? button.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault()?.Text;
            });
            await Task.Delay(TimeSpan.FromSeconds(1));
            await SelfTest.Photographed("96-data-import");
            check.Numbers["opened"] = taken ? 1 : 0;
            check.Passed = sheet && taken && planned;
            check.Detail = $"Export all my data wrote {file.Length / 1024} KB and {(sheet ? "opened" : "did not open")} the share sheet; the file opened in GroupLab "
                + (planned ? $"brought Settings back with \"{said}\"" : "did not bring Settings back with its import plan");
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = SelfTestChecks.Describe(e);
        }

        return check;
    }
}
