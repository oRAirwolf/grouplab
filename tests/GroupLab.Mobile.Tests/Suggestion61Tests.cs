using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// Suggestion 61 (Discord, Alan's yes on 2026-11-24) on the phone: Compare loads shares the comparison as a picture of one page, or as a PDF
/// of the same page, or saves the picture to the gallery; the page is the computer's own.
/// </summary>
public class Suggestion61Tests
{
    private static readonly TestPhone ThePhone = new();

    [AvaloniaFact]
    public void CompareLoadsSharesThePictureAndThePdf()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var platform = (TestPhone)Phone.Platform!;
        string copy = Path.Combine(platform.CacheFolder, "chosen-61.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        var store = PhoneAnalysis.Store();
        var saved = store.Get(result.SessionId!.Value)!;
        long first = store.Save(saved with { Id = 0, CreatedUtc = "2026-11-24T04:40:00Z", Load = "N135 22.0" });
        long second = store.Save(saved with { Id = 0, CreatedUtc = "2026-11-24T05:01:00Z", Load = "N135 22.3" });

        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        var page = new ComparePage([store.Get(first)!, store.Get(second)!], UnitSettings.Imperial, () => { });
        window.Content = page;
        Dispatcher.UIThread.RunJobs();
        var ids = page.GetVisualDescendants().OfType<Control>().Select(AutomationProperties.GetAutomationId).OfType<string>().ToHashSet();
        Assert.Contains("compare-share-picture", ids);
        Assert.Contains("compare-share-pdf", ids);
        Assert.Contains("compare-save-picture", ids);

        int shared = platform.SharedPaths.Count;
        page.Export(picture: true, gallery: false);
        string png = platform.SharedPaths[shared];
        Assert.EndsWith(".png", png, StringComparison.Ordinal);
        Assert.StartsWith("Compare loads N135 22.0 vs N135 22.3 ", Path.GetFileName(png), StringComparison.Ordinal);
        Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], File.ReadAllBytes(png).Take(4));

        page.Export(picture: false, gallery: false);
        Assert.Contains(platform.Asked, a => a.What == "share pdf" && a.Name.StartsWith("Compare loads N135 22.0 vs N135 22.3 ", StringComparison.Ordinal));
        window.Close();
    }
}
