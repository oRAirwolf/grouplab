using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Analysis;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Trace;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab analyze &lt;image&gt; --target &lt;definition&gt; [-v 1|2|3] [--json &lt;marking&gt;]</c>: NOTES-FROM-PLANNING.md entry 33
/// section 1, a photograph or scan of a GroupLab sheet in and a group out, through <see cref="SheetAnalysis"/>. It prints the stage trace in
/// DETECTION-PIPELINE.md section 6.3's console form, then every recovered shot and the pooled group, and with <c>--json</c> writes the result
/// as a marking file the marking screen can open.
/// <para>
/// <c>--target</c> is required for now. The definition's identifier is printed on the sheet and encoded in its codes, but nothing in the
/// pipeline reads either yet, and guessing among the built-in definitions, which share marker ids, is not safe.
/// </para>
/// </summary>
public static class AnalyzeVerb
{
    public static int Run(string imagePath, string[] rest, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(rest);
        string? target = null, json = null;
        int verbosity = 1;
        for (int i = 0; i < rest.Length; i++)
        {
            switch (rest[i])
            {
                case "--target" when i + 1 < rest.Length:
                    target = rest[++i];
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

        if (target is null)
        {
            error.WriteLine("analyze: --target <definition> is required; nothing reads the sheet's identifier from the image yet.");
            return 2;
        }

        var result = Analyze(imagePath, target, out string? loadFailure);
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
        output.WriteLine("shot  bull   page x, y (in)      offset from bull x, y (in)");
        foreach (var s in result.Shots)
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{s.Id,4}  {s.BullLabel ?? "none",-5}  {s.PageInches.X,7:0.000}, {s.PageInches.Y,7:0.000}   {(s.OffsetInches is { } o ? $"{o.X,+8:+0.000;-0.000}, {o.Y,+8:+0.000;-0.000}" : "no bull")}{(s.Sighter ? "   sighter, not in the group" : "")}"));
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

    /// <summary>The whole analysis of one image against one definition file, as the command runs it; null with the reason when either cannot be read.</summary>
    public static SheetAnalysisResult? Analyze(string imagePath, string definitionPath, out string? failure)
    {
        failure = null;
        var read = GltdJsonReader.ReadFile(definitionPath);
        if (read.Definition is null)
        {
            failure = $"{definitionPath} is not a readable GroupLab definition: {string.Join("; ", read.Diagnostics.Select(d => d.Message))}";
            return null;
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

        return SheetAnalysis.Run(imagePath, grey, value, metadata, read.Definition, new OpenCvSharpBackend(), trace);
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
