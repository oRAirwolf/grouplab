using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// A chronograph string entered by hand, and the reconciliation DESIGN.md section 15 requires, NOTES-FROM-PLANNING.md entry 115 section 3. The
/// readings are pasted as a list; they are never assumed to line up with the shots. GroupLab proposes the in-order pairing and a person accepts
/// it, after marking any reading that belongs to no shot, such as the fouling round fired into the berm, or any shot the chronograph missed.
/// Nothing downstream assumes a shot has a velocity: what the readings give is their own spread, which is fed to the load's velocity SD with a
/// note of where it came from, and that is what the hit probability of entry 113 section 3 takes as an input.
/// <para>
/// Garmin Xero import waits for a sample file. Once there is one it is a reader that produces the same list of numbers as this box.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    private readonly TextBox chronoSource = new() { Width = 180, Text = "Chronograph" };
    private readonly TextBox chronoDate = new() { Width = 120, Text = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
    private readonly TextBox chronoReadings = new() { AcceptsReturn = true, Height = 72, HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "2705, 2711, 2698 ..." };
    private readonly StackPanel chronoRows = new() { Spacing = 0 };
    private readonly StackPanel chronoLines = new() { Spacing = Tokens.Space4 };

    /// <summary>Entry 141 section 5.2.5: the readings drawn, with the mean and one SD marked on them.</summary>
    private readonly StackPanel chronoPicture = new() { Spacing = Tokens.Space8 };

    private readonly VelocityStrip velocityStrip = new();
    private List<double> chronoValues = [];
    private readonly HashSet<int> chronoShotsWithNoReading = [];
    private readonly HashSet<int> chronoReadingsOfNoShot = [];

    private void BuildChronograph(StackPanel column)
    {
        column.Children.Add(Heading("Chronograph"));
        column.Children.Add(Line("A string of velocities for the session open in the analysis, pasted or typed. They are reconciled with the shots, never assumed to line up with them."));
        column.Children.Add(Row(FieldLabel("From"), chronoSource, FieldLabel("Date"), chronoDate));
        column.Children.Add(chronoReadings);
        column.Children.Add(Row(Button("Read the list", () => ReadChronograph()), Button("Accept the mapping", AcceptChronograph), Button("Start again", () =>
        {
            chronoValues = [];
            chronoShotsWithNoReading.Clear();
            chronoReadingsOfNoShot.Clear();
            FillChronograph();
        })));
        column.Children.Add(chronoLines);
        column.Children.Add(chronoPicture);
        column.Children.Add(chronoRows);
    }

    /// <summary>The shots a string is reconciled against: the ones the figures count, in the order the shot table lists them.</summary>
    private List<MarkedShot> ChronographShots() =>
        [.. GroupShots(session.State).OrderBy(s => int.TryParse(ShotLabel(s.Id), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : int.MaxValue).ThenBy(s => ShotLabel(s.Id), StringComparer.Ordinal)];

    /// <summary>Reads the list as typed, and proposes the pairing; the refusal names what is not a velocity.</summary>
    internal void ReadChronograph(string? source = null, string? date = null, string? list = null)
    {
        chronoSource.Text = source ?? chronoSource.Text;
        chronoDate.Text = date ?? chronoDate.Text;
        chronoReadings.Text = list ?? chronoReadings.Text;
        var (velocities, refusal) = Chronograph.Read(chronoReadings.Text ?? "");
        chronoShotsWithNoReading.Clear();
        chronoReadingsOfNoShot.Clear();
        chronoValues = [.. velocities];
        if (refusal is not null)
        {
            problem.Text = refusal;
        }

        FillChronograph();
    }

    /// <summary>Marks a reading as belonging to no shot, or puts it back.</summary>
    internal void ReadingOfNoShot(int reading)
    {
        if (!chronoReadingsOfNoShot.Add(reading))
        {
            chronoReadingsOfNoShot.Remove(reading);
        }

        FillChronograph();
    }

    /// <summary>Marks a shot as having no reading, or puts it back.</summary>
    internal void ShotWithNoReading(int shot)
    {
        if (!chronoShotsWithNoReading.Add(shot))
        {
            chronoShotsWithNoReading.Remove(shot);
        }

        FillChronograph();
    }

    /// <summary>The pairing as it stands, which is a proposal until it is accepted.</summary>
    private IReadOnlyList<ChronographPair> ChronographPairs() =>
        Chronograph.Pair([.. ChronographShots().Select(s => s.Id)], chronoValues, chronoShotsWithNoReading, chronoReadingsOfNoShot);

    /// <summary>
    /// Keeps the string on the session with the mapping a person has accepted, and sets the load's velocity SD from the readings kept, saying
    /// where it came from. A reading of no shot is kept in the string, since it was recorded; it simply belongs to no shot.
    /// </summary>
    internal void AcceptChronograph()
    {
        if (sessions is null || currentSession is not { } session_)
        {
            problem.Text = "Accept and analyse the sheet first: a chronograph string belongs to a session.";
            return;
        }

        if (chronoValues.Count == 0)
        {
            problem.Text = "Paste the readings and press Read the list first.";
            return;
        }

        var pairs = ChronographPairs();
        long stringId = sessions.AddChronographString(session_, string.IsNullOrWhiteSpace(chronoSource.Text) ? "Chronograph" : chronoSource.Text!.Trim(),
            string.IsNullOrWhiteSpace(chronoDate.Text) ? null : chronoDate.Text!.Trim(), chronoValues);
        foreach (var pair in pairs.Where(p => p is { ShotId: not null, Reading: not null }))
        {
            sessions.MapShot(new ShotVelocity(session_, pair.ShotId!.Value, stringId, pair.Reading!.Value));
        }

        var kept = pairs.Where(p => p is { ShotId: not null, Reading: not null }).Select(p => chronoValues[p.Reading!.Value]).ToList();
        DiagnosticLog.Info("chronograph.accept", ("session", session_), ("readings", chronoValues.Count), ("mapped", kept.Count));
        string said = string.Create(CultureInfo.InvariantCulture, $"Kept {chronoValues.Count} readings on this session, {kept.Count} of them beside a shot.");

        // Entry 115 section 3: the measured spread is fed in rather than typed, and the record says where it came from.
        if (Chronograph.Spread(kept) is { Readings: >= 3 } spread && book.FindLoad(session.State.Load) is { } load)
        {
            string from = string.Create(CultureInfo.InvariantCulture, $"{spread.Readings} readings, {chronoSource.Text?.Trim()}, {chronoDate.Text?.Trim()}");
            book = book.With(load with { MuzzleVelocitySdFps = spread.SdFps, MuzzleVelocitySdFrom = from });
            SaveBook();
            said += string.Create(CultureInfo.InvariantCulture, $" {load.Name}'s velocity SD is {spread.SdFps:0.0} ft/s, from {from}.");
        }

        status.Text = said;
        chronoValues = [];
        chronoShotsWithNoReading.Clear();
        chronoReadingsOfNoShot.Clear();
        FillBallistics();
        Refresh();
    }

    /// <summary>What the chronograph section says: the strings this session holds, the proposal, and the rows to settle it.</summary>
    internal void FillChronograph()
    {
        chronoLines.Children.Clear();
        chronoRows.Children.Clear();
        FillVelocityPicture();
        if (sessions is null || currentSession is not { } id)
        {
            chronoLines.Children.Add(Line("Accept and analyse a sheet first: a chronograph string belongs to a session."));
            return;
        }

        foreach (var kept in sessions.ChronographStrings(id))
        {
            var spread = Chronograph.Spread(kept.VelocitiesFps);
            int mapped = sessions.ShotVelocities(id).Count(v => v.StringId == kept.Id);
            chronoLines.Children.Add(Detail(string.Create(CultureInfo.InvariantCulture,
                $"{kept.Source}{(kept.RecordedUtc is { } when ? ", " + when : "")}: {kept.VelocitiesFps.Count} readings, {mapped} beside a shot{(spread is null ? "" : $", mean {spread.MeanFps:0} ft/s, SD {spread.SdFps:0.0}")}")));
        }

        if (chronoValues.Count == 0)
        {
            chronoLines.Children.Add(Line("Paste a string of velocities and press Read the list."));
            return;
        }

        var shots = ChronographShots();
        var pairs = ChronographPairs();
        chronoLines.Children.Add(new TextBlock
        {
            Text = Chronograph.Describe(pairs, chronoValues.Count),
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.SemiBold,
            Classes = { pairs.Any(p => p.ShotId is null || p.Reading is null) ? AppStyles.Warn : AppStyles.Good },
        });

        var head = ChronographRow("shot", "reading, ft/s", null, heading: true);
        chronoRows.Children.Add(head);
        int index = 0;
        foreach (var pair in pairs)
        {
            string shot = pair.ShotId is { } shotId ? "Shot " + ShotLabel(shotId) : "no shot";
            string reading = pair.Reading is { } r ? chronoValues[r].ToString("0.#", CultureInfo.InvariantCulture) : "no reading";
            Button? mark = pair switch
            {
                { ShotId: { } s, Reading: not null } => Button("No reading for it", () => ShotWithNoReading(s)),
                { ShotId: { } s } => Button("It has a reading", () => ShotWithNoReading(s)),
                { Reading: { } k } when chronoReadingsOfNoShot.Contains(k) => Button("It is a shot's", () => ReadingOfNoShot(k)),
                { Reading: { } k } => Button("Belongs to no shot", () => ReadingOfNoShot(k)),
                _ => null,
            };
            var row = ChronographRow(shot, reading, mark, heading: false);
            if (index++ % 2 == 1)
            {
                row.Classes.Add(AppStyles.Shaded);
            }

            chronoRows.Children.Add(row);
        }
    }

    private Border ChronographRow(string shot, string reading, Button? mark, bool heading)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("120,120,*") };
        var cells = new Control[]
        {
            new TextBlock { Text = shot, FontFamily = heading ? Tokens.Sans : Mono, FontSize = Tokens.DetailSize, Classes = { heading ? AppStyles.Dim : AppStyles.Secondary } },
            new TextBlock { Text = reading, FontFamily = heading ? Tokens.Sans : Mono, FontSize = Tokens.DetailSize, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, Tokens.Space8, 0), Classes = { heading ? AppStyles.Dim : AppStyles.Secondary } },
            mark ?? (Control)new TextBlock(),
        };
        for (int c = 0; c < cells.Length; c++)
        {
            Grid.SetColumn(cells[c], c);
            grid.Children.Add(cells[c]);
        }

        return new Border { Child = grid, Padding = new Thickness(Tokens.Space4, 1), Classes = { AppStyles.TableRow } };
    }

    /// <summary>The chronograph section's lines and rows, for the headless tests.</summary>
    /// <summary>
    /// The velocity picture, NOTES-FROM-PLANNING.md entry 141 section 5.2.5. It draws the string in hand: the list just read where there is
    /// one, and otherwise the newest string this session has saved. Nothing is combined across strings, because two strings shot on
    /// different days are two measurements and pooling them would invent a spread neither of them has.
    /// </summary>
    private void FillVelocityPicture()
    {
        chronoPicture.Children.Clear();
        var velocities = chronoValues.Count >= 2
            ? chronoValues
            : sessions is not null && currentSession is { } id
                ? sessions.ChronographStrings(id).LastOrDefault()?.VelocitiesFps ?? []
                : [];

        if (velocities.Count < 2)
        {
            return;
        }

        velocityStrip.VelocitiesFps = velocities;
        velocityStrip.Speed = units.Speed;
        velocityStrip.SpeedDifference = units.SpeedDifference;
        velocityStrip.InvalidateVisual();
        chronoPicture.Children.Add(new TextBlock { Text = "How much do these shots vary in velocity?", Classes = { AppStyles.Section } });
        chronoPicture.Children.Add(velocityStrip);
        chronoPicture.Children.Add(Line(velocityStrip.Description));
    }

    /// <summary>What the velocity picture says, for the headless tests.</summary>
    internal string VelocityPictureText =>
        string.Join(" ", chronoPicture.Children.OfType<TextBlock>().Select(t => t.Text));

    internal IEnumerable<string> ChronographText => chronoLines.GetLogicalDescendants().Concat(chronoRows.GetLogicalDescendants()).OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>The pairing as the screen has it, for the headless tests.</summary>
    internal IReadOnlyList<(int? Shot, double? Reading)> ChronographPairing =>
        [.. ChronographPairs().Select(p => (p.ShotId, p.Reading is { } r ? chronoValues[r] : (double?)null))];
}
