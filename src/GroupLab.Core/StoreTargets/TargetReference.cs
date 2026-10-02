using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 344 section 1: what sets the scale when a store-bought target is fingerprinted from a camera photograph
/// rather than a flatbed scan. Each is recorded with the reference, with its honest uncertainty.
/// </summary>
public enum ScaleSource
{
    /// <summary>(a) The sheet's printed outer size, typed in, fitted to its four corners.</summary>
    PrintedSize,

    /// <summary>(b) A GroupLab sheet or card in the same photograph, on the same flat surface, read by its markers.</summary>
    GroupLabSheet,

    /// <summary>(c) Two tapped points a typed distance apart, the four corners taking out the angle.</summary>
    TwoPoints,

    /// <summary>A 600 dpi flatbed scan, as the first five products were made.</summary>
    Scan,
}

/// <summary>
/// The flat surface the target lies on, between the photograph's pixels and inches on the target: the straightening of entry 344 section 1.
/// </summary>
public interface ITargetPlane
{
    PointD ToInches(PointD image);

    PointD ToImage(PointD inches);
}

/// <summary>A plane given by a homography from the photograph's pixels to inches.</summary>
public sealed class HomographyPlane(Homography imageToInches) : ITargetPlane
{
    private readonly Homography back = imageToInches.Inverse();

    public Homography ImageToInches { get; } = imageToInches;

    public PointD ToInches(PointD image) => ImageToInches.Apply(image);

    public PointD ToImage(PointD inches) => back.Apply(inches);
}

/// <summary>
/// The plane a GroupLab sheet's registration gives, page decimillimetres taken to inches, its lens correction included where the
/// registration fitted one: that is how the photograph is lens-corrected as GroupLab's own captures are. <paramref name="printScale"/> is the
/// printer's measured scale where a profile says it, so the sheet's inches are real ones.
/// </summary>
public sealed class SheetPlane(IPageMapping mapping, double printScale = 1) : ITargetPlane
{
    public PointD ToInches(PointD image)
    {
        var page = mapping.ToPage(image);
        return new PointD(page.X * printScale / 254, page.Y * printScale / 254);
    }

    public PointD ToImage(PointD inches) => mapping.ToImage(new PointD(inches.X * 254 / printScale, inches.Y * 254 / printScale));
}

/// <summary>
/// A target straightened: the plane, which scale source made it, the four corners in inches (the first at the origin, the printed area
/// running right and down), the relative scale uncertainty at about two standard deviations, and the sentence that says so.
/// </summary>
public sealed record StraightenedTarget(ITargetPlane Plane, ScaleSource Source, double WidthInches, double HeightInches, double Uncertainty, string Says)
{
    /// <summary>The plane moved so the target's first corner is the origin and its first edge runs along x.</summary>
    public static ITargetPlane Anchored(ITargetPlane plane, IReadOnlyList<PointD> imageCorners, out double width, out double height)
    {
        ArgumentNullException.ThrowIfNull(plane);
        ArgumentNullException.ThrowIfNull(imageCorners);
        var c = imageCorners.Select(plane.ToInches).ToArray();
        double angle = Math.Atan2(c[1].Y - c[0].Y, c[1].X - c[0].X);
        double cos = Math.Cos(-angle), sin = Math.Sin(-angle);
        PointD Turn(PointD p) => new(((p.X - c[0].X) * cos) - ((p.Y - c[0].Y) * sin), ((p.X - c[0].X) * sin) + ((p.Y - c[0].Y) * cos));
        var turned = c.Select(Turn).ToArray();
        width = (Distance(turned[0], turned[1]) + Distance(turned[3], turned[2])) / 2;
        height = (Distance(turned[0], turned[3]) + Distance(turned[1], turned[2])) / 2;
        return new TurnedPlane(plane, c[0], angle);
    }

    internal static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

    private sealed class TurnedPlane(ITargetPlane inner, PointD origin, double angle) : ITargetPlane
    {
        public PointD ToInches(PointD image)
        {
            var p = inner.ToInches(image);
            double cos = Math.Cos(-angle), sin = Math.Sin(-angle);
            return new PointD(((p.X - origin.X) * cos) - ((p.Y - origin.Y) * sin), ((p.X - origin.X) * sin) + ((p.Y - origin.Y) * cos));
        }

        public PointD ToImage(PointD inches)
        {
            double cos = Math.Cos(angle), sin = Math.Sin(angle);
            return inner.ToImage(new PointD(origin.X + (inches.X * cos) - (inches.Y * sin), origin.Y + (inches.X * sin) + (inches.Y * cos)));
        }
    }
}

/// <summary>
/// Entry 344 section 1: from one photograph of a blank target to a straightened target, by each of the three scale sources, with an honest
/// uncertainty for each. The uncertainty is a relative error in the scale at about two standard deviations, from what limits each source:
/// <list type="bullet">
/// <item>(a) where the four corners are, to their edges' straightness or a pixel, over the shortest side, and the typed size read to 1/32 in;</item>
/// <item>(b) the GroupLab sheet's registration residual over its own size, carried out to the target's far corner with the square of the
/// reach, plus its print scale, to 0.1 percent with a printer check and 1.5 percent without;</item>
/// <item>(c) the two taps, to three pixels each, over their distance, the typed distance to 1/32 in, and the shape the corners give, which
/// depends on the focal length by the square of the angle's tangent.</item>
/// </list>
/// </summary>
public static class TargetStraightening
{
    /// <summary>How closely a person reads a tape measure: 1/32 in.</summary>
    public const double TypedInches = 1.0 / 32;

    /// <summary>How far a tap lands from where it was meant, pixels of the photograph, one standard deviation.</summary>
    public const double TapPixels = 3;

    /// <summary>
    /// A GroupLab sheet's own size is only real inches when its print scale is known; without a printer profile it is the sheet's own inches,
    /// and printers scale by up to a few percent ("fit to page" prints at 94 to 97 percent).
    /// </summary>
    public const string UnmeasuredPrinter = "It assumes the GroupLab sheet printed at its own size; a printer check (Settings, Printers) removes that doubt.";

    /// <summary>(a) The printed outer size, typed, fitted to the four corners: the rectangle removes the angle exactly.</summary>
    public static StraightenedTarget FromPrintedSize(IReadOnlyList<PointD> corners, double widthInches, double heightInches, double cornerPixels)
    {
        ArgumentNullException.ThrowIfNull(corners);
        var h = HomographyEstimate.Fit(corners, [new(0, 0), new(widthInches, 0), new(widthInches, heightInches), new(0, heightInches)])
            ?? throw new ArgumentException("the four corners do not make a rectangle's image", nameof(corners));
        double shortest = Sides(corners).Min();
        double sigma = Math.Max(1, cornerPixels);
        double u = 2 * Math.Sqrt(Math.Pow(Math.Sqrt(2) * sigma / shortest, 2) + Math.Pow(TypedInches / Math.Min(widthInches, heightInches), 2));
        return new StraightenedTarget(new HomographyPlane(h), ScaleSource.PrintedSize, widthInches, heightInches, u, Said(ScaleSource.PrintedSize, u,
            string.Create(CultureInfo.InvariantCulture, $"the printed size typed, {widthInches:0.###} by {heightInches:0.###} in, fitted to the four corners")));
    }

    /// <summary>
    /// (b) A GroupLab sheet on the same surface: its registration's plane, residual <paramref name="residualInches"/> over a sheet
    /// <paramref name="sheetInches"/> across, carried out to the target.
    /// </summary>
    public static StraightenedTarget FromGroupLabSheet(ITargetPlane sheet, IReadOnlyList<PointD> corners, double residualInches, double sheetInches, bool printerMeasured)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(corners);
        var plane = StraightenedTarget.Anchored(sheet, corners, out double w, out double ht);
        double reach = Math.Max(1, Math.Sqrt((w * w) + (ht * ht)) / sheetInches);
        // The print scale: a printer check measures it to about 0.1 percent; unmeasured, printers run a few percent small ("fit to page"
        // prints at 94 to 97 percent), taken as 1.5 percent either way.
        // Carried beyond the sheet, a fit's error grows with the square of the reach: the poster trial (grouplab poster-trial) found a 12 by
        // 18 in poster read from a letter sheet beside it wrong by 0.3 percent (0.55 at most) where the residual alone promised a tenth of
        // that, so the residual is taken as at least 0.01 in.
        double registration = Math.Sqrt(2) * reach * reach * Math.Max(residualInches, 0.01) / sheetInches, print = printerMeasured ? 0.001 : 0.015;
        double u = 2 * Math.Sqrt((registration * registration) + (print * print));
        string printer = printerMeasured ? "" : " " + UnmeasuredPrinter;
        return new StraightenedTarget(plane, ScaleSource.GroupLabSheet, w, ht, u, Said(ScaleSource.GroupLabSheet, u,
            "a GroupLab sheet in the same photograph, read by its markers, which also corrects the lens") + printer);
    }

    /// <summary>
    /// (c) Two tapped points <paramref name="inches"/> apart: the four corners straighten the target with the shape the camera geometry gives
    /// them, and the two points set its size.
    /// </summary>
    public static StraightenedTarget FromTwoPoints(IReadOnlyList<PointD> corners, PointD a, PointD b, double inches, int width, int height, ImageMetadata? metadata)
    {
        ArgumentNullException.ThrowIfNull(corners);
        var unit = HomographyEstimate.Fit([new(0, 0), new(1, 0), new(1, 1), new(0, 1)], corners)
            ?? throw new ArgumentException("the four corners do not make a rectangle's image", nameof(corners));
        var angle = CameraGeometry.Measure(unit, width, height, metadata);
        double aspect = angle.Aspect;
        var shaped = HomographyEstimate.Fit(corners, [new(0, 0), new(aspect, 0), new(aspect, 1), new(0, 1)])!;
        double measured = StraightenedTarget.Distance(shaped.Apply(a), shaped.Apply(b));
        double k = inches / measured;
        var h = Homography.Compose(shaped, new Homography([k, 0, 0, 0, k, 0, 0, 0, 1]));
        double pixels = StraightenedTarget.Distance(a, b);
        double taps = Math.Sqrt(2) * TapPixels / pixels;

        // The shape the corners give depends on the focal length, by about the square of the angle's tangent times the focal length's own
        // error: a percent from the file's equivalent, three solved from the outline, ten assumed (phones' main cameras run 23 to 28 mm).
        double focal = angle.Focal switch { FocalSource.Camera => 0.01, FocalSource.Solved => 0.03, _ => 0.1 };
        double shape = Math.Pow(Math.Tan(angle.Degrees * Math.PI / 180), 2) * focal;
        double u = 2 * Math.Sqrt((taps * taps) + Math.Pow(TypedInches / inches, 2) + (shape * shape));
        return new StraightenedTarget(new HomographyPlane(h), ScaleSource.TwoPoints, aspect * k, k, u, Said(ScaleSource.TwoPoints, u,
            string.Create(CultureInfo.InvariantCulture, $"two points {inches:0.###} in apart, the corners taking out the angle ({angle.Degrees:0} degrees, the shape from {CameraGeometry.Describe(angle.Focal)})")));
    }

    private static IEnumerable<double> Sides(IReadOnlyList<PointD> c) =>
        Enumerable.Range(0, 4).Select(i => StraightenedTarget.Distance(c[i], c[(i + 1) % 4]));

    private static string Said(ScaleSource source, double u, string what) => string.Create(CultureInfo.InvariantCulture,
        $"Scale from {what}: good to about {100 * u:0.##} percent.");
}

/// <summary>
/// Entry 344 section 3: one made reference, the small file Alan exports and Code adds to the library: the fingerprint, the product's name and
/// printed size, its bulls, its family, and the scale source with its uncertainty. Never the photograph.
/// </summary>
public sealed record TargetReference(StoreTarget Target, TargetFingerprint Fingerprint, ScaleSource Source, double Uncertainty, string Says)
{
    /// <summary>The format's name, written first in every file.</summary>
    public const string Format = "grouplab-target-reference-1";

    public JsonObject ToJson() => new()
    {
        ["format"] = Format,
        ["id"] = Target.Id,
        ["maker"] = Target.Maker,
        ["name"] = Target.Name,
        ["size"] = Target.Size,
        ["catalog"] = Target.Catalogue,
        ["family"] = Target.Family,
        ["scaleSource"] = Source.ToString(),
        ["scaleUncertainty"] = Math.Round(Uncertainty, 5),
        ["says"] = Says,
        ["fingerprint"] = Convert.ToBase64String(Fingerprint.ToBytes()),
    };

    public string Write() => ToJson().ToJsonString(new JsonSerializerOptions { WriteIndented = true });

    /// <summary>Reads a reference; throws <see cref="InvalidDataException"/> where it is not one.</summary>
    public static TargetReference Read(JsonNode? node)
    {
        try
        {
            return ReadUnchecked(node);
        }
        catch (Exception e) when (e is InvalidOperationException or FormatException or ArgumentException or OverflowException or IOException or InvalidCastException)
        {
            throw new InvalidDataException("not a GroupLab target reference GroupLab can read: it is damaged or another kind of file", e);
        }
    }

    /// <summary>NOTES-FROM-PLANNING.md entry 352 item 3: the largest reference read, far beyond one fingerprint and its words.</summary>
    public const int MostChars = 32 * 1024 * 1024;

    private static TargetReference ReadUnchecked(JsonNode? node)
    {
        if (node is not JsonObject o || (string?)o["format"] != Format)
        {
            throw new InvalidDataException("not a GroupLab target reference");
        }

        string id = (string?)o["id"] ?? throw new InvalidDataException("a reference with no id");
        using var bytes = new MemoryStream(Convert.FromBase64String((string?)o["fingerprint"] ?? ""));
        var fingerprint = TargetFingerprint.Read(bytes);
        if (fingerprint.Product != id)
        {
            throw new InvalidDataException("the fingerprint inside is another product's");
        }

        var target = new StoreTarget(id, (string?)o["maker"] ?? "", (string?)o["name"] ?? id, (string?)o["size"] ?? "", (string?)o["catalog"] ?? "", (string?)o["family"]);
        return new TargetReference(target, fingerprint, Enum.TryParse<ScaleSource>((string?)o["scaleSource"], out var s) ? s : ScaleSource.Scan,
            (double?)o["scaleUncertainty"] ?? 0, (string?)o["says"] ?? "");
    }

    public static TargetReference Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > MostChars)
        {
            throw new InvalidDataException("not a GroupLab target reference: it is far larger than any reference");
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException("not a GroupLab target reference: it is not the JSON a reference is written in", e);
        }

        return Read(node);
    }
}
