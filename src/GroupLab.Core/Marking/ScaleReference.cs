using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Marking;

/// <summary>
/// How image pixels become inches at the target, NOTES-FROM-PLANNING.md entry 21 section 4. A result always says which kind it
/// used, because the kinds are not equally right: a single length is a uniform scale that silently absorbs perspective, a
/// rectangle removes perspective exactly, and a registered GroupLab sheet also models the lens.
/// </summary>
public abstract record ScaleReference
{
    /// <summary>The image point's position on the target plane, in inches, x right and y down as the image is.</summary>
    public abstract PointD ToTarget(PointD image);

    /// <summary>A sentence for the report: what the scale came from and what it assumes, in inches, as the export records it.</summary>
    public abstract string Description { get; }

    /// <summary>The same sentence with its lengths in the screen's units (NOTES-FROM-PLANNING.md entry 25 section 1).</summary>
    public abstract string Describe(UnitSettings units);

    /// <summary>True when the scale cannot see perspective, so a result from it should say so beside its figures.</summary>
    public abstract bool AssumesSquareOn { get; }

    /// <summary>
    /// True when the target axes are the stored image's axes, so "right" and "low" mean the image's as stored and the screen must turn
    /// them with the view (NOTES-FROM-PLANNING.md entry 26). A rectangle's axes are its own tapped sides, and a sheet's its page.
    /// </summary>
    public virtual bool AxesFollowImage => false;
}

/// <summary>
/// A known length between two tapped points: fast, and a uniform scale over the whole image. On an off-axis photograph that is
/// wrong by an amount that varies across the frame, and nothing in the figures shows it, which is why it declares the assumption.
/// </summary>
public sealed record LengthReference(PointD A, PointD B, double Inches) : ScaleReference
{
    private double InchesPerPixel => Inches / Math.Sqrt(((B.X - A.X) * (B.X - A.X)) + ((B.Y - A.Y) * (B.Y - A.Y)));

    public override PointD ToTarget(PointD image) => new(image.X * InchesPerPixel, image.Y * InchesPerPixel);

    public override string Description => string.Create(System.Globalization.CultureInfo.InvariantCulture,
        $"a single {Inches:0.###} in reference length, which assumes the photograph is square on and the sheet flat");

    public override string Describe(UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return $"a single {units.Length(Inches)} reference length, which assumes the photograph is square on and the sheet flat";
    }

    public override bool AssumesSquareOn => true;

    public override bool AxesFollowImage => true;
}

/// <summary>
/// A known rectangle, four tapped corners in order around it starting top left, and its size: four taps instead of two, and a
/// homography instead of a scale factor, the same mapping the automatic path fits from fiducials. It removes perspective exactly.
/// It cannot remove a bow in the sheet, because a homography is planar.
/// </summary>
public sealed record RectangleReference : ScaleReference
{
    private readonly HomographyMapping mapping;

    public RectangleReference(IReadOnlyList<PointD> corners, double widthInches, double heightInches)
    {
        ArgumentNullException.ThrowIfNull(corners);
        if (corners.Count != 4)
        {
            throw new ArgumentException("a rectangle needs its four corners.", nameof(corners));
        }

        Corners = [.. corners];
        WidthInches = widthInches;
        HeightInches = heightInches;
        PointD[] target = [new(0, 0), new(widthInches, 0), new(widthInches, heightInches), new(0, heightInches)];
        mapping = new HomographyMapping(HomographyEstimate.Fit(Corners, target) ?? throw new ArgumentException("the four corners do not make a rectangle's image.", nameof(corners)));
    }

    public IReadOnlyList<PointD> Corners { get; }

    public double WidthInches { get; }

    public double HeightInches { get; }

    public override PointD ToTarget(PointD image) => mapping.ToPage(image);

    public override string Description => string.Create(System.Globalization.CultureInfo.InvariantCulture,
        $"a {WidthInches:0.###} by {HeightInches:0.###} in reference rectangle, which removes perspective but assumes the sheet flat");

    public override string Describe(UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return $"a {units.Number(WidthInches)} by {units.Length(HeightInches)} reference rectangle, which removes perspective but assumes the sheet flat";
    }

    public override bool AssumesSquareOn => false;
}

/// <summary>
/// A GroupLab sheet registered from its fiducials: the pipeline's own mapping from image pixels to page dmm.
/// <para>
/// <b>On a scan, real inches; on a photograph, the sheet's own.</b> NOTES-FROM-PLANNING.md entry 171 section 1, answering question 49. A scan's
/// stated resolution is an absolute ruler, so it measures how large the sheet was printed, and every distance here is multiplied by that
/// <see cref="PrintScale"/>: a group on a sheet printed at 96.2 percent reads its true size. A photograph has no absolute ruler, so its
/// figures stay in the sheet's inches and the screen says so. The scale is applied uniformly, by area, because that is the one figure the
/// screen reports and a scanner's own x and y differ by more than a printer's.
/// </para>
/// </summary>
public sealed record SheetReference(IPageMapping Mapping, string Summary) : ScaleReference
{
    /// <summary>
    /// The lowest and highest print scale believed. Outside them the file's stated resolution is more likely wrong than the print, a scan
    /// resampled or a screenshot saying 96 DPI, so the figures stay in the sheet's own inches rather than being multiplied by nonsense. A
    /// "fit to page" dialog prints at 94 to 97 percent, well inside.
    /// </summary>
    public const double LowestBelievable = 0.85;

    public const double HighestBelievable = 1.15;

    public override PointD ToTarget(PointD image)
    {
        var page = Mapping.ToPage(image);
        double k = (PrintScale ?? 1) / 254;
        return new PointD(page.X * k, page.Y * k);
    }

    /// <summary>
    /// The measured print scale every distance is multiplied by, so the figures are real inches; null where they are the sheet's own inches,
    /// on a photograph or wherever the scale could not be believed.
    /// </summary>
    public double? PrintScale { get; init; }

    /// <summary>Whether this sheet's figures are real inches, which is what a saved or exported group records.</summary>
    public bool RealInches => PrintScale is not null;

    /// <summary>The print scale a measurement's figures should be multiplied by, or null to keep them in the sheet's own inches.</summary>
    public static double? Correction(ScaleReport? report) =>
        report?.Scale is { } s && s >= LowestBelievable && s <= HighestBelievable ? s : null;

    public override string Description => "the sheet's own printed markers: " + Summary;

    /// <summary>
    /// How many of the sheet's markers were found and how many it carries, for the one-line scale readout of NOTES-FROM-PLANNING.md entry 109
    /// section 2; null for a marking reopened from a file, which keeps only the summary.
    /// </summary>
    public int? MarkersFound { get; init; }

    public int? MarkersExpected { get; init; }

    public override string Describe(UnitSettings units) => Description;

    public override bool AssumesSquareOn => false;
}
