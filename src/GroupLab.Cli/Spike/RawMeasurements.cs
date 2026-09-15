using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;

namespace GroupLab.Cli.Spike;

/// <summary>
/// The per-element rows behind every Phase 0 table, written to <c>scans/phase0/measurements/</c>, one file per
/// measurement, so that each figure in PHASE0-RESULTS.md can be checked rather than trusted (NOTES-FROM-PLANNING.md
/// entry 7). Values are rounded to 0.0001 of their unit, which is below every quantity the tables report.
/// </summary>
public static class RawMeasurements
{
    public const string Units =
        "Page lengths are dmm, 0.1 mm, measured from the page top-left unless a name ends in Px or In. Image points are " +
        "pixels, with pixel (u, v) at (u, v), the convention OpenCV reports marker corners in.";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,

        // The record is compared byte for byte across platforms (NOTES-FROM-PLANNING.md entry 32 section 3), and the default is the
        // platform's newline, which wrote CRLF on Windows and LF elsewhere.
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static void Write(string scans, string measurement, object rows)
    {
        string directory = Path.Combine(scans, "measurements");
        Directory.CreateDirectory(directory);
        var document = new { measurement, command = $"grouplab spike {measurement}", units = Units, rows };
        File.WriteAllText(Path.Combine(directory, measurement + ".json"), JsonSerializer.Serialize(document, Options) + "\n");
    }

    public static double R(double value) => Math.Round(value, 4);

    public static object[] Bulls(IReadOnlyList<BullLocation> bulls, TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(definition);
        return [.. bulls.Select(b => (object)new
        {
            label = b.Name,
            scoring = definition.Bulls[b.Index].Scoring,
            declaredX = b.Declared.X,
            declaredY = b.Declared.Y,
            recoveredX = b.Recovered is { } r ? R(r.X) : (double?)null,
            recoveredY = b.Recovered is { } q ? R(q.Y) : (double?)null,
            dx = R(b.Dx),
            dy = R(b.Dy),
            error = R(b.Error),
            inkSpread = b.InkSpread is { } s ? R(s) : (double?)null,
            threshold = b.Threshold is { } t ? R(t) : (double?)null,
            b.Iterations,
            b.EdgePoints,
            b.Failure,
        })];
    }

    public static object[] Corners(RegistrationFit fit)
    {
        ArgumentNullException.ThrowIfNull(fit);
        return [.. fit.Corners.Select(c => (object)new
        {
            c.MarkerId,
            c.Corner,
            imageXPx = R(c.Image.X),
            imageYPx = R(c.Image.Y),
            pageX = R(c.Page.X),
            pageY = R(c.Page.Y),
            error = R(c.Error),
            c.Inlier,
        })];
    }

    public static object? Mapping(IPageMapping? mapping) => mapping switch
    {
        HomographyMapping h => new { model = h.Model, imageToPage = Matrix(h.ImageToPage) },
        RadialHomographyMapping r => new
        {
            model = r.Model,
            centreXPx = r.CentreX,
            centreYPx = r.CentreY,
            scalePx = r.Scale,
            k1 = r.K1,
            k2 = r.K2,
            normalisedToPage = Matrix(r.NormalisedToPage),
            form = "page = normalisedToPage(u (1 + k1 r^2 + k2 r^4)), u = (image - centre) / scale",
        },
        SurfaceMapping s => new
        {
            model = s.Model,
            parameters = s.Parameters,
            form = "sheet = along-ruling * (cos a, sin a, 0) + X(t) * (-sin a, cos a, 0) + Z(t) * (0, 0, 1), where (X, Z) integrates " +
                "(cos, sin) of the tangent angle sum bend[k] (t / 1000)^k along arc length t across the rulings; image = distort(project(R sheet + T))",
        },
        _ => null,
    };

    /// <summary>Everything one measured sample produced, for the sheets and photographs tables.</summary>
    public static object Sample(SampleResult r, TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new
        {
            file = r.Sample.File,
            kind = r.Sample.Kind,
            dpi = r.Sample.Dpi,
            description = r.Sample.Description,
            excluded = r.Sample.Excluded,
            photographGate = r.Sample.Kind == SampleSet.SampleKind.Photograph ? r.Sample.Gate : (SampleSet.PhotographGate?)null,
            gated = r.Sample.Gated,
            tileNamed = r.Sample.Tile,
            tileInferred = r.Fiducials.TileIndex,
            camera = r.Metadata.IsCamera ? new { r.Metadata.CameraModel, r.Metadata.FocalLengthMm, r.Metadata.FNumber, r.Metadata.FocalLength35mm, r.Metadata.DigitalZoomRatio, lensGroupKey = r.Metadata.LensGroupKey } : null,
            markersExpected = r.Fiducials.Expected,
            markersMatched = r.Fiducials.Matches.Select(m => m.Id).Order().ToArray(),
            markersMissing = r.Fiducials.Missing.Select(m => m.Id).ToArray(),
            r.Failure,
            residualRms = r.Fit is { } f ? R(f.RmsResidual) : (double?)null,
            residualMax = r.Fit is { } g ? R(g.MaxResidual) : (double?)null,
            homographyResidualRms = r.Fit?.HomographyRmsResidual is { } h ? R(h) : (double?)null,
            scale = r.Scale is { } s ? new { s.ScaleX, s.ScaleY, s.Scale, pixelsPerDmmX = s.PixelsPerDmmX, pixelsPerDmmY = s.PixelsPerDmmY } : null,
            lens = r.Lens,
            mapping = Mapping(r.Fit?.Mapping),
            corners = r.Fit is { } c ? Corners(c) : null,
            bulls = new { centroid = Bulls(r.Centroid, definition), edgeFit = Bulls(r.EdgeFit, definition) },
        };
    }

    private static double[] Matrix(Homography h) => [.. Enumerable.Range(0, 9).Select(i => h[i / 3, i % 3])];
}
