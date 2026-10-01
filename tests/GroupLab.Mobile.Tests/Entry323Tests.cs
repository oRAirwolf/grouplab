using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 323, Phone B: "Velocity and the vertical" as a card of its own above All figures, the same as the desktop's
/// block; its "Add readings" opening a chronograph entry that keeps the pairing only when it is pressed for; the velocity band's chip in the
/// plot's key, remembered; and nothing squeezed at the narrowest phone width.
/// </summary>
public class Entry323Tests
{
    private static readonly TestPhone ThePhone = new();

    private const double Narrowest = 320;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Press(Control within, string id)
    {
        var button = within.GetLogicalDescendants().OfType<Button>().First(b => AutomationIdOf(b) == id);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    private static string? AutomationIdOf(Control control) => Avalonia.Automation.AutomationProperties.GetAutomationId(control);

    [AvaloniaFact]
    public void TheCardSitsAboveAllFiguresAndItsReadingsBandAndChipWorkAtTheNarrowestWidth()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = Narrowest, Height = 900, FontSize = 16 };
        window.Show();
        string copy = Path.Combine(ThePhone.CacheFolder, "chosen-323.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var store = PhoneAnalysis.Store();
        var before = store.LoadBook();
        try
        {
            Run(window, copy, store);
        }
        finally
        {
            store.SaveBook(before);
        }
    }

    private static void Run(Window window, string copy, SessionStore store)
    {
        store.SaveBook(RecordBook.Empty.With(new Load("Phone load", null) { BallisticCoefficient = 0.243, DragModel = DragModel.G7, BulletWeightGrains = 175 }));
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
        window.Content = view;
        Settle();

        // State 3, above All figures.
        var figures = view.GetLogicalDescendants().OfType<FiguresView>().Single();
        Assert.Equal(VelocityBlockState.NoReadings, figures.Velocity!.State);
        var column = (StackPanel)figures.Content!;
        var card = column.Children.Single(c => AutomationIdOf(c) == "result-velocity");
        // The first section after it is All figures.
        Assert.Equal(column.Children.IndexOf(card) + 1, column.Children.ToList().FindIndex(c => c is Expander));
        Assert.DoesNotContain(figures.GetLogicalDescendants().OfType<Button>(), b => Equals(b.Content, "Velocity band"));

        // "Add readings" opens the chronograph entry; the pairing is kept only when pressed for, and the card comes back with a result.
        card.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Add readings")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
        var page = (Control)view.Content!;
        int shots = result.State.Shots.Count(s => s.IsShot && s.Exclusion is null);
        page.GetLogicalDescendants().OfType<TextBox>().Single(b => AutomationIdOf(b) == "chrono-readings").Text =
            string.Join(", ", Enumerable.Range(0, shots).Select(i => 2700 + ((i * 7) % 23) - 11));
        Press(page, "chrono-read");
        Press(page, "chrono-keep-paired");
        Settle();

        var block = figures.Velocity!;
        Assert.True(block.HasResult, block.Sentence);
        Assert.Equal(shots, block.Dots.Count);
        var paired = store.ShotVelocities(result.SessionId!.Value);
        Assert.Equal(shots, paired.Count);
        Assert.Equal(Enumerable.Range(1, shots), paired.Select(p => p.Ordinal).Order());

        // Nothing squeezed: every drawing inside the card and wider than a stub, no word broken, the band's chip there and remembered.
        column = (StackPanel)figures.Content!;
        card = column.Children.Single(c => AutomationIdOf(c) == "result-velocity");
        foreach (var drawing in card.GetVisualDescendants().Where(c => c is VelocityMeter or VelocityBars or VelocityChart).Cast<Control>())
        {
            Assert.True(drawing.Bounds.Width > 200 && drawing.Bounds.Width <= Narrowest, $"{drawing.GetType().Name} is {drawing.Bounds.Width} wide");
        }

        Assert.Single(card.GetVisualDescendants().OfType<VelocityChart>());
        foreach (var text in card.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text)))
        {
            string words = text.Text!;
            foreach (var line in text.TextLayout.TextLines.Skip(1))
            {
                int start = line.FirstTextSourceIndex;
                Assert.True(start > 0 && start <= words.Length && (char.IsWhiteSpace(words[start - 1]) || words[start - 1] is '-' or '/' or ','), $"\"{words}\" is broken inside a word");
            }
        }

        var plot = view.GetVisualDescendants().OfType<CompositePlot>().First();
        Assert.Equal(block.Band, plot.VelocityBand);
        var chip = figures.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Velocity band"));
        chip.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
        Assert.False(plot.Shown.VelocityBand);
        Assert.False(Phone.Settings.LoadPlotMarks().VelocityBand);
        figures.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Velocity band")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
        Assert.True(Phone.Settings.LoadPlotMarks().VelocityBand);
        window.Close();
    }

    /// <summary>The distance page keeps a distance above zero in the person's unit, and refuses anything else.</summary>
    [AvaloniaFact]
    public void TheDistancePageTakesYardsOrMetres()
    {
        double? kept = null;
        var yards = VelocityPages.Distance(UnitSettings.Imperial, d => kept = d, () => { });
        var box = yards.GetLogicalDescendants().OfType<TextBox>().Single();
        box.Text = "0";
        Press(yards, "velocity-distance-keep");
        Assert.Null(kept);
        box.Text = "100";
        Press(yards, "velocity-distance-keep");
        Assert.Equal(3600, kept!.Value, 6);

        var metres = VelocityPages.Distance(UnitSettings.Imperial with { Distance = DistanceUnit.Metre }, d => kept = d, () => { });
        metres.GetLogicalDescendants().OfType<TextBox>().Single().Text = "100";
        Press(metres, "velocity-distance-keep");
        Assert.Equal(100 / 0.0254, kept!.Value, 6);
    }
}
