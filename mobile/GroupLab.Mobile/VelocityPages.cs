using System.Globalization;
using Avalonia.Controls;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile;

/// <summary>
/// The two places the phone's "Velocity and the vertical" card sends a person to, NOTES-FROM-PLANNING.md entry 323 sections 3 and 4: the
/// chronograph entry for "Add readings", and the distance shot for "Set the distance". The phone had neither after a picture was read, and a
/// button that opened nothing would leave the card in state 3 or 4 for good.
/// </summary>
internal static class VelocityPages
{
    /// <summary>
    /// The chronograph entry: the readings pasted or typed, read, and the in-order pairing proposed against the shots as their labels number
    /// them. As on the desktop the readings are never assumed to line up with the shots (DESIGN.md section 15): the pairing is kept only when
    /// the person presses for it, and "Keep without pairing" keeps the readings with no shot beside any of them.
    /// </summary>
    public static Control Chronograph(long? sessionId, MarkingState state, Action done)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(done);
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Chronograph readings"));
        if (sessionId is not { } id)
        {
            column.Children.Add(Screens.Line("This session could not be saved on the phone, so there is nowhere to keep readings for it."));
            column.Children.Add(Screens.Choice("Back", done));
            return Screens.Page(column);
        }

        column.Children.Add(Screens.Line("The velocities of this group, in the order they were fired, separated by commas or spaces."));
        var box = new TextBox { AcceptsReturn = true, MinHeight = 96, TextWrapping = Avalonia.Media.TextWrapping.Wrap, PlaceholderText = "2705, 2711, 2698 ..." }.Id("chrono-readings");
        Screens.Numeric(box);
        var said = Screens.Line("");
        var choices = new StackPanel { Spacing = 8 };
        var labels = ShotLabels.For(state);
        var shots = state.Shots.Where(s => s.IsShot && s.Exclusion is null && !GroupAnalysis.OnSighter(state, s))
            .OrderBy(s => int.TryParse(labels.FirstOrDefault(l => l.ShotId == s.Id)?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : int.MaxValue)
            .Select(s => s.Id).ToList();

        void Keep(IReadOnlyList<double> readings, IReadOnlyList<ChronographPair>? pairs)
        {
            var store = PhoneAnalysis.Store();
            long stringId = store.AddChronographString(id, "Chronograph", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), readings);
            foreach (var pair in pairs?.Where(p => p is { ShotId: not null, Reading: not null }) ?? [])
            {
                store.MapShot(new ShotVelocity(id, pair.ShotId!.Value, stringId, pair.Reading!.Value + 1)); // the store counts readings from 1
            }

            DiagnosticLog.Info("chronograph.accept", ("session", id), ("readings", readings.Count), ("mapped", pairs?.Count(p => p is { ShotId: not null, Reading: not null }) ?? 0));
            done();
        }

        column.Children.Add(box);
        column.Children.Add(Screens.Primary("Read the list", () =>
        {
            choices.Children.Clear();
            var (readings, refusal) = GroupLab.Core.Records.Chronograph.Read(box.Text ?? "");
            if (refusal is not null || readings.Count == 0)
            {
                said.Text = refusal ?? "Type or paste the readings first.";
                return;
            }

            var pairs = GroupLab.Core.Records.Chronograph.Pair(shots, readings);
            said.Text = GroupLab.Core.Records.Chronograph.Describe(pairs, readings.Count);
            choices.Children.Add(Screens.Primary("Keep, paired in this order", () => Keep(readings, pairs)).Id("chrono-keep-paired"));
            choices.Children.Add(Screens.Choice("Keep without pairing", () => Keep(readings, null)).Id("chrono-keep-unpaired"));
        }).Id("chrono-read"));
        column.Children.Add(said);
        column.Children.Add(choices);
        column.Children.Add(Screens.Choice("Back", done));
        return Screens.Page(column);
    }

    /// <summary>The distance shot, in the person's distance unit, kept on the session; the figures then show their angles too.</summary>
    public static Control Distance(UnitSettings units, Action<double> kept, Action back)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(kept);
        ArgumentNullException.ThrowIfNull(back);
        bool metres = units.Distance == DistanceUnit.Metre;
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("The distance shot"));
        column.Children.Add(Screens.Line(metres ? "How far the target was, in metres." : "How far the target was, in yards."));
        var box = Screens.Numeric(new TextBox { MinHeight = Screens.Touch, MinWidth = 110 }).Id("velocity-distance");
        var said = Screens.Line("");
        column.Children.Add(box);
        column.Children.Add(Screens.Primary("Keep this distance", () =>
        {
            if (Screens.Read(box.Text) is not { } d || d <= 0)
            {
                said.Text = "Type the distance as a number above zero.";
                return;
            }

            kept(metres ? d / 0.0254 : d * 36);
        }).Id("velocity-distance-keep"));
        column.Children.Add(said);
        column.Children.Add(Screens.Choice("Back", back));
        return Screens.Page(column);
    }
}
