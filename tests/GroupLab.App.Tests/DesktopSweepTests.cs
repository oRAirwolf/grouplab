using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker A item 3: every desktop window at 1060 wide and at the sizes a 4K screen gives at 150 and 200
/// percent scaling (2560 by 1440 and 1920 by 1080 units), keyboard only and by screen reader name. For every screen and window, in both
/// themes' layout at those sizes: every control a person can act on has a name a screen reader announces, is a tab stop, is reached by
/// Tab from the window, and is not past the window's right edge; no line of text is either. Buttons, check boxes, toggles and combo boxes
/// answer Enter or Space as Avalonia's own controls do, so a control that is a tab stop works from the keyboard.
/// <para>
/// <b>What this cannot check headlessly.</b> The headless platform lays out in device-independent units and renders at a scaling of one,
/// so "4K scaling" here is the logical size that scaling leaves, not the pixels: blurry icons or hairlines at 150 percent are not seen. It
/// cannot hear a screen reader, only ask Avalonia's automation tree what it would announce. It cannot see a focus ring, and the operating
/// system's own dialogs (open, save, print) are outside it. Things worked by pointer alone are listed in <see cref="PointerOnly"/> with what
/// the keyboard does instead.
/// </para>
/// </summary>
public class DesktopSweepTests
{
    /// <summary>
    /// What is worked by the pointer and has no tab stop of its own, each with the keyboard's way to the same thing. The marking canvas is
    /// one control that takes the keyboard itself (its keys are in the key line above it); a tapped number's units are switched under
    /// Settings, Units, as well; a word's explanation is in the glossary; the plots repeat what the tables beside them say.
    /// </summary>
    internal static readonly string[] PointerOnly =
    [
        "MarkingCanvas: the marking keys C, V, B, Space, Enter, N and the bull numbers",
        "UnitTap numbers: Settings, Units",
        "TermHelp words: the glossary in the user guide",
        "CompositePlot, TrajectoryGraph, ShotOrderChart and the other plots: the shot table and the figures beside them",
        "SheetThumbnail: the library's list of sheets, which is a tab stop",
    ];

    private static void Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    /// <summary>Controls a person acts on, as the keyboard and a screen reader meet them.</summary>
    private static bool Actionable(Control c) =>
        c is Button or ToggleButton or CheckBox or ComboBox or TextBox or Slider or NumericUpDown or ListBox or AutoCompleteBox or RadioButton;

    /// <summary>Whether a control is inside one the keyboard enters as a whole, such as a combo box's own parts or a list's items.</summary>
    private static bool Inner(Control c) => c.GetVisualAncestors().OfType<Control>().Any(a => a is ComboBox or ListBox or AutoCompleteBox or NumericUpDown or Slider or TextBox or ScrollBar);

    /// <summary>The control that takes focus for one the keyboard enters as a whole: an auto-complete box's or a number box's own text box.</summary>
    private static Control Focused(Control c) => c is AutoCompleteBox or NumericUpDown ? c.GetVisualDescendants().OfType<TextBox>().FirstOrDefault() ?? c : c;

    private static string Describe(Control c)
    {
        string near = "";
        for (Control? at = c; at?.Parent is Panel panel && near.Length == 0; at = panel)
        {
            int i = panel.Children.IndexOf(at);
            near = panel.Children.Take(Math.Max(0, i)).Reverse().SelectMany(k => k is TextBlock t ? [t] : k.GetVisualDescendants().OfType<TextBlock>().Reverse())
                .Select(t => t.Text ?? "").FirstOrDefault(t => t.Length > 0) ?? "";
        }

        return $"{c.GetType().Name} '{(c as ContentControl)?.Content as string ?? (c as TextBox)?.PlaceholderText ?? c.Name ?? ""}' after '{near}'";
    }

    /// <summary>Everything wrong with one window as it stands.</summary>
    internal static List<string> Problems(Window window, string where)
    {
        Settle(window);
        var found = new List<string>();
        var controls = window.GetVisualDescendants().OfType<Control>()
            .Where(c => Actionable(c) && c.IsEffectivelyVisible && c.IsEffectivelyEnabled && c.Bounds.Width > 0 && !Inner(c)).ToList();

        // A name a screen reader announces: the control's own, its content's words, or a label it names.
        foreach (var c in controls)
        {
            string? name = ControlAutomationPeer.CreatePeerForElement(c).GetName();
            if (string.IsNullOrWhiteSpace(name))
            {
                found.Add($"{where}: {Describe(c)} has no name a screen reader can announce");
            }
        }

        // A tab stop, and reached by pressing Tab in the window, as a person would, round until focus comes back to where it started.
        var reached = new HashSet<IInputElement>();
        window.Focus();
        IInputElement? first = null;
        for (int i = 0; i < 3000; i++)
        {
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();
            var at = window.FocusManager?.GetFocusedElement();
            if (at is null || (first is not null && ReferenceEquals(at, first) && reached.Count > 1))
            {
                break;
            }

            first ??= at;
            reached.Add(at);
        }

        foreach (var c in controls.Select(Focused))
        {
            if (!c.Focusable || !KeyboardNavigation.GetIsTabStop(c))
            {
                found.Add($"{where}: {Describe(c)} is not a tab stop");
            }
            else if (!reached.Contains(c))
            {
                found.Add($"{where}: {Describe(c)} is never reached by Tab");
            }
        }

        // Nothing past the window's right edge, unless it scrolls sideways into view.
        foreach (var c in window.GetVisualDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible && c.Bounds.Width > 0 && (Actionable(c) || c is TextBlock { Text.Length: > 0 })))
        {
            if (c.GetVisualAncestors().OfType<ScrollViewer>().Any(s => s.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled) || Inner(c))
            {
                continue;
            }

            if (c.TranslatePoint(new Point(c.Bounds.Width, 0), window) is { } corner && corner.X > window.Bounds.Width + 1)
            {
                found.Add($"{where}: {(c is TextBlock t ? $"'{t.Text![..Math.Min(40, t.Text!.Length)]}'" : Describe(c))} reaches {corner.X:0}, past {window.Bounds.Width:0}");
            }
        }

        return found;
    }

    /// <summary>
    /// A control reached by Tab works from the keyboard: Space and Enter press it, even on the marking screen, where Space and Enter are
    /// otherwise the review's keys and are taken before a button the mouse clicked last can see them.
    /// </summary>
    [AvaloniaFact]
    public void SpaceAndEnterWorkWhatTabReaches()
    {
        var (window, path, _) = Entry109Tests.Sheet(1060, 720);
        try
        {
            ToggleButton Tool(string name) => window.GetVisualDescendants().OfType<ToggleButton>().First(b => Avalonia.Automation.AutomationProperties.GetName(b) == name);
            Assert.True(Tool("Scale: length").Focus(NavigationMethod.Tab), "focus");
            Assert.Same(Tool("Scale: length"), window.FocusManager?.GetFocusedElement());
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MarkingTool.Length, window.ToolNow);
            Tool("Scale: rectangle").Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MarkingTool.Rectangle, window.ToolNow);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    public static TheoryData<int, int> Sizes => new() { { 1060, 720 }, { 1920, 1080 }, { 2560, 1440 } };

    [AvaloniaTheory]
    [MemberData(nameof(Sizes))]
    public void EveryScreenIsNamedReachedAndInsideTheWindow(int width, int height)
    {
        var (window, path, _) = Entry109Tests.Sheet(width, height);
        var found = new List<string>();
        try
        {
            var rifle = new Rifle("Test rifle", 0.1, GroupLab.Core.Statistics.AngularUnit.Mrad) { SightHeightInches = 1.75, ZeroDistanceYards = 100 };
            window.Book = RecordBook.Empty.With(rifle).With(new Load("Test load", null)
            {
                MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = DragModel.G7,
                BcReference = ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
            });
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(rifle, null, "Test load");
            window.CalibreAnswered();
            found.AddRange(Problems(window, "marking"));
            window.Analyse();
            foreach (var (screen, show) in new (string, Action)[]
            {
                ("analysis", () => window.ShowSessions(false)),
                ("session records", () => window.ShowSessions()),
                ("targets", () => window.ShowLibrary()),
                ("ballistics", () => window.ShowBallistics()),
                ("compare loads", () => window.ShowCompare()),
                ("equipment", () => window.ShowEquipment(EquipmentKind.Rifle)),
                ("settings", () => window.ShowSettings()),
            })
            {
                show();
                found.AddRange(Problems(window, screen));
            }

            window.ShowSessions(false);
            foreach (var (name, make) in new (string, Func<Window>)[]
            {
                ("Shots and clicks", window.ShotsWindow),
                ("Zero from this group", window.ZeroFromWindow),
                ("Share a picture", window.ShareWindow),
            })
            {
                var tool = make();
                tool.Width = Math.Min(tool.Width is double.NaN ? width : tool.Width, width);
                tool.Show(window);
                found.AddRange(Problems(tool, name));
                tool.Close();
            }

            var report = new ReportWindow(null, null, null, sendUrl: "");
            report.Show();
            found.AddRange(Problems(report, "error report"));
            report.Close();
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }

        Assert.True(found.Count == 0, $"at {width} by {height}:{Environment.NewLine}" + string.Join(Environment.NewLine, found.Distinct().Take(80)));
    }
}
