using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 472 section 5, docs/PROOF-CHECKLIST.md row 27: the file the phone's own Share this session hands to the share
/// sheet is one the computer opens, read by the same reader its window uses (<c>SessionFileTests</c> holds the window's half), with every
/// shot where it was. What is left for the phone sitting is Android's share sheet carrying it across.
/// </summary>
public class Entry472ShareTests
{
    [AvaloniaFact]
    public void TheSessionThePhoneSharesOpensWithEveryShotWhereItWas()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var phone = (TestPhone)Phone.Platform!;
        var result = MobileSweepTests.Result();
        if (result.State.ImagePath is { } picture && !File.Exists(picture))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(picture)!);
            File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), picture);
        }

        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        var window = new Window { Width = 412, Height = 915, Content = view };
        window.Show();
        MobileSweepTests.Settle(window);
        try
        {
            int before = phone.SharedPaths.Count;
            view.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "result-share-session")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            MobileSweepTests.Settle(window);
            Assert.Equal(before + 1, phone.SharedPaths.Count);
            string shared = phone.SharedPaths[^1];
            Assert.EndsWith(SessionPackage.Extension, shared, StringComparison.Ordinal);

            using var file = File.OpenRead(shared);
            var opened = SessionPackage.Read(file);
            var sent = result.State.Shots.Where(s => s.IsShot).Select(s => s.Image).ToList();
            Assert.Equal(sent, opened.State.Shots.Where(s => s.IsShot).Select(s => s.Image).ToList());
            Assert.Equal(result.State.Calibre?.DiameterInches, opened.State.Calibre?.DiameterInches);
            Assert.NotEmpty(opened.Image);
        }
        finally
        {
            window.Close();
        }
    }
}
