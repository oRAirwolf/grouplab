using System.Security.Cryptography;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using GroupLab.App;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 472 section 1, the class of fault entry 470 found: a press decided by a button's words, so Android's back
/// on Sync and your data made a copy of the records. Here every place along the bottom, a result, every Settings page, Sync and the
/// printer check is opened, each button on it pressed in turn as the phone sweep presses them, and then Back: it goes to the screen the
/// press came from and changes nothing. No copy of the records, no file written or removed, nothing shared, printed or opened outside.
/// A new screen reached by any button is walked by itself; a page class the walk never reaches fails <see cref="EveryPageIsWalked"/>
/// until it is reached or named in <see cref="NotOnePressAway"/> with the reason.
/// </summary>
public class Entry472BackTests
{
    /// <summary>Pages the walk does not reach one press from a start, and why Back on them is held elsewhere.</summary>
    internal static readonly Dictionary<string, string> NotOnePressAway = new()
    {
        ["FirstRunView"] = "shown before the shell's frame; its own Back is walked by BackOnTheFirstRunGoesAStepBackAndSavesNothing",
        ["MarkingAPage"] = "opened by a hand-marked target's + Aim point, which needs a picture marked by hand (ParityTour on iOS)",
        ["WhichTargetPage"] = "opened only when a picture's sheet is not recognised, from Capture's reading",
        ["PairingView"] = "opened from a chronograph file's import, which needs a file picked; its way back is a BackButton (Entry470Tests)",
        ["ComparePage"] = "opened with saved sessions chosen; its way back is a BackButton (Entry470Tests)",
        ["CsvImportPage"] = "opened with a CSV file picked; its ways back are BackButtons (Entry470Tests)",
        ["FeedbackView"] = "the picture's check after the camera; held by BackOnThePictureCheckGoesToTheStartAndKeepsTheReading",
        ["SetPage"] = "opened from a set of pictures read as one (Entry423PhoneTests)",
    };

    private static TestPhone Started(out Shell shell, out Window window)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        shell = new Shell();
        window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        MobileSweepTests.Settle(window);
        return (TestPhone)Phone.Platform!;
    }

    /// <summary>Where the phone is: the place, the page's own names, the open Settings page and whether a sheet is over it.</summary>
    private static string Where(Shell shell, Window window)
    {
        var shown = window.GetVisualDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible).ToList();
        var titles = shown.OfType<TextBlock>().Where(t => t.Classes.Contains(PhoneStyles.Title)).Select(t => t.Text);
        var pages = shown.Where(c => c is UserControl && c.GetType().Assembly == typeof(Shell).Assembly).Select(c => c.GetType().Name);
        string open = shown.OfType<SettingsView>().FirstOrDefault()?.Open?.ToString() ?? "";
        bool sheet = shown.OfType<Button>().Any(b => b.Classes.Contains(PhoneStyles.SheetDismiss));
        return $"{shell.Showing}|{shell.PageContent?.GetType().Name}|{string.Join("/", pages.Distinct())}|{string.Join("/", titles)}|{open}|{(sheet ? "sheet" : "")}";
    }

    /// <summary>What Back must not change: every file the phone keeps, the copies of the records, and what was asked of the phone.</summary>
    private static string Lasting(TestPhone phone)
    {
        var files = Directory.GetFiles(phone.FilesFolder, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(f => Path.GetRelativePath(phone.FilesFolder, f) + "=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f)))[..12]);
        return $"copies {Phone.Copies?.List().Count ?? 0}; asked {phone.Asked.Count}; {string.Join("; ", files)}";
    }

    private static IEnumerable<(string Name, Action<Shell> Show)> Starts() =>
    [
        .. MobileSweepTests.Places(),
        .. SettingsMenu.All.Select(c => ($"Settings, {c}", (Action<Shell>)(s => s.ShowSettings(c)))),
        ("Sync", s => s.ShowSync()),
        ("the printer check", s => s.ShowPrinterCheck()),
    ];

    private static readonly HashSet<string> Reached = [];

    [AvaloniaFact]
    public void BackAfterEveryPressGoesToTheScreenBeforeAndChangesNothing()
    {
        var phone = Started(out var shell, out var window);
        // The result's picture is shared with other tests, one of which may have taken it away; without it Fix holes and Share a picture
        // are not offered and the walk would miss them.
        if (MobileSweepTests.Result().State.ImagePath is { } picture && !File.Exists(picture))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(picture)!);
            File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), picture);
        }

        var found = new List<string>();
        int backs = 0;
        try
        {
            // A place's own page, shown fresh: Back from any of them returns to Capture, wherever the press came from.
            var placeRoots = new HashSet<string>();
            foreach (var place in Enum.GetValues<Shell.Place>())
            {
                shell.Show(place);
                MobileSweepTests.Settle(window);
                placeRoots.Add(Where(shell, window));
            }

            shell.Show(Shell.Place.Capture);
            MobileSweepTests.Settle(window);
            string capture = Where(shell, window);

            foreach (var (start, show) in Starts())
            {
                show(shell);
                MobileSweepTests.Settle(window);
                string before = Where(shell, window);
                Note(window);
                var keys = MobileSweepTests.Buttons(window).Select(MobileSweepTests.Key).ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i];
                    int nth = keys.Take(i).Count(k => k == key);
                    show(shell);
                    MobileSweepTests.Settle(window);
                    var button = MobileSweepTests.Buttons(window).Where(b => MobileSweepTests.Key(b) == key).ElementAtOrDefault(nth);
                    string words = button is null ? "" : MobileSweepTests.NameOf(button) + (AutomationProperties.GetAutomationId(button) is { Length: > 0 } id ? $" ({id})" : "");
                    // A way back or a way out pressed is Back itself; what reaches outside or changes something for good is never pressed, as
                    // in the sweep.
                    if (button is null || MobileSweepTests.Lasting().IsMatch(words) || MobileSweepTests.Platforms().IsMatch(words) || MobileSweepTests.WayBack().IsMatch(words.Trim()) ||
                        button.Classes.Contains(PhoneStyles.NavItem) || button.Classes.Contains(PhoneStyles.PageBack) || button.Classes.Contains(PhoneStyles.SheetDismiss))
                    {
                        continue;
                    }

                    try
                    {
                        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        MobileSweepTests.Settle(window);
                    }
                    catch (Exception e)
                    {
                        found.Add($"{start}, '{words}': the press threw {e.GetType().Name}: {e.Message}");
                        continue;
                    }

                    phone.ClosePicker();
                    shell.Keyboard.CloseKeyboard();
                    MobileSweepTests.Settle(window);
                    string after = Where(shell, window);
                    Note(window);
                    if (after == before)
                    {
                        continue;
                    }

                    // A press that went to another place leaves Back to return to Capture, as it does from any place.
                    bool atPlace = placeRoots.Contains(after);
                    if (after == capture)
                    {
                        continue;
                    }

                    string expected = atPlace ? capture : before;
                    string kept = Lasting(phone);
                    bool answered;
                    try
                    {
                        answered = shell.Back();
                        MobileSweepTests.Settle(window);
                        backs++;
                    }
                    catch (Exception e)
                    {
                        found.Add($"{start}, after '{words}': Back threw {e.GetType().Name}: {e.Message}");
                        continue;
                    }

                    string back = Where(shell, window);
                    if (!answered)
                    {
                        found.Add($"{start}, after '{words}': Back was left to the phone, which closes GroupLab from an inner screen");
                    }
                    else if (back != expected)
                    {
                        found.Add($"{start}, after '{words}': Back went to [{back}], not [{expected}]");
                    }

                    if (Lasting(phone) != kept)
                    {
                        var now = Lasting(phone).Split("; ");
                        found.Add($"{start}, after '{words}': Back changed what the phone keeps: {string.Join(", ", kept.Split("; ").Except(now))} became {string.Join(", ", now.Except(kept.Split("; ")))}");
                    }
                }
            }
        }
        finally
        {
            window.Close();
        }

        Assert.True(backs > 30, $"Back was pressed only {backs} times");
        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found.Distinct().Take(80)));
    }

    private static void Note(Window window)
    {
        lock (Reached)
        {
            foreach (var c in window.GetLogicalDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible && c.GetType().Assembly == typeof(Shell).Assembly))
            {
                Reached.Add(c.GetType().Name);
            }
        }
    }

    /// <summary>
    /// A new page is walked by itself when a button reaches it; one that no button on a start reaches is named here with its reason, so
    /// nobody adds a screen Back was never pressed on.
    /// </summary>
    [AvaloniaFact]
    public void EveryPageIsWalked()
    {
        if (Reached.Count == 0)
        {
            BackAfterEveryPressGoesToTheScreenBeforeAndChangesNothing();
        }

        var pages = typeof(Shell).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(UserControl).IsAssignableFrom(t) && (t.Name.EndsWith("Page", StringComparison.Ordinal) || t.Name.EndsWith("View", StringComparison.Ordinal)))
            .Select(t => t.Name)
            .ToList();
        var missed = pages.Where(p => !Reached.Contains(p) && !NotOnePressAway.ContainsKey(p)).Order().ToList();
        var stale = NotOnePressAway.Keys.Where(p => !pages.Contains(p) || Reached.Contains(p)).Order().ToList();
        Assert.True(missed.Count == 0, "pages Back was never pressed on and not named with a reason: " + string.Join(", ", missed));
        Assert.True(stale.Count == 0, "named as not reached but now reached or gone, so take them off the list: " + string.Join(", ", stale));
    }

    [AvaloniaFact]
    public void BackOnTheFirstRunGoesAStepBackAndSavesNothing()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var store = new AppSettingsStore(Path.Combine(Phone.Platform!.FilesFolder, "first-run-back", "settings.json"));
        var view = new FirstRunView(store, () => { });
        var window = new Window { Width = 412, Height = 915, Content = view };
        window.Show();
        MobileSweepTests.Settle(window);
        try
        {
            var back = view.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "first-run-back");
            var skip = view.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "first-run-skip");
            Assert.False(back.IsEffectivelyEnabled);
            string first = string.Join("/", view.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text));
            skip.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            MobileSweepTests.Settle(window);
            Assert.True(back.IsEffectivelyEnabled);
            string saved = File.Exists(store.Path) ? File.ReadAllText(store.Path) : "";
            back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            MobileSweepTests.Settle(window);
            Assert.Equal(first, string.Join("/", view.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text)));
            Assert.Equal(saved, File.Exists(store.Path) ? File.ReadAllText(store.Path) : "");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BackOnThePictureCheckGoesToTheStartAndKeepsTheReading()
    {
        var phone = Started(out var shell, out var window);
        try
        {
            shell.Show(Shell.Place.Capture);
            MobileSweepTests.Settle(window);
            var capture = (CapturePage)shell.PageContent!;
            string start = Where(shell, window);
            bool retaken = false, used = false;
            var verdict = new GroupLab.Core.Capture.PictureVerdict(40, default, "Hard to measure", "The picture is dark.", [], [], CanMeasure: false);
            capture.Content = new FeedbackView(verdict, null, () => used = true, () => retaken = true);
            MobileSweepTests.Settle(window);
            string kept = Lasting(phone);
            Assert.True(shell.Back(), "back on the picture's check closed GroupLab");
            MobileSweepTests.Settle(window);
            Assert.Equal(start, Where(shell, window));
            Assert.False(retaken, "back took the picture again, which deletes the reading");
            Assert.False(used);
            Assert.Equal(kept, Lasting(phone));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>A target open on Capture, switched to from Ballistics: back returns to Ballistics, and back on Capture's own result to its start.</summary>
    [AvaloniaFact]
    public void BackFromATargetSwitchedToReturnsToThePlaceItWasSwitchedFrom()
    {
        Started(out var shell, out var window);
        try
        {
            shell.Show(Shell.Place.Capture);
            MobileSweepTests.Settle(window);
            var capture = (CapturePage)shell.PageContent!;
            var setup = new ShotSetup(GroupLab.Core.Marking.Calibre.Of(0.308), 3600);
            var view = new ResultView(MobileSweepTests.Result(), setup, GroupLab.Core.Marking.UnitSettings.Imperial, capture.ShowStart);
            capture.ShowResult(view);
            MobileSweepTests.Settle(window);
            Assert.Equal("result-another-target", AutomationProperties.GetAutomationId(shell.WayBack()!));

            shell.Show(Shell.Place.Ballistics);
            shell.SwitchTo(view);
            MobileSweepTests.Settle(window);
            Assert.Equal(Shell.Place.Capture, shell.Showing);
            Assert.True(shell.Back());
            MobileSweepTests.Settle(window);
            Assert.Equal(Shell.Place.Ballistics, shell.Showing);

            shell.Show(Shell.Place.Capture);
            capture.ShowResult(view);
            MobileSweepTests.Settle(window);
            Assert.True(shell.Back());
            MobileSweepTests.Settle(window);
            Assert.Equal(Shell.Place.Capture, shell.Showing);
            Assert.False(view.IsEffectivelyVisible && TopLevel.GetTopLevel(view) is not null, "back on Capture's result left the result showing");
        }
        finally
        {
            window.Close();
        }
    }
}
