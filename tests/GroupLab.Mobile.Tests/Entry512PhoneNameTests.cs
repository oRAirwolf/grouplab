using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Rendering;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 512 section 3 on the phone (Alan's queue of 2026-11-24, part 1): the Session name box on the phone's Targets
/// screen, as on the computer. A name typed there shows at once in the sheet's picture and prints in the load block's square, which on the
/// phone always carries words because the phone prints the load block blank; a sheet with no load block shows no box.
/// </summary>
public class Entry512PhoneNameTests
{
    private static Window Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        return window;
    }

    private static LibrarySheet Sheet(string file) =>
        new(file, "test", null, GltdJsonReader.ReadFile(Repo.PathTo("targets", file)).Definition!);

    private static TextBox? NameBox(Control root) =>
        root.GetLogicalDescendants().OfType<TextBox>().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "targets-session-name");

    private static IEnumerable<string> Words(Scene? page) =>
        page is null ? [] : page.Items.Where(i => i.Layer == SceneLayer.DataBlockContent).OfType<TextRun>().Select(t => t.Text);

    [AvaloniaFact]
    public void ANameTypedOnASheetShowsInItsPictureAndPrintsInTheSquare()
    {
        var window = Started();
        try
        {
            var page = new TargetsPage();
            window.Content = page;
            page.ShowSheet(Sheet("GL-CF9-T-C.gltd.json"));
            Dispatcher.UIThread.RunJobs();
            var box = NameBox(page);
            Assert.NotNull(box);
            var view = page.GetLogicalDescendants().OfType<SheetView>().Single();
            Assert.DoesNotContain("Rifle A 140 gr", Words(view.Scene));

            box!.Text = "  Rifle A 140 gr ";
            Dispatcher.UIThread.RunJobs();
            Assert.Contains("Rifle A 140 gr", Words(view.Scene));
            Assert.Contains("1 of 3", Words(view.Scene));

            // What prints is drawn from the same options as the picture.
            Assert.Equal(Words(view.Scene), Words(TargetsPage.Page(Sheet("GL-CF9-T-C.gltd.json").Definition, name: TargetsPage.SessionNameFor(box.Text))));
            Assert.Null(TargetsPage.SessionNameFor("   "));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ASheetWithNoLoadBlockShowsNoNameBox()
    {
        var window = Started();
        try
        {
            string file = Directory.GetFiles(Repo.PathTo("targets"), "*.gltd.json").Select(Path.GetFileName).First(f =>
                GltdJsonReader.ReadFile(Repo.PathTo("targets", f!)).Definition is { DataBlock: null })!;
            var page = new TargetsPage();
            window.Content = page;
            page.ShowSheet(Sheet(file));
            Dispatcher.UIThread.RunJobs();
            Assert.Null(NameBox(page));
        }
        finally
        {
            window.Close();
        }
    }
}
