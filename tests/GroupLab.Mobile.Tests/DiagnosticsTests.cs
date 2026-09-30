using System.IO.Compression;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using GroupLab.App;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 311 section 3 items 1 and 2: Settings, About, "Send diagnostics" hands the share sheet one zip with the newest
/// logs and the kept pictures, on every phone; the iPhone and iPad application offers the keep-pictures switch too, off until turned on.
/// </summary>
public class DiagnosticsTests
{
    private static TestPhone Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        return (TestPhone)Phone.Platform!;
    }

    [AvaloniaFact]
    public void SendDiagnosticsSharesOneZipWithTheLogAndTheKeptPictures()
    {
        var phone = Started();
        DiagnosticLog.Info("diagnostics.test", ("marker", "in-the-zip"));
        string picture = Path.Combine(phone.CacheFolder, "diagnostics-sitting.jpg");
        File.WriteAllBytes(picture, [0xFF, 0xD8, 0xFF, 0xE0, 0, 4, 1, 2, 0xFF, 0xDA, 0, 2, 9, 9, 0xFF, 0xD9]);
        try
        {
            phone.IsDevBuild = true;
            Phone.Settings.SaveKeepSitting(true);
            Assert.NotNull(SittingRecord.Keep(picture, "live words"));
            phone.Asked.Clear();

            string said = DiagnosticsPackage.Send(new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Local));
            Assert.StartsWith("Ready to send:", said, StringComparison.Ordinal);
            Assert.Contains(phone.Asked, a => a.What == "share" && a.Name == "GroupLab diagnostics 2026-09-30 1200.zip");

            string zip = Path.Combine(phone.CacheFolder, "shared", "GroupLab diagnostics 2026-09-30 1200.zip");
            using var archive = ZipFile.OpenRead(zip);
            var names = archive.Entries.Select(e => e.FullName).ToList();
            Assert.Contains("about.txt", names);
            var logs = archive.Entries.Where(e => e.FullName.StartsWith("logs/grouplab-", StringComparison.Ordinal))
                .OrderByDescending(e => e.FullName, StringComparer.Ordinal).ToList();
            Assert.InRange(logs.Count, 1, DiagnosticsPackage.Logs);
            Assert.Contains(names, n => n.StartsWith("sitting/picture-", StringComparison.Ordinal) && n.EndsWith("/picture.jpg", StringComparison.Ordinal));
            Assert.Contains(names, n => n.EndsWith("/live.txt", StringComparison.Ordinal));
            using var reader = new StreamReader(logs[0].Open());
            Assert.Contains("in-the-zip", reader.ReadToEnd(), StringComparison.Ordinal);
        }
        finally
        {
            SittingRecord.Clear();
            phone.IsDevBuild = false;
            Phone.Settings.SaveKeepSitting(true);
        }
    }

    [AvaloniaFact]
    public void TheIPadOffersTheSwitchOffUntilTurnedOnAndGroupLabDevOnUntilTurnedOff()
    {
        var phone = Started();
        try
        {
            // Nothing saved yet: the public iPhone and iPad application keeps nothing, GroupLab Dev keeps.
            var fresh = new AppSettingsStore(Path.Combine(phone.CacheFolder, "fresh-settings.json"));
            Assert.False(fresh.LoadKeepSitting(unset: false));
            Assert.True(fresh.LoadKeepSitting());

            phone.Keeps = (true, false);
            Phone.Settings.SaveKeepSitting(true);
            Assert.True(SittingRecord.On);
            Phone.Settings.SaveKeepSitting(false);
            Assert.False(SittingRecord.On);

            // The Play build never offers it, whatever the setting says.
            phone.Keeps = (false, false);
            Phone.Settings.SaveKeepSitting(true);
            Assert.False(SittingRecord.On);
        }
        finally
        {
            phone.Keeps = null;
            Phone.Settings.SaveKeepSitting(true);
        }
    }

    [AvaloniaFact]
    public void SettingsAboutHasSendDiagnosticsOnEveryBuild()
    {
        Started();
        var view = new SettingsView(Phone.Settings);
        var window = new Window { Content = view };
        window.Show();
        var buttons = view.GetLogicalDescendants().OfType<Button>().Select(b => (b.Content as TextBlock)?.Text ?? b.Content as string).ToList();
        Assert.Contains("Send diagnostics", buttons);
        window.Close();
    }
}
