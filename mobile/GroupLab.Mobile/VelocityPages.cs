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
    /// The chronograph entry: the readings pasted or typed, read, and the pairing proposed against the shots as their labels number them. As
    /// on the desktop the readings are never assumed to line up with the shots (DESIGN.md section 15). Entry 351, pairing A: reading the list
    /// opens a row per reading with its mark, each changed from a sheet; the pairing is kept only when the person presses "Keep this pairing",
    /// and "Leave unpaired" keeps the readings with no shot beside any of them.
    /// </summary>
    public static Control Chronograph(long? sessionId, MarkingState state, Action done)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(done);
        var column = new StackPanel { Spacing = 12 };
        var host = new ContentControl();
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
        var labels = ShotLabels.For(state);
        string ShotName(int id) => labels.FirstOrDefault(l => l.ShotId == id)?.Text ?? id.ToString(CultureInfo.InvariantCulture);
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

        // Entry 342: the string an import put in the box, so reading it proposes the marks its own evidence gives; typing changes the box and drops it.
        ChronographImport? imported = null;
        string? importedText = null;
        void Remember(ChronographImport read)
        {
            box.Text = string.Join(", ", read.VelocitiesFps.Select(v => v.ToString("0.#", CultureInfo.InvariantCulture)));
            (imported, importedText) = (read, box.Text);
        }

        column.Children.Add(box);
        column.Children.Add(Screens.Primary("Read the list", () =>
        {
            var (readings, refusal) = GroupLab.Core.Records.Chronograph.Read(box.Text ?? "");
            if (refusal is not null || readings.Count == 0)
            {
                said.Text = refusal ?? "Type or paste the readings first.";
                return;
            }

            // Entry 351: a row per reading, its mark proposed by the import's own evidence where the list is still the import's.
            var from = box.Text == importedText ? imported : null;
            var marks = new ChronographMarks(shots, readings, from?.Shots, ShotName, from?.InMetres == true);
            DiagnosticLog.Info("chronograph.pairing", ("readings", readings.Count), ("reasons", marks.Reasons.Count));
            var entry = host.Content;
            host.Content = new PairingView(marks, pairs => Keep(readings, pairs), () => host.Content = entry);
        }).Id("chrono-read"));
        // Entry 331 section 2: a chronograph file instead of typing, read into the same box and then read as the list is.
        var columns = new WrapPanel();
        void Imported(string text, int? chosen)
        {
            columns.Children.Clear();
            ChronographImport read;
            try
            {
                read = ChronographFiles.Read(text, chosen);
            }
            catch (FormatException e)
            {
                ProblemSheet.Stop(said, said, "That file could not be read", "That file could not be read as a chronograph file: " + e.Message);
                return;
            }

            DiagnosticLog.Info("chronograph.import", ("format", read.Format.ToString()), ("readings", read.VelocitiesFps.Count));
            Remember(read);
            said.Text = read.Said;
            if (read.Format == ChronographFormat.Generic)
            {
                for (int i = 0; i < read.Columns.Count; i++)
                {
                    int at = i;
                    columns.Children.Add(Screens.Choice((at == read.Column ? "Using " : "Use ") + read.Columns[at], () => Imported(text, at)));
                }
            }
        }

        column.Children.Add(Screens.Choice("Import a file", async () =>
        {
            if (TopLevel.GetTopLevel(column)?.StorageProvider is not { } storage)
            {
                return;
            }

            var files = await storage.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions { Title = "Import a chronograph file", AllowMultiple = false });
            if (files.FirstOrDefault() is not { } file)
            {
                return;
            }

            // Entry 352 item 3: read to a limit, so a huge file is refused in words rather than filling the phone's memory.
            byte[] bytes;
            try
            {
                await using var stream = await file.OpenReadAsync();
                bytes = await ChronographFiles.ReadBoundedAsync(stream);
            }
            catch (Exception e) when (e is FormatException or IOException or UnauthorizedAccessException)
            {
                ProblemSheet.Stop(said, said, "That file could not be read", "That file could not be read as a chronograph file: " + e.Message);
                return;
            }

            if (Path.GetExtension(file.Name).ToLowerInvariant() is ".csv" or ".txt" or "")
            {
                string text;
                try
                {
                    text = ChronographFiles.Text(bytes);
                }
                catch (FormatException e)
                {
                    ProblemSheet.Stop(said, said, "That file could not be read", "That file could not be read as a chronograph file: " + e.Message);
                    return;
                }

                Imported(text, null);
                return;
            }

            // Entry 334: a workbook, a Garmin Xero workbook of the strings selected in ShotView, one sheet per string; each is a choice by its name.
            IReadOnlyList<ChronographImport> strings;
            try
            {
                strings = ChronographFiles.ReadFile(new MemoryStream(bytes, writable: false), file.Name, out _);
            }
            catch (Exception e) when (e is FormatException or NotSupportedException or IOException or ExcelDataReader.Exceptions.ExcelReaderException)
            {
                ProblemSheet.Stop(said, said, "That file could not be read", "That file could not be read as a chronograph file: " + e.Message);
                return;
            }

            columns.Children.Clear();
            void Choose(ChronographImport chosen)
            {
                Remember(chosen);
                said.Text = chosen.Said + (chosen.Disagrees is { } d ? " " + d : "");
            }

            if (strings.Count == 0)
            {
                said.Text = "That file holds no chronograph string GroupLab can read.";
                return;
            }

            Choose(strings[0]);
            if (strings.Count > 1)
            {
                foreach (var each in strings)
                {
                    var chosen = each;
                    columns.Children.Add(Screens.Choice($"{chosen.Name ?? "A string"}, {chosen.VelocitiesFps.Count} shots", () => Choose(chosen)));
                }
            }
        }).Id("chrono-import"));
        column.Children.Add(said);
        column.Children.Add(columns);
        column.Children.Add(Screens.Choice("Back", done));
        host.Content = Screens.Page(column);
        return host;
    }

    /// <summary>
    /// Entry 342: the pairing of a list just read, with the marks an imported string's own evidence proposes and their reasons, each after a
    /// space; in order with no reasons where the list was typed, or no longer matches the import.
    /// </summary>
    internal static (IReadOnlyList<ChronographPair> Pairs, string Reasons) Proposed(IReadOnlyList<int> shots, IReadOnlyList<double> readings, ChronographImport? imported)
    {
        if (imported is not { } from || from.Shots.Count != readings.Count || from.Shots.Count == 0)
        {
            return (GroupLab.Core.Records.Chronograph.Pair(shots, readings), "");
        }

        var proposal = ChronographReconciliation.Propose(shots, from.Shots);
        return (proposal.Pairs(shots, readings), string.Concat(proposal.Reasons.Select(r => " " + r)));
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
