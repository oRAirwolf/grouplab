using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.1.3: no text cut off or wrapping mid-number, at either size.
/// <para>
/// <b>Why a measurement and not a look.</b> Reading a screenshot for a clipped word is guesswork: a figure right-aligned two pixels inside
/// the edge and a figure two pixels outside it look the same at any scale a person actually views a render at. Avalonia knows exactly where
/// it put every word, so this asks it.
/// </para>
/// <para>
/// It is also the check that will not rot. A screenshot is looked at once, on the night it is taken; this runs on every commit, at both of
/// the sizes entry 141 names, and it fails with the word and how far outside its panel the word landed.
/// </para>
/// </summary>
public class NothingIsCutOffTests
{
    private static MainWindow NewWindow(int width, int height)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = width, Height = height };
    }

    /// <summary>A marking with the figures a person actually reads: ten shots, a scale, an aim point and a calibre.</summary>
    private static void Mark(MainWindow window)
    {
        var session = window.Session;
        session.Open(@"C:\a-target.png");
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        for (int k = 0; k < 10; k++)
        {
            double angle = 2 * Math.PI * k / 10;
            session.AddShot(new PointD(27 * Math.Cos(angle), 27 * Math.Sin(angle)));
        }

        session.SetCalibre(Calibre.Of(0.308));
        session.SetShotDistance(3600);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Every word the window shows sits inside the window. A word whose right edge is past the window's own width has been cut off, whatever
    /// a screenshot looks like, and the panel it is in is the one that needs the room.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(1280, 720, false)]
    [InlineData(2560, 1440, false)]
    [InlineData(1280, 720, true)]
    [InlineData(2560, 1440, true)]
    public void NoTextRunsPastTheEdgeOfTheWindow(int width, int height, bool explained)
    {
        var window = NewWindow(width, height);
        window.Show();
        Mark(window);

        // NOTES-FROM-PLANNING.md entry 163 section 5.4: every explanation collapsed, as a person first sees it, and every one opened.
        window.SetEveryWhy(explained);
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(width, height));
        window.Arrange(new Rect(0, 0, width, height));
        Dispatcher.UIThread.RunJobs();

        var over = new List<string>();
        foreach (var text in window.GetVisualDescendants().OfType<TextBlock>())
        {
            if (!text.IsVisible || string.IsNullOrWhiteSpace(text.Text) || text.Bounds.Width <= 0)
            {
                continue;
            }

            var corner = text.TranslatePoint(new Point(text.Bounds.Width, 0), window);
            if (corner is { } at && at.X > width + 0.5)
            {
                over.Add($"\"{text.Text}\" ends {at.X - width:0.0} px past the right edge");
            }
        }

        Assert.True(over.Count == 0, $"at {width} by {height}{(explained ? ", explained" : "")}, text is cut off at the window's edge:\n  " + string.Join("\n  ", over.Take(12)));
        window.Close();
    }

    /// <summary>
    /// And the cut the window's own edge cannot see: a word inside a panel that clips its contents. It sits well within the window and is
    /// still cut in half, and a render shows a number that simply reads as a smaller number.
    /// <para>
    /// The library's sheet list is why this exists. Its sighter counts sit within a pixel or two of the divider, and "25 + 3" losing its
    /// last character reads as "25 + ", while "6" losing its only one reads as nothing at all. There is no way to tell that from a render,
    /// which is the whole argument for measuring rather than looking.
    /// </para>
    /// </summary>
    [AvaloniaTheory]
    [InlineData(1280, 720, false)]
    [InlineData(2560, 1440, false)]
    [InlineData(1280, 720, true)]
    [InlineData(2560, 1440, true)]
    public void NoTextIsCutOffByThePanelItIsIn(int width, int height, bool explained)
    {
        var window = NewWindow(width, height);
        window.Show();
        Mark(window);

        // NOTES-FROM-PLANNING.md entry 163 section 5.4: every explanation collapsed, as a person first sees it, and every one opened.
        window.SetEveryWhy(explained);
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(width, height));
        window.Arrange(new Rect(0, 0, width, height));
        Dispatcher.UIThread.RunJobs();

        var cut = new List<string>();
        int looked = CutInside(window, width, height, cut);

        // Every screen, not only the marking one. The library's sheet list is the tightest thing in the application and it is three clicks
        // away from anything the marking screen shows.
        foreach (var (name, show) in Screens(window))
        {
            show();
            Dispatcher.UIThread.RunJobs();
            window.Measure(new Size(width, height));
            window.Arrange(new Rect(0, 0, width, height));
            Dispatcher.UIThread.RunJobs();
            looked += CutInside(window, width, height, cut, name);
        }

        // A test that cannot fail is worse than no test. If nothing in the window sits inside a panel that clips, this walk proves nothing,
        // and the day somebody replaces those panels it would go on passing while saying nothing at all.
        Assert.True(looked > 0, "nothing in the window sits inside a panel that clips, so this measured nothing");

        Assert.True(cut.Count == 0, $"at {width} by {height}{(explained ? ", explained" : "")}, text is cut off inside a panel:\n  " + string.Join("\n  ", cut.Take(12)));
        window.Close();
    }

    /// <summary>The screens a person can reach, each with the way to it, so the walk cannot quietly miss one.</summary>
    private static IEnumerable<(string Name, Action Show)> Screens(MainWindow window) =>
    [
        ("Targets", () => window.ShowLibrary()),
        ("Session records", () => { window.ShowLibrary(false); window.ShowSessions(); }),
        ("Ballistics", () => { window.ShowSessions(false); window.ShowBallistics(); }),
        ("Equipment", () => { window.ShowBallistics(false); window.ShowEquipment(EquipmentKind.Rifle); }),
        ("Settings", () => { window.BackToEditor(); window.ShowSettings(); }),
    ];

    /// <summary>Everything the window is showing now that a panel has cut in half, named so the failure says where to look.</summary>
    /// <returns>How many words were held against a panel that clips, so a walk that measured nothing can say so.</returns>
    private static int CutInside(MainWindow window, int width, int height, List<string> cut, string? screen = null)
    {
        int looked = 0;
        foreach (var text in window.GetVisualDescendants().OfType<TextBlock>())
        {
            // IsEffectivelyVisible, not IsVisible: the editor's own header is still in the tree while another screen is showing, and its
            // buttons sit far off to the side. Something nobody can see has not been cut off.
            if (!text.IsEffectivelyVisible || string.IsNullOrWhiteSpace(text.Text) || text.Bounds.Width <= 0)
            {
                continue;
            }

            foreach (var ancestor in text.GetVisualAncestors())
            {
                // Only a panel that clips can cut anything. A scroller clips on purpose and scrolls to what it hides, and the window's own
                // edge is the other test's business.
                if (ancestor is not Visual v || v is Window || !v.ClipToBounds
                    || v is Avalonia.Controls.Presenters.ScrollContentPresenter || v.Bounds.Width <= 0)
                {
                    continue;
                }

                looked++;
                if (text.TranslatePoint(new Point(text.Bounds.Width, 0), v) is { } at && at.X > v.Bounds.Width + 0.5)
                {
                    cut.Add($"\"{text.Text}\" is cut {at.X - v.Bounds.Width:0.0} px by the {v.GetType().Name} it is in{(screen is null ? "" : ", on " + screen)}");
                    break;
                }
            }
        }

        return looked;
    }
}
