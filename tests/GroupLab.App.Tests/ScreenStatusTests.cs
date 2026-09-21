using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 125 section 2: Alan's screenshot of the settings page showed the status bar reading "Drag to move the image.
/// Zoom with the wheel or the buttons.", which is the marking screen's line about a tool the settings page does not have.
/// <para>
/// Entry 120 section 10.3 had already fixed this for the library, one screen at a time, which is why it came back somewhere else. This visits
/// every screen there is and checks the line belongs to it, so a screen added later fails here rather than shipping somebody else's words.
/// </para>
/// </summary>
public class ScreenStatusTests
{
    /// <summary>Words that belong to the marking screen and to no other, taken from the tools' own lines.</summary>
    private static readonly string[] TheMarkingScreensWords =
    [
        "Drag to move the image",
        "Tap two points a known distance apart",
        "Tap four corners",
        "Tap the point of aim",
        "Press on each impact",
        "Tap a shot to select it",
    ];

    private static MainWindow Open()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // Named rather than passed as the enum, because a public test method cannot take an internal type.
    [AvaloniaTheory]
    [InlineData(nameof(Destination.Settings))]
    [InlineData(nameof(Destination.Sessions))]
    [InlineData(nameof(Destination.Library))]
    [InlineData(nameof(Destination.Ballistics))]
    [InlineData(nameof(Destination.Compare))]
    public void EveryScreenSaysItsOwnWordsInTheStatusBar(string named)
    {
        var screen = Enum.Parse<Destination>(named);
        var window = Open();

        // Arrive from the marking screen, which is where the borrowed line came from: the settings page was showing it because nothing
        // replaced it on the way in.
        Assert.Equal(Destination.Analyse, window.Where);
        string marking = window.StatusText;
        Assert.Contains(TheMarkingScreensWords, words => marking.Contains(words, StringComparison.Ordinal));

        window.GoTo(screen);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(screen, window.Where);
        Assert.Equal(window.StatusFor(screen), window.StatusText);
        Assert.False(string.IsNullOrWhiteSpace(window.StatusText), $"{screen} left the status bar empty");

        foreach (string words in TheMarkingScreensWords)
        {
            Assert.DoesNotContain(words, window.StatusText, StringComparison.Ordinal);
        }

        window.Close();
    }

    /// <summary>
    /// Going back to the marking screen brings back the marking screen's line rather than leaving the last screen's words behind, which is
    /// the same fault the other way round.
    /// </summary>
    [AvaloniaFact]
    public void ComingBackToTheMarkingScreenBringsBackItsOwnLine()
    {
        var window = Open();
        string marking = window.StatusText;

        window.GoTo(Destination.Settings);
        Dispatcher.UIThread.RunJobs();
        Assert.NotEqual(marking, window.StatusText);

        window.GoTo(Destination.Analyse);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(marking, window.StatusText);
        window.Close();
    }

    /// <summary>Every screen a person can reach has a line of its own, so none of them can be forgotten as one is added.</summary>
    [AvaloniaFact]
    public void NoScreenIsWithoutALineOfItsOwn()
    {
        var window = Open();
        var said = new List<string>();
        foreach (Destination screen in Enum.GetValues<Destination>())
        {
            if (screen == Destination.Analyse)
            {
                continue;
            }

            string line = window.StatusFor(screen);
            Assert.False(string.IsNullOrWhiteSpace(line), $"{screen} has no status line of its own");
            said.Add(line);
        }

        // And no two screens say the same thing, which would make the line useless for telling where you are.
        Assert.Equal(said.Count, said.Distinct(StringComparer.Ordinal).Count());
        window.Close();
    }
}
