using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.3.4 and question 37, on the screen: telling GroupLab which bulls you aimed at.
/// <para>
/// <b>A rule nobody can reach is a rule that does not exist.</b> <c>AimedBulls</c> builds the rule and the matching honours it, and until
/// there is a control it is code nobody can use. So this drives the control rather than the helper: it types what a person would type and
/// checks the marking now reads the way they said.
/// </para>
/// </summary>
public class AimedBullsControlTests
{
    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    /// <summary>A five by five sheet, loaded as a marking with a bull at each place.</summary>
    private static void Sheet(MainWindow window)
    {
        var bulls = new List<BullAim>();
        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                int index = (r * 5) + c;
                bulls.Add(new BullAim(index, (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    new PointD(100 + (c * 100), 100 + (r * 100)), Scoring: true, Declared: new PointD(400 + (c * 500), 300 + (r * 500))));
            }
        }

        window.Session.Open(@"C:\a-sheet.png");
        window.Session.LoadDetections(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), bulls, [], "test");
        Dispatcher.UIThread.RunJobs();
    }

    private static TextBox Box(MainWindow window) =>
        window.GetLogicalDescendants().OfType<TextBox>().First(t => (t.PlaceholderText ?? "").Contains("rows", StringComparison.Ordinal));

    private static ComboBox Choice(MainWindow window) =>
        window.GetLogicalDescendants().OfType<ComboBox>().First(c => c.ItemsSource is IEnumerable<string> items && items.Contains("Only the bulls I aimed at"));

    [AvaloniaTheory]
    [InlineData("rows 1-3", 15)]
    [InlineData("columns 2-5", 20)]
    [InlineData("1-10", 10)]
    [InlineData("", 25)]
    public void WhatIsTypedBecomesTheBullsAimedAt(string typed, int expected)
    {
        var window = NewWindow();
        window.Show();
        Sheet(window);

        Choice(window).SelectedIndex = 3;
        Box(window).Text = typed;
        window.ApplyShotsPerBull();
        Dispatcher.UIThread.RunJobs();

        var rule = window.Session.State.Rule;
        Assert.NotNull(rule);
        Assert.Equal(expected, AimedBulls.Of(rule, window.Session.State.Bulls).Count);
        Assert.Equal(expected, rule.PerBull.Values.Sum());
        window.Close();
    }

    /// <summary>
    /// Bulls 2 to 5 of every row is entry 120's 6 ARC sheet, the one where every shot landed nearer a bull it was not aimed at. Column 1 of
    /// every row is exactly the bull those shots would be given by nearest bull, and it is closed.
    /// </summary>
    [AvaloniaFact]
    public void ColumnsOfEveryRowClosesTheBullsTheShotsWouldHaveBeenGiven()
    {
        var window = NewWindow();
        window.Show();
        Sheet(window);

        Choice(window).SelectedIndex = 3;
        Box(window).Text = "columns 2-5";
        window.ApplyShotsPerBull();
        Dispatcher.UIThread.RunJobs();

        var rule = window.Session.State.Rule!;
        foreach (var row in AimedBulls.Rows(window.Session.State.Bulls))
        {
            Assert.Equal(0, rule.For(row[0]));
            Assert.Equal(1, rule.For(row[1]));
        }

        window.Close();
    }

    /// <summary>
    /// It says back what it was told, on the screen, because this is the input that decides what every figure afterwards is about. A person
    /// who mistypes a row has to be able to see that they did.
    /// </summary>
    [AvaloniaFact]
    public void TheScreenSaysBackWhatItWasTold()
    {
        var window = NewWindow();
        window.Show();
        Sheet(window);

        Choice(window).SelectedIndex = 3;
        Box(window).Text = "rows 1-3";
        window.ApplyShotsPerBull();
        Dispatcher.UIThread.RunJobs();

        var said = window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
        Assert.Contains(said, t => t == "15 shots at 15 bulls, one shot each, and 10 bulls nobody aimed at.");
        window.Close();
    }

    /// <summary>Nonsense is refused with what to type, rather than quietly leaving the old rule in place.</summary>
    [AvaloniaFact]
    public void SomethingThatNamesNoBullIsRefusedWithWhatToType()
    {
        var window = NewWindow();
        window.Show();
        Sheet(window);

        Choice(window).SelectedIndex = 3;
        Box(window).Text = "rows banana";
        window.ApplyShotsPerBull();
        Dispatcher.UIThread.RunJobs();

        Assert.Null(window.Session.State.Rule);
        Assert.Contains("rows 1-3", window.ProblemText, StringComparison.Ordinal);
        window.Close();
    }
}
