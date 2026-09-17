using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Analysis;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab analyze &lt;image&gt; [--target &lt;definition&gt;] [--library &lt;directory&gt;]... [--calibre &lt;calibre&gt;] [-v 1|2|3] [--json &lt;marking&gt;]</c>: NOTES-FROM-PLANNING.md entry 33
/// section 1, a photograph or scan of a GroupLab sheet in and a group out, through <see cref="SheetAnalysis"/>. It prints the stage trace in
/// DETECTION-PIPELINE.md section 6.3's console form, then every recovered shot and the pooled group, and with <c>--json</c> writes the result
/// as a marking file the marking screen can open.
/// <para>
/// Without <c>--target</c> the sheet names its own definition, NOTES-FROM-PLANNING.md entry 35 section 6 item 3: <see cref="SheetIdentification"/>
/// reads the GLTD-B frame in its printed codes and finds the definition with that identifier among every <c>*.gltd.json</c> under the
/// <c>--library</c> directories, by default <c>targets</c> in the current directory with the frozen definitions beneath it. It never guesses
/// among definitions, which share marker ids: when the codes cannot be read, it says why and asks for <c>--target</c>.
/// </para>
/// </summary>
public static class AnalyzeVerb
{
    public static int Run(string imagePath, string[] rest, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(rest);
        string? target = null, json = null;
        Calibre? calibre = null;
        var libraries = new List<string>();
        int verbosity = 1;
        for (int i = 0; i < rest.Length; i++)
        {
            switch (rest[i])
            {
                case "--target" when i + 1 < rest.Length:
                    target = rest[++i];
                    break;
                case "--library" when i + 1 < rest.Length:
                    libraries.Add(rest[++i]);
                    break;
                case "--calibre" when i + 1 < rest.Length:
                    calibre = Calibre.Parse(rest[++i], out string? problem);
                    if (problem is not null)
                    {
                        error.WriteLine($"analyze: {problem}");
                        return 2;
                    }

                    break;
                case "--json" when i + 1 < rest.Length:
                    json = rest[++i];
                    break;
                case "-v" when i + 1 < rest.Length && int.TryParse(rest[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int v):
                    verbosity = v;
                    i++;
                    break;
                default:
                    error.WriteLine($"analyze: unknown option {rest[i]}");
                    return 2;
            }
        }

        var result = Analyze(imagePath, target, out string? loadFailure, libraries.Count > 0 ? libraries : null, calibre);
        if (loadFailure is not null)
        {
            error.WriteLine($"analyze: {loadFailure}");
            return 1;
        }

        foreach (var record in result!.Trace)
        {
            output.Write(TraceConsole.Format(record, verbosity));
        }

        output.WriteLine($"total {result.Trace.Sum(r => r.DurationMs)} ms");
        if (result.Failure is not null)
        {
            error.WriteLine($"analyze: {result.Failure}. Mark this image by hand with a reference length or rectangle.");
            return 1;
        }

        output.WriteLine();
        output.WriteLine("shot        bull   page x, y (in)      offset from bull x, y (in)");
        // NOTES-FROM-PLANNING.md entry 75: a shot is named by its bull, in the sheet's order, and never by the order detection emitted it.
        var byId = result.Shots.ToDictionary(s => s.Id);
        foreach (var label in ShotLabels.For(result.Marking!).Where(l => byId.ContainsKey(l.ShotId)))
        {
            var s = byId[label.ShotId];
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{label.Text ?? "",-10}  {s.BullLabel ?? "none",-5}  {s.PageInches.X,7:0.000}, {s.PageInches.Y,7:0.000}   {(s.OffsetInches is { } o ? $"{o.X,+8:+0.000;-0.000}, {o.Y,+8:+0.000;-0.000}" : "no bull")}{(s.Sighter ? "   sighter, not in the group" : "")}{(result.Marking!.Find(s.Id)?.Oversize is { } flag ? string.Create(CultureInfo.InvariantCulture, $"   {(flag.Tentative ? "may be two holes" : "oversized")}, about {flag.Holes:0.0} holes' area") : "")}"));
        }

        output.WriteLine();
        WriteGroup(output, result.Report!);
        if (json is not null)
        {
            File.WriteAllText(json, MarkingFile.Write(result.Marking!));
            output.WriteLine($"marking written to {json}");
        }

        return 0;
    }

    /// <summary>Where definitions are looked for when the command names neither a definition nor a library.</summary>
    public const string DefaultLibrary = "targets";

    /// <summary>
    /// The whole analysis of one image, as the command runs it: against the definition file named, or, when none is, against the definition
    /// the sheet's codes name among those under <paramref name="libraries"/>. Null with the reason when the image cannot be read, or no
    /// definition can be.
    /// </summary>
    public static SheetAnalysisResult? Analyze(string imagePath, string? definitionPath, out string? failure, IReadOnlyList<string>? libraries = null, Calibre? calibre = null)
    {
        failure = null;
        TargetDefinition? definition = null;
        if (definitionPath is not null)
        {
            var read = GltdJsonReader.ReadFile(definitionPath);
            if (read.Definition is null)
            {
                failure = $"{definitionPath} is not a readable GroupLab definition: {string.Join("; ", read.Diagnostics.Select(d => d.Message))}";
                return null;
            }

            definition = read.Definition;
        }

        var trace = new TraceRecorder();
        GrayImage grey, value;
        ImageMetadata metadata;
        using (var stage = trace.Begin("S0.decode"))
        {
            try
            {
                (grey, metadata) = ImageLoader.Load(imagePath);
                (value, _) = ImageLoader.LoadMaxChannel(imagePath);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or OpenCvSharp.OpenCVException)
            {
                stage.Done(StageStatus.Failed, ex.Message);
                failure = ex.Message;
                return null;
            }

            string camera = metadata.IsCamera ? $", {metadata.CameraMake} {metadata.CameraModel}, lens group {metadata.LensGroupKey}" : "";
            stage.Done(StageStatus.Ok, string.Create(CultureInfo.InvariantCulture, $"{metadata.Format} {grey.Width}x{grey.Height}{camera}"));
        }

        var backend = new OpenCvSharpBackend();
        if (definition is null)
        {
            var identity = SheetIdentification.Identify(grey, SheetIdentification.Candidates(libraries ?? [DefaultLibrary]), backend, trace);
            if (identity.Definition is null)
            {
                failure = $"{identity.Failure}; name the sheet's definition with --target <definition>";
                return null;
            }

            definition = identity.Definition;
        }

        return SheetAnalysis.Run(imagePath, grey, value, metadata, definition, backend, trace, calibre);
    }

    private static void WriteGroup(TextWriter output, GroupReport report)
    {
        if (report.AllShots is not { } all)
        {
            output.WriteLine("group: " + (report.Problem ?? "no shots"));
            return;
        }

        var inv = CultureInfo.InvariantCulture;
        output.WriteLine(string.Create(inv, $"group: {all.Shots} shots, pooled about their own bulls{(report.SighterShots > 0 ? $", {report.SighterShots} sighter shots left out" : "")}; scale from {report.Scale}"));
        if (report.Detection is { } detection)
        {
            output.WriteLine("  " + detection);
        }

        if (all.DispersionWithheld is { } withheld)
        {
            output.WriteLine("  " + withheld);
            return;
        }

        static string Figure(string name, ReportedEstimate e) => e is { Lower: { } lo, Upper: { } hi, Coverage: { } c }
            ? string.Create(CultureInfo.InvariantCulture, $"  {name,-15} {e.Value:0.000} in   {100 * c:0.0}% interval {lo:0.000} to {hi:0.000} in")
            : string.Create(CultureInfo.InvariantCulture, $"  {name,-15} {e.Value:0.000} in   no interval: {e.IntervalUnavailable}");
        output.WriteLine(Figure("mean radius", all.MeanRadius!));
        output.WriteLine(Figure("sigma", all.Sigma!));
        output.WriteLine(Figure("extreme spread", all.ExtremeSpread!));
        if (all.CentreFromAim is { } c)
        {
            output.WriteLine(string.Create(inv, $"  centre from aim  {c.X:+0.000;-0.000} in across, {c.Y:+0.000;-0.000} in down"));
        }

        if (all.TrueSizeRange is { } range && all.Shots < GroupAnalysis.SmallGroupShots)
        {
            output.WriteLine(string.Create(inv, $"  from {all.Shots} shots the true group size could be {range.Lower:0.00} to {range.Upper:0.00} times what they measure"));
        }
    }
}
