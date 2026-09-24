using System.Globalization;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// The session report, NOTES-FROM-PLANNING.md entry 112 section 2. Every sentence and figure in it is produced by the same functions the
/// analysis screen lays out, so nothing on paper is more certain than the screen: the intervals, the without-exclusions lines, the stringing
/// power statement and the refusals come across unchanged. What the screen keeps behind a "why" the report prints on its second page.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The report of the marking as it stands, as the analysis screen shows it.</summary>
    internal SessionReport BuildReport()
    {
        var state = session.State;
        var analysis = GroupAnalysis.Analyse(state);
        var saved = currentSession is { } id ? sessions?.Get(id) : null;
        string sheet = plotDefinition?.Name ?? (state.ImagePath is { } image ? Path.GetFileName(image) : "Marked by hand");
        string date = saved?.ShotDate ?? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var particulars = new List<ReportPair>
        {
            new("Sheet", sheet),
            new("Date", date),
            new("Distance", state.ShotDistanceInches is { } d ? units.DistanceText(d) : "not set"),
            new("Rifle", state.Rifle is { } rifle ? $"{rifle.Name}, {rifle.DescribeClick()}" : "not chosen"),
            new("Barrel", state.Barrel ?? "not chosen"),
            new("Load", state.Load ?? "not chosen"),
            new("Calibre", state.Calibre?.Name ?? "not set"),
            new("Shots", CountedShots(state).ToString(CultureInfo.InvariantCulture)),
        };

        var summary = new List<string>();
        var figures = new List<ReportFigure>();
        var cards = new List<ReportCard>();
        var why = new List<ReportSection>();
        if (analysis.AllShots is { } all)
        {
            var reduced = analysis.WithoutExclusions!;
            bool excluded = analysis.Excluded > 0;
            summary.Add(PlacedLine(all.Shots, analysis.Automatic, analysis.Corrected, analysis.Manual)
                + string.Create(CultureInfo.InvariantCulture, $"{(excluded ? $"; {reduced.Shots} without the {analysis.Excluded} excluded" : "")}{(analysis.NotShots > 0 ? $"; {analysis.NotShots} marked not a shot" : "")}."));
            if (all.CentreFromAim is { } offset)
            {
                var (value, detail) = CentreTexts(AsDisplayed(offset));
                figures.Add(new ReportFigure("Center from aim", value, detail is null ? [] : [detail]));
            }
            else
            {
                summary.Add($"Center from aim: {all.CentreFromAimUnavailable}.");
            }

            if (all.DispersionWithheld is { } withheld)
            {
                summary.Add(withheld);
            }
            else
            {
                GroupFigures? without = excluded ? reduced : null;
                figures.Add(new ReportFigure("Mean radius", units.Length(all.MeanRadius!.Value), FigureDetails(all.MeanRadius, without, f => f.MeanRadius, interval: true)));
                figures.Add(new ReportFigure("Sigma", units.Length(all.Sigma!.Value), FigureDetails(all.Sigma, without, f => f.Sigma, interval: true)));
                figures.Add(new ReportFigure("Extreme spread", units.Length(all.ExtremeSpread!.Value), FigureDetails(all.ExtremeSpread, without, f => f.ExtremeSpread, interval: false)));
                if (all is { Cep90: { } cep90, Cep50: not null, Cep95: not null })
                {
                    figures.Add(new ReportFigure("CEP 90", units.Length(cep90.Value), CepDetails(all, without)));
                    why.Add(new ReportSection("CEP", [CepWhy]));
                }

                if (all is { SdX: not null, SdY: not null } && SizeValue(all) is { } size)
                {
                    figures.Add(new ReportFigure("Group width \u00d7 height", size, SizeDetails(all, without)));
                }

                foreach (var card in JudgementCardsFor(state, all))
                {
                    string title = card.Name == "shape" ? "Shape" : "Worst shot";
                    cards.Add(new ReportCard(title, card.Verdict, card.Evidence, card.Why));
                    if (card.Why.Count > 0)
                    {
                        why.Add(new ReportSection(title, card.Why));
                    }
                }

                why.Add(new ReportSection("More figures", MoreFigureLines(state, all)));
            }

            foreach (var shot in state.Shots.Where(s => s.IsShot && s.Oversize is not null))
            {
                summary.Add(shot.Oversize!.Describe(ShotLabel(shot.Id)));
            }
        }
        else
        {
            summary.Add(analysis.Problem ?? "No shots are marked.");
        }

        var zero = ZeroFor(state);
        var zeroCard = new ReportCard("Zero correction", zero.Verdict,
            [.. zero.Rows.Select(r => string.Join("  ", new[] { r.Label + ":", r.Linear, r.Angular, r.Sits }.Where(t => t.Length > 0))), .. zero.Note is { } note ? [note] : Array.Empty<string>(), .. zero.AtZero is { } atZero ? [atZero] : Array.Empty<string>()],
            zero.Why);
        if (zero.Why.Count > 0)
        {
            why.Insert(0, new ReportSection("Zero correction", zero.Why));
        }

        var open = ReviewQueue.For(state, analyseSighters).Where(i => !i.Resolved).ToList();
        if (open.Count > 0)
        {
            why.Add(new ReportSection("Decisions left unmade", [open.Count == 1
                ? "1 decision was left unmade when this was accepted, and every figure here inherits it."
                : $"{open.Count} decisions were left unmade when this was accepted, and every figure here inherits them."]));
        }

        var (headings, rows) = ShotTable(state);
        var exclusions = state.Shots.Where(s => s.IsShot && s.Exclusion is not null)
            .Select(s => $"Shot {ShotLabel(s.Id)}: {s.Exclusion!.Value.Words()}. It is drawn hollow, struck through in the table, and every figure is given with and without it.")
            .ToList();
        var unmade = open.Select(i => $"{ReviewTitle(i.Kind)}{(i.ShotId is { } shot ? ", shot " + ShotLabel(shot) : i.Bull is { } b ? ", bull " + BullLabel(b) : "")}: {i.Sentence}").ToList();

        var registration = new List<string>
        {
            state.Scale switch
            {
                null => "No scale: the figures are in image pixels only.",
                SheetReference when registrationResidual is { } residual => "Registered from the sheet's markers, residual " + units.Length(residual) + ".",
                SheetReference => "Registered from the sheet's markers.",
                _ => "Scale set by hand.",
            },
        };
        if (state.Scale is { } scale)
        {
            registration.Add("From " + scale.Describe(units) + ".");
        }

        registration.Add(state.ImagePath is { } path
            ? $"Image: {Path.GetFileName(path)}{(saved?.ImageSha256 is { } hash ? ", SHA-256 " + hash : "")}."
            : "No image: the shots were marked by hand.");

        var identity = new List<ReportPair>
        {
            new("GroupLab", AppInfo.Version),
            new("Session", saved is null ? "not saved" : saved.Id.ToString(CultureInfo.InvariantCulture)),
            new("Sheet identifier", saved?.DefinitionId ?? (plotDefinition is { } defined ? GltdBinary.Encode(defined).Encoding?.DefinitionId ?? "none" : "none")),
            new("Written", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
        };

        var reportPlot = new ReportPlot(
            ReportDiscs(state),
            [.. plot.Shots.Select(s => new ReportShot(s.Label, s.Offset, s.Excluded))],
            state.Calibre?.DiameterInches,
            plot.Centre,
            plot.Cep50Inches,
            plot.Cep90Inches,
            "Every scoring shot on one bull, each from its own bull's center. Hollow: excluded, drawn and not counted. Blue: the center of the counted shots, with CEP 50 and CEP 90 about it.",
            UnitSettings.Symbol(units.Linear),
            UnitSettings.FromInches(1, units.Linear));
        return new SessionReport(sheet, particulars, reportPlot, summary, figures, zeroCard, cards, headings, rows, exclusions, unmade, registration, why, identity);
    }

    /// <summary>The centre from aim as the figure row shows it: the offsets in the length unit, and in the angular unit when there is a distance.</summary>
    private (string Value, string? Detail) CentreTexts(PointD centre)
    {
        double? distance = session.State.ShotDistanceInches;
        string across = centre.X >= 0 ? "right" : "left", down = centre.Y >= 0 ? "low" : "high";
        string value = $"{units.Length(Math.Abs(centre.X))} {across}, {units.Length(Math.Abs(centre.Y))} {down}";
        return (value, units.AngleText(Math.Abs(centre.X), distance) is { } x ? $"{x} {across}, {units.AngleText(Math.Abs(centre.Y), distance)} {down}" : null);
    }

    /// <summary>The shot table as the analysis shows it, with the bull always named and each shot's standing, an excluded one struck through with its reason.</summary>
    private (IReadOnlyList<string> Headings, IReadOnlyList<ReportRow> Rows) ShotTable(MarkingState state)
    {
        var centre = plot.Centre;
        var reasons = state.Shots.Where(s => s.Exclusion is not null).ToDictionary(s => s.Id, s => s.Exclusion!.Value.InSentence());
        var ordered = plot.Shots.OrderBy(p => int.TryParse(p.Label, NumberStyles.Integer, CultureInfo.InvariantCulture, out int k) ? k : int.MaxValue).ThenBy(p => p.Label, StringComparer.Ordinal);
        var rows = new List<ReportRow>();
        foreach (var shot in ordered)
        {
            double r = centre is { } c ? Math.Sqrt(Math.Pow(shot.Offset.X - c.X, 2) + Math.Pow(shot.Offset.Y - c.Y, 2)) : 0;
            var shown = AsDisplayed(shot.Offset);
            rows.Add(new ReportRow(
                [shot.Label, shot.Bull ?? "", units.Number(shown.X), units.Number(-shown.Y), units.Number(r), reasons.TryGetValue(shot.Id, out string? reason) ? "excluded: " + reason : "counted"],
                shot.Excluded));
        }

        string unit = UnitSettings.Symbol(units.Linear);
        return (["shot", "bull", $"across, {unit}", $"up/down, {unit}", $"radius, {unit}", "standing"], rows);
    }

    /// <summary>The scoring bull's discs as the sheet prints them, outermost first, for the report's plot.</summary>
    private IReadOnlyList<ReportDisc> ReportDiscs(MarkingState state)
    {
        if (plotDefinition is not { } definition || definition.Bulls.FirstOrDefault(b => b.Scoring) is not { } bull
            || definition.RingSets.FirstOrDefault(r => r.Key == bull.RingSet) is not { } rings || state.Scale is null)
        {
            return [];
        }

        var inks = definition.Inks.ToDictionary(i => i.Key);
        return [.. rings.Discs.Select(d => inks.TryGetValue(d.Ink, out var ink)
            ? new ReportDisc(d.Diameter / 254.0, Colour(ink.Srgb), ink.Role == GroupLab.Core.Gltd.Model.InkRole.Paper)
            : new ReportDisc(d.Diameter / 254.0, new Rgb(255, 255, 255), true))];
    }

    private static Rgb Colour(string srgb) => new(
        byte.Parse(srgb.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        byte.Parse(srgb.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        byte.Parse(srgb.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    /// <summary>The report button: the PDF saved where the person chooses, named after the sheet and the date.</summary>
    private async Task ReportDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "report"));
        var report = BuildReport();
        string date = report.Particulars.First(p => p.Label == "Date").Value;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the session report",
            SuggestedFileName = $"{Path.GetFileNameWithoutExtension(session.State.ImagePath ?? "group")} report {date}.pdf",
            DefaultExtension = "pdf",
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "report"), ("chosen", file is not null));
        if (file?.TryGetLocalPath() is { } path)
        {
            WriteReport(path, report);
        }
    }

    /// <summary>Writes the report, and says where; also the headless tests' way in.</summary>
    internal void WriteReport(string path, SessionReport? report = null)
    {
        byte[] pdf = ReportWriter.Write(report ?? BuildReport());
        File.WriteAllBytes(path, pdf);
        status.Text = "Report saved to " + path;
        DiagnosticLog.Info("file.save", [.. DiagnosticLog.File(path), ("kind", "report"), ("bytes", pdf.Length)]);
    }
}
