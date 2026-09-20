using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab sample</c>, NOTES-FROM-PLANNING.md entry 116 section 2: a sheet with shots on it for somebody trying GroupLab who has no
/// printer, no scanner and no rifle. It is GroupLab's own sheet, rendered and then shot at by a random number generator with a fixed seed, so
/// it carries nobody's data and needs nobody's consent. The real scans in the repository are of unshot sheets, so this is what a tester can
/// open and see an analysis of.
/// </summary>
public static class SampleVerb
{
    public const string Usage = "grouplab sample <output-image> [--target <file.gltd.json>] [--dpi <d>] [--seed <n>]";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        string? file = null, target = null;
        double dpi = 300;
        int seed = 116;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--target" when i + 1 < args.Length:
                    target = args[++i];
                    break;
                case "--dpi" when i + 1 < args.Length && double.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d) && d > 0:
                    dpi = d;
                    i++;
                    break;
                case "--seed" when i + 1 < args.Length && int.TryParse(args[i + 1], out int n):
                    seed = n;
                    i++;
                    break;
                case var option when option.StartsWith("--", StringComparison.Ordinal):
                    error.WriteLine($"sample: unknown option {option}");
                    error.WriteLine(Usage);
                    return 2;
                default:
                    file = args[i];
                    break;
            }
        }

        if (file is null)
        {
            error.WriteLine(Usage);
            return 2;
        }

        target ??= Path.Combine(AppContext.BaseDirectory, "targets", "GL-CF25-LTR.gltd.json");
        if (!File.Exists(target))
        {
            error.WriteLine($"sample: there is no {target}; name a definition with --target");
            return 2;
        }

        if (GltdJsonReader.ReadFile(target).Definition is not { } definition)
        {
            error.WriteLine($"sample: {target} is not a readable GroupLab definition");
            return 1;
        }

        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        var random = new Random(seed);

        // A group a rifle might really shoot: every scoring bull holds one hole, a little off the middle.
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871))
            .ToList();
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        using (var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels))
        {
            if (!Cv2.ImWrite(file, mat))
            {
                error.WriteLine($"sample: {file} could not be written");
                return 1;
            }
        }

        output.WriteLine($"Wrote {file}: {definition.Name}, {holes.Count} shots, {dpi:0} dpi, {image.Width} by {image.Height} px.");
        return 0;
    }
}
