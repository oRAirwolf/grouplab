using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab identify-trial &lt;folder&gt;...</c>, NOTES-FROM-PLANNING.md entry 386 section 2 (question 83 (b)): every picture under the
/// folders identified as the application does, and wherever no GroupLab marker is found, identified again the old way, every size, the
/// corners and the cut-outs, so a sheet the new way would lose is named. Pictures with markers take the same path either way. Nothing it
/// reads is written anywhere; names are printed, never metadata.
/// </summary>
public static class IdentifyTrial
{
    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Count == 0)
        {
            error.WriteLine("grouplab identify-trial <folder>...");
            return 2;
        }

        var inv = CultureInfo.InvariantCulture;
        var library = SheetIdentification.Candidates([AnalyzeVerb.DefaultLibrary]);
        var backend = new OpenCvSharpBackend();
        int pictures = 0, withMarkers = 0, lost = 0;
        double newMs = 0, oldMs = 0;
        foreach (string file in args.SelectMany(f => Directory.EnumerateFiles(f, "*.*", SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal))
        {
            GrayImage image;
            try
            {
                image = ImageLoader.Load(file).Image;
            }
            catch (Exception e) when (e is IOException or InvalidDataException or OpenCvSharp.OpenCVException)
            {
                continue;
            }

            pictures++;
            if (LiveSheet.PhotographMarkers(image, backend).Count > 0)
            {
                withMarkers++;
                continue;
            }

            var watch = Stopwatch.StartNew();
            string? now = SheetIdentification.Identify(image, library, backend, new TraceRecorder()).DefinitionId;
            double a = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            string? before = SheetIdentification.Identify(image, library, backend, new TraceRecorder(), wholeOnlyWithoutMarkers: false).DefinitionId;
            double b = watch.Elapsed.TotalMilliseconds;
            newMs += a;
            oldMs += b;
            bool lostHere = before is not null && before != now;
            lost += lostHere ? 1 : 0;
            output.WriteLine(string.Create(inv, $"{Path.GetFileName(file)}: no marker; now {now ?? "none"} in {a / 1000:0.0} s, the old way {before ?? "none"} in {b / 1000:0.0} s{(lostHere ? "  LOST" : "")}"));
        }

        output.WriteLine(string.Create(inv, $"{pictures} pictures, {withMarkers} with GroupLab markers (the same path either way), {pictures - withMarkers} without: {newMs / 1000:0} s now against {oldMs / 1000:0} s the old way; sheets lost: {lost}"));
        return 0;
    }
}
