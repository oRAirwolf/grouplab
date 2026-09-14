using GroupLab.Core.Imaging;
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

    /// <summary>A sentence for the report: what the scale came from and what it assumes.</summary>
    public abstract string Description { get; }

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

    public override bool AssumesSquareOn => false;
}

/// <summary>A GroupLab sheet registered from its fiducials: the pipeline's own mapping from image pixels to page dmm.</summary>
public sealed record SheetReference(IPageMapping Mapping, string Summary) : ScaleReference
{
    public override PointD ToTarget(PointD image)
    {
        var page = Mapping.ToPage(image);
        return new PointD(page.X / 254, page.Y / 254);
    }

    public override string Description => "the sheet's own printed markers: " + Summary;

    public override bool AssumesSquareOn => false;
}
