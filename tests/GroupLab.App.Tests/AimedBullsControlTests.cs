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
/// there is a control it is code nobody can use. So this drives the control rather than the helper. Entry 407 section 2 replaced the typed
/// box with bulls tapped on the sheet ("Tap the bulls you fired at"), so it taps what a person would tap and checks the marking now reads the
/// way they said.
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

    /// <summary>Taps the bulls a person would, the way they say it: "rows 1-3", "columns 2-5", "1-10", or every bull.</summary>
    private static void Tap(MainWindow window, string said)
    {
        var bulls = window.Session.State.Bulls;
        var rule = AimedBulls.Parse(said, bulls)!;
        window.TapBulls(MainWindow.BullTaps.FiredAt);
        foreach (int bull in AimedBulls.Of(rule, bulls))
        {
            window.TapBullForGroups(bull);
        }

        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTheory]
    [InlineData("rows 1-3", 15)]
    [InlineData("columns 2-5", 20)]
    [InlineData("1-10", 10)]
    [InlineData("", 25)]
    public void WhatIsTappedBecomesTheBullsAimedAt(string said, int expected)
    {
        var window = NewWindow();
        window.Show();
        Sheet(window);

        Tap(window, said);

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

        Tap(window, "columns 2-5");

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

        Tap(window, "rows 1-3");

        Assert.Equal("You said which bulls you fired at: 10 bulls unused. Tap a bull to change it.", window.UnusedText);
        Assert.StartsWith("GroupLab will read it as: 15 bulls fired at, one shot each", window.ReadBackText, StringComparison.Ordinal);
        window.Close();
    }

    /// <summary>
    /// A bull tapped twice is back where it was, rather than quietly left in: what used to be "nonsense is refused" when the bulls were typed.
    /// The tapping ends when its button is pressed again, and a click on a bull then chooses it for its holes instead (entry 407 section 1).
    /// </summary>
    [AvaloniaFact]
    public void ABullTappedTwiceIsTakenOutAgain()
    {
        var window = NewWindow();
        window.Show();
        Sheet(window);

        Tap(window, "1-10");
        var bull = window.Session.State.Bulls[3].Index;
        window.TapBullForGroups(bull);
        Assert.Equal(9, AimedBulls.Of(window.Session.State.Rule, window.Session.State.Bulls).Count);
        window.TapBullForGroups(bull);
        Assert.Equal(10, AimedBulls.Of(window.Session.State.Rule, window.Session.State.Bulls).Count);
        window.TapBulls(MainWindow.BullTaps.None);
        Assert.Null(window.Canvas.BullTapTaker);
        window.Close();
    }
}
