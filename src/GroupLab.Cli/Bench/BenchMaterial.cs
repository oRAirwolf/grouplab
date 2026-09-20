using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.Cli.Bench;

/// <summary>
/// What the benchmark measures against, NOTES-FROM-PLANNING.md entry 117 section 3a: "it runs with no input of any kind". Every piece here is
/// either committed in this repository or made by GroupLab out of its own sheet, so <c>grouplab bench</c> runs on a bare checkout, on CI and
/// on a machine that has never analysed a target. Nothing is read from a person's own folders, and nothing is written outside a temporary
/// directory this class makes and removes.
/// </summary>
public sealed class BenchMaterial : IDisposable
{
    private BenchMaterial(string root, string temporary, TargetDefinition definition)
    {
        Root = root;
        Temporary = temporary;
        Definition = definition;
    }

    /// <summary>The folder holding <c>targets</c>, and where present <c>scans</c>: the repository, or the package.</summary>
    public string Root { get; }

    /// <summary>Where generated material goes, removed when the run ends.</summary>
    public string Temporary { get; }

    /// <summary>The sheet everything is measured on: GL-CF25-LTR, the 5 by 5 Letter sheet.</summary>
    public TargetDefinition Definition { get; }

    /// <summary>The sheet's pages, built once.</summary>
    public IReadOnlyList<Scene> Pages { get; private set; } = [];

    /// <summary>A zeroing sheet, which is where the data block and the measurement grid are, so their derivations are measured on a sheet that has them.</summary>
    public TargetDefinition? Zeroing { get; private set; }

    /// <summary>A 25 shot sheet GroupLab generated from its own definition, at 300 dpi. It carries nobody's data.</summary>
    public string GeneratedSheet { get; private set; } = "";

    /// <summary>Alan's own scan of an unshot sheet at 600 dpi, or null where the repository's scans are not beside this build.</summary>
    public string? Scan600 { get; private set; }

    /// <summary>The same sheet at 300 dpi, or null.</summary>
    public string? Scan300 { get; private set; }

    /// <summary>Alan's own photograph of an unshot sheet, or null.</summary>
    public string? Photograph { get; private set; }

    /// <summary>The generated sheet loaded once, so a case that is not measuring loading does not pay for it.</summary>
    public (GrayImage Grey, GrayImage Value, ImageMetadata Metadata) Loaded { get; private set; }

    /// <summary>Where a session database is generated and read back.</summary>
    public string Database => Path.Combine(Temporary, "bench-sessions.db");

    /// <summary>
    /// Finds the material and makes what has to be made. The sheet is rendered and shot at with a fixed seed, so two runs of the benchmark
    /// measure the same work.
    /// </summary>
    public static BenchMaterial Prepare(string? root = null)
    {
        root = Locate(root);
        string temporary = Path.Combine(Path.GetTempPath(), "grouplab-bench-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(temporary);
        string definitionPath = Path.Combine(root, "targets", "GL-CF25-LTR.gltd.json");
        var read = GltdJsonReader.ReadFile(definitionPath);
        if (read.Definition is not { } definition)
        {
            throw new InvalidOperationException($"{definitionPath} is not a readable GroupLab definition, so there is nothing to measure against.");
        }

        var material = new BenchMaterial(root, temporary, definition);
        material.Pages = SceneBuilder.Build(definition).Pages;
        string zeroing = Path.Combine(root, "targets", "GL-ZERO-MOA-100Y.gltd.json");
        material.Zeroing = File.Exists(zeroing) ? GltdJsonReader.ReadFile(zeroing).Definition : null;
        material.Scan600 = Existing(root, "scans", "phase0", "gl-cf25-ltr-1-600-dpi.png");
        material.Scan300 = Existing(root, "scans", "phase0", "gl-cf25-ltr-1-300-dpi.png");
        material.Photograph = Existing(root, "scans", "phase0", "20260913_130543.jpg");
        material.GeneratedSheet = material.Generate("bench-25-shots.png", 300);
        material.Loaded = Load(material.GeneratedSheet);
        return material;
    }

    /// <summary>The definition file's path, for the cases that measure reading it.</summary>
    public string DefinitionPath => Path.Combine(Root, "targets", "GL-CF25-LTR.gltd.json");

    /// <summary>Loads an image the way the application does: the grey channel and the strongest channel, with the file's own metadata.</summary>
    public static (GrayImage Grey, GrayImage Value, ImageMetadata Metadata) Load(string path)
    {
        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        return (grey, value, metadata);
    }

    /// <summary>
    /// A sheet with shots in it, rendered and then shot at by a seeded random number generator, exactly as <c>grouplab sample</c> does. It is
    /// nobody's target, so it needs nobody's consent, and it is the only way the benchmark can measure hole detection: the repository's own
    /// scans are of unshot sheets.
    /// </summary>
    public string Generate(string name, double dpi)
    {
        string path = Path.Combine(Temporary, name);
        var render = SceneRasterizer.Rasterize(Pages[0], dpi);
        var random = new Random(117);
        var holes = Definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871))
            .ToList();
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);
        using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        if (!Cv2.ImWrite(path, mat))
        {
            throw new InvalidOperationException($"the benchmark could not write its own sample to {path}");
        }

        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Temporary))
            {
                Directory.Delete(Temporary, recursive: true);
            }
        }
        catch (IOException)
        {
            // A file the operating system is still holding is not worth failing a benchmark over.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string? Existing(string root, params string[] parts)
    {
        string path = Path.Combine([root, .. parts]);
        return File.Exists(path) ? path : null;
    }

    /// <summary>The folder holding <c>targets</c>: the one named, the working directory, the folder this build sits in, or an ancestor of either.</summary>
    private static string Locate(string? root)
    {
        if (root is not null)
        {
            return root;
        }

        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "targets", "GL-CF25-LTR.gltd.json")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new InvalidOperationException("the benchmark could not find a targets folder holding GL-CF25-LTR.gltd.json; run it from the repository, or name the folder with --root.");
    }
}
