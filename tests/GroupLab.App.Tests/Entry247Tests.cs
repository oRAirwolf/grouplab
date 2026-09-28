using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 247: the Ballistics screen laid out as concept B. Three columns at every size the analysis screen is held at,
/// the right one under the middle on a narrow window; a missing field marked in its row; a range chosen from a row or the curve worked out
/// on the right, with the analyzed group and the hit following it; and the middle switching between the trajectory and the chance of a hit.
/// </summary>
public class Entry247Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static MainWindow Ready(double width, double height, bool fields = true)
    {
        var (window, _, _) = Entry109Tests.Sheet();
        window.Width = width;
        window.Height = height;
        window.Book = RecordBook.Empty.With(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa)).With(new Load("H4350 41.5", null));
        window.ShowBallistics();
        Settle();
        if (fields)
        {
            window.SetBallisticFields("Tikka T3x", "H4350 41.5", "1.75", "100", "2710", "0.326", model: 2, reference: 1, weight: "140");
            window.KeepBallistics();
            Settle();
        }

        return window;
    }

    public static TheoryData<int, int> Sizes() => new() { { 1280, 720 }, { 1400, 900 }, { 1920, 1080 }, { 2560, 1440 } };

    [AvaloniaTheory]
    [MemberData(nameof(Sizes))]
    public void ThreeColumnsSitSideBySideAtEverySizeTheAnalysisIsHeldAt(int width, int height)
    {
        var window = Ready(width, height);
        try
        {
            Assert.False(window.BallisticRightUnderMiddle);
            var (left, middle, right) = window.BallisticColumnWidths;
            Assert.InRange(left, MainWindow.BallisticLeftMost - 1, MainWindow.BallisticLeft + 1);
            Assert.InRange(right, MainWindow.BallisticRightMost - 1, MainWindow.BallisticRight + 1);
            Assert.True(middle >= MainWindow.BallisticMiddleLeast - 1, $"the middle is {middle:0} wide at {width}");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OnANarrowWindowTheRightColumnGoesUnderTheMiddle()
    {
        var window = Ready(960, 700);
        try
        {
            Assert.True(window.BallisticRightUnderMiddle);
            var (left, _, _) = window.BallisticColumnWidths;
            Assert.InRange(left, MainWindow.BallisticLeftMost - 1, MainWindow.BallisticLeft + 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AFieldTheSolverNeedsIsMarkedInItsRow()
    {
        var window = Ready(1400, 900, fields: false);
        try
        {
            window.SetBallisticFields("Tikka T3x", "H4350 41.5", "", "100", "2710", "0.326", model: 2, reference: 1, weight: "140");
            window.KeepBallistics();
            Settle();
            Assert.Equal(1, window.BallisticFieldsNeeded);
            Assert.StartsWith("The solver needs the rifle's sight height", window.DopeRows.Single(), StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ARangeChosenIsWorkedOutOnTheRightAndEverythingFollowsIt()
    {
        var window = Ready(1400, 900);
        try
        {
            window.ChooseRange(500);
            Settle();
            string said = string.Join(" | ", window.AtRangeText);
            Assert.Contains("Elevation", said, StringComparison.Ordinal);
            Assert.Contains("MOA up", said, StringComparison.Ordinal);
            Assert.Contains("Time of flight", said, StringComparison.Ordinal);
            Assert.Contains("needs the twist", said, StringComparison.Ordinal);
            Assert.Equal("500", window.HitDistanceShown);

            // The table's row for it is the one marked.
            Assert.Contains(window.DopeRows, r => r.StartsWith("500 | ", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheMiddleSwitchesBetweenTheTrajectoryAndTheChanceOfAHit()
    {
        var window = Ready(1400, 900);
        try
        {
            Assert.Equal(MainWindow.BallisticView.Trajectory, window.BallisticViewShown);
            window.ShowBallisticView(MainWindow.BallisticView.Hit);
            Settle();
            Assert.Equal(MainWindow.BallisticView.Hit, window.BallisticViewShown);
            Assert.Contains(window.GetLogicalDescendants().OfType<Button>(), b => Equals(b.Content, "Work out the chance") && b.IsVisible);
            window.ShowBallisticView(MainWindow.BallisticView.Trajectory);
            Settle();

            // Photographed for a person to compare with the concept, as AnalysisStateTests does.
            string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "out", "screens"));
            Directory.CreateDirectory(output);
            window.ChooseRange(500);
            Settle();
            window.CaptureRenderedFrame()!.Save(Path.Combine(output, "ballistics-b.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Item 6: dark, light and high contrast, both views. In each the one solid amber button is the primary action, and a chosen switch is
    /// the tint, so the eye goes to what to press. Photographed to out/screens for a person to look at; nothing here asserts pixels.
    /// </summary>
    [AvaloniaFact]
    public void InEveryThemeThePrimaryIsTheOnlySolidAmber()
    {
        var window = Ready(1400, 900);
        try
        {
            string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "out", "screens"));
            Directory.CreateDirectory(output);
            foreach (var (theme, name, palette) in new[] { (ThemeChoice.Dark, "dark", Tokens.Dark), (ThemeChoice.Light, "light", Tokens.Light), (ThemeChoice.HighContrast, "high-contrast", Tokens.HighContrast) })
            {
                window.SetTheme(theme);
                foreach (var view in new[] { MainWindow.BallisticView.Trajectory, MainWindow.BallisticView.Hit })
                {
                    window.ShowBallisticView(view);
                    Settle();
                    var (primary, chosen) = window.BallisticButtonFills;
                    Assert.Equal(palette.Amber, Assert.IsAssignableFrom<Avalonia.Media.ISolidColorBrush>(primary).Color);
                    Assert.NotEqual(palette.Amber, Assert.IsAssignableFrom<Avalonia.Media.ISolidColorBrush>(chosen).Color);
                    window.CaptureRenderedFrame()!.Save(Path.Combine(output, $"ballistics-b-{view.ToString().ToLowerInvariant()}-{name}.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
                }
            }

            window.ShowBallisticView(MainWindow.BallisticView.Trajectory);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The addition: in the hit view the rifle, the load and the air fold to their summaries and the target and what is unsure open, the
    /// answer leads with the first round's chance, and going back to the trajectory opens the shared sections as they were left.
    /// </summary>
    [AvaloniaFact]
    public void TheHitViewFoldsWhatItSharesAndLeadsWithTheFirstRound()
    {
        var (window, path) = Entry156Tests.Open();
        try
        {
            window.Width = 1400;
            window.Height = 900;
            window.ShowBallisticView(MainWindow.BallisticView.Trajectory);
            Settle();
            var before = window.BallisticSectionsOpen.ToList();
            Assert.Contains("The rifle", before);

            window.ShowBallisticView(MainWindow.BallisticView.Hit);
            window.WorkOutHit("600", 0, "30", "30");
            Settle();
            var open = window.BallisticSectionsOpen.ToList();
            Assert.DoesNotContain("The rifle", open);
            Assert.DoesNotContain("The load", open);
            Assert.DoesNotContain("The air", open);
            Assert.Contains("The target", open);
            Assert.Contains("What you are unsure of", open);
            Assert.DoesNotContain("The shot and the simulation", open);

            var shown = window.HitShown.ToList();
            Assert.Equal("First round", shown[0]);
            Assert.Matches(@"^(more than |under )?[\d.]+ %$", shown[1]);
            int costs = shown.IndexOf("What costs the most");
            Assert.True(costs > shown.FindIndex(t => t.StartsWith("All together", StringComparison.Ordinal)), "the costs come after the answer's card");

            string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "out", "screens"));
            Directory.CreateDirectory(output);
            window.CaptureRenderedFrame()!.Save(Path.Combine(output, "ballistics-b-hit.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());

            window.ShowBallisticView(MainWindow.BallisticView.Trajectory);
            Settle();
            Assert.Equal(before, window.BallisticSectionsOpen.ToList());
        }
        finally
        {
            Entry156Tests.Close(window, path);
        }
    }
}
