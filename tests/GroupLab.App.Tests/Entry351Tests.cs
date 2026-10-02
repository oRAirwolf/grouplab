using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App.Theme;
using GroupLab.Core.Records;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 351 on the computer: the chronograph's rows are a row per reading in the order fired, its number, speed and
/// time and a mark saying what it goes with, and clicking the mark opens, under the row, the same choices in the same words as the phone's
/// sheet. A shot another reading has swaps the two and says so; nothing else moves.
/// </summary>
public class Entry351Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    [AvaloniaFact]
    public void TheRowsOfferThePhonesChoicesInItsWords()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string store = window.SettingsStore.Path;
        try
        {
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            window.ShowBallistics();
            Settle();
            int shots = window.Session.State.Shots.Count(s => s.IsShot);

            // Four readings of another group, a six minute pause, then this group's, its first marked clean bore and its fifth left out.
            var start = new TimeSpan(14, 2, 0);
            var timed = Enumerable.Range(0, shots + 4).Select(i => new ChronographShot(i + 1, 1040.0 + i, CleanBore: i == 4, LeftOutByChronograph: i == 8,
                Time: start + TimeSpan.FromSeconds(30 * i) + (i >= 4 ? TimeSpan.FromMinutes(6) : TimeSpan.Zero))).ToList();
            var import = new ChronographImport(ChronographFormat.GarminXero, [.. timed.Select(t => t.Fps)], "A Garmin Xero export.", false, [], 1) { Name = "afternoon", Shots = timed };
            window.ImportChronographStrings([import], "Sessions_OCT_2026");
            Settle();

            var marks = window.ChronographMarks!;
            Assert.Equal($"Proposed: readings 5 to {shots + 4} are this group", marks.Headline);
            Assert.Contains(window.ChronographText, t => t == marks.Headline);
            Assert.Contains(window.ChronographText, t => t.EndsWith("Click any mark to change it.", StringComparison.Ordinal));
            Assert.Contains(window.ChronographText, t => t == "6 minute pause");
            Assert.Equal("Not this group", marks.Label(0));
            Assert.StartsWith("Shot ", marks.Label(4), StringComparison.Ordinal);
            Assert.EndsWith(", clean bore", marks.Label(4), StringComparison.Ordinal);
            Assert.EndsWith(", left out in ShotView", marks.Label(8), StringComparison.Ordinal);

            Button Mark(int reading) => window.GetLogicalDescendants().OfType<Button>()
                .First(b => (AutomationProperties.GetName(b) ?? "").StartsWith($"Reading {reading}, ", StringComparison.Ordinal));
            var fourth = Mark(4);
            Assert.False(fourth.Classes.Contains(AppStyles.Good) || fourth.Classes.Contains(AppStyles.Warn));
            Assert.Contains(AppStyles.Warn, Mark(5).Classes);
            Assert.Contains(AppStyles.Good, Mark(6).Classes);

            // The choices under the row, the phone's sheet's, in its words and order, then Cancel.
            Click(Mark(4));
            Assert.Contains(window.ChronographText, t => t == "Reading 4, 1043 fps, goes with");
            var words = window.GetLogicalDescendants().OfType<Button>().Select(b => b.Content as string).Where(c => c is not null).ToList();
            int at = words.IndexOf(ChronographMarks.AShotWords + "…") - 1;
            Assert.True(at >= 0);
            Assert.Equal([.. ChronographMarks.Choices.Select(c => c.Kind == ReadingGoesWith.Shot ? c.Words + "…" : c.Words), "Cancel"], words.Skip(at).Take(5));

            // A shot of this group, then shot 2, which reading 6 had: the two swap, and it says so.
            Click(window.GetLogicalDescendants().OfType<Button>().First(b => b.Content as string == ChronographMarks.AShotWords + "…"));
            var shotTwo = window.GetLogicalDescendants().OfType<Button>().First(b => (b.Content as string ?? "").StartsWith("Shot 2,", StringComparison.Ordinal));
            Click(shotTwo);
            Assert.Equal(ReadingGoesWith.Shot, marks[3].Kind);
            Assert.Equal(ReadingGoesWith.NotThisGroup, marks[5].Kind);
            Assert.Contains(window.ChronographText, t => t.Contains("swapped places", StringComparison.Ordinal));

            // Accepted, the pairing as it stands is what is kept.
            window.AcceptChronograph();
            Settle();
            long id = window.CurrentSession!.Value;
            Assert.Equal(shots, window.Sessions!.ShotVelocities(id).Count);
            Assert.Contains(window.Sessions.ShotVelocities(id), v => v.Ordinal == 4);
            Assert.DoesNotContain(window.Sessions.ShotVelocities(id), v => v.Ordinal == 6);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(store);
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
