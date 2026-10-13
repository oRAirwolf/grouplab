using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 416, Alan's concept A on the desktop: Conditions on the day inside Velocity and the vertical, once it has
/// readings. Closed it says what is in use; open, the values typed change the block at once, the comparison with a standard day appears, the
/// values are kept on the session and saved with it, Fill from Ballistics says why it cannot, Standard day clears them, and Remember offers
/// them to the next sheet of the same day and not the next day's.
/// </summary>
public class Entry416Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static readonly Load WithBc = new("H4350 41.5", null) { BallisticCoefficient = 0.243, DragModel = DragModel.G7, BulletWeightGrains = 175 };

    [AvaloniaFact]
    public void TheConditionsOnTheDayChangeTheBlockAndAreKept()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string store = window.SettingsStore.Path;
        try
        {
            window.Book = RecordBook.Empty.With(WithBc);
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(null, null, WithBc.Name);
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            int shots = window.Session.State.Shots.Count(s => s.IsShot);
            window.ReadChronograph("Chronograph", "2026-10-01", string.Join(", ", Enumerable.Range(0, shots).Select(i => 2700 + ((i * 7) % 23) - 11)));
            window.AcceptChronograph();
            window.ShowBallistics(false);
            Settle();
            var standard = Assert.IsType<VelocityBlock>(window.Velocity);
            Assert.True(standard.HasResult, standard.Sentence);

            // Closed by default, one line saying what is in use.
            var part = Assert.IsType<MainWindow.ConditionsPart>(window.Conditions);
            Assert.Equal("Standard day assumed: no temperature or altitude entered.", part.Summary);
            Assert.Null(part.Temperature);

            window.OpenConditions(true);
            Settle();
            part = window.Conditions!;
            Assert.NotNull(part.FillReason);
            Assert.Null(part.Comparison);

            // Typed values change the block at once and are kept on the session.
            part.Temperature!.Text.Text = "90";
            part.Where!.Text.Text = "6000";
            part.FromBoxes();
            Settle();
            Assert.Equal(new SessionConditions(90, 6000), window.Session.State.Conditions);
            var hot = Assert.IsType<VelocityBlock>(window.Velocity);
            Assert.NotEqual(standard.PredictedSdInches!.Value, hot.PredictedSdInches!.Value, 6);
            part = window.Conditions!;
            Assert.Equal("90 F, 6000 ft, 50% humidity, shot level, from this sheet, and the rest a standard day.", part.Summary);
            Assert.StartsWith("With these, velocity alone is ", part.Comparison, StringComparison.Ordinal);
            Assert.Contains("On a standard day it would be ", part.Comparison, StringComparison.Ordinal);

            // Saved with the session, and offered to the next sheet of the same day only.
            long before = Assert.IsType<long>(window.CurrentSession);
            window.Analyse();
            Settle();
            Assert.True(window.Velocity!.HasResult, $"after the second Accept: {window.Velocity.State}, session {before} then {window.CurrentSession}");
            var saved = window.Sessions!.Get(Assert.IsType<long>(window.CurrentSession))!;
            Assert.Equal(new SessionConditions(90, 6000), MarkingFile.Read(saved.MarkingJson).State.Conditions);
            var today = DateOnly.FromDateTime(DateTime.Now);
            Assert.Equal(new SessionConditions(90, 6000, FromLastSheet: true), window.SettingsStore.ConditionsForNewSheet(today));
            Assert.Null(window.SettingsStore.ConditionsForNewSheet(today.AddDays(1)));

            // Not ticked, the next sheet starts blank.
            part.Remember!.IsChecked = false;
            Settle();
            Assert.Null(window.SettingsStore.ConditionsForNewSheet(today));

            // Standard day clears the four, and the block says so again.
            window.Session.SetConditions(null);
            Settle();
            Assert.Null(window.Session.State.Conditions);
            Assert.True(window.Velocity!.Why.Contains("Standard day assumed: no temperature or altitude entered."), $"{window.Velocity.State}: session {window.CurrentSession}, analysing {window.Analysing}");
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(store);
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
