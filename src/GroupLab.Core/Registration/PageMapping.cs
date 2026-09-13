using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// The fitted transform from image pixels to page dmm, DETECTION-PIPELINE.md stage S3. Pixel (u, v) is taken at (u, v),
/// the convention OpenCV reports marker corners in, so anything measured in the image and mapped through here agrees
/// with the registration about where a pixel is.
/// </summary>
public interface IPageMapping
{
    string Model { get; }

    PointD ToPage(PointD image);

    PointD ToImage(PointD page);

    /// <summary>Page dmm per image pixel at <paramref name="image"/>: the derivatives of page x and y by image x and y.</summary>
    (double XX, double XY, double YX, double YY) Jacobian(PointD image);
}

/// <summary>A scan's registration, DETECTION-PIPELINE.md stage S3: a homography.</summary>
public sealed class HomographyMapping : IPageMapping
{
    private readonly Homography _inverse;

    public HomographyMapping(Homography imageToPage)
    {
        ArgumentNullException.ThrowIfNull(imageToPage);
        ImageToPage = imageToPage;
        _inverse = imageToPage.Inverse();
    }

    public Homography ImageToPage { get; }

    public string Model => "homography";

    public PointD ToPage(PointD image) => ImageToPage.Apply(image);

    public PointD ToImage(PointD page) => _inverse.Apply(page);

    public (double XX, double XY, double YX, double YY) Jacobian(PointD image) => ImageToPage.Jacobian(image);
}

/// <summary>
/// A photograph's registration, DESIGN.md section 11 step 3: a homography after radial lens distortion is removed.
/// Image coordinates are normalised about the image centre by half the longer side, then undistorted as
/// <c>u' = u (1 + k1 r^2 + k2 r^4)</c>, and the homography takes <c>u'</c> to the page. The model runs from image to
/// page, the direction every measurement uses, so only <see cref="ToImage"/> needs an iterative inverse.
/// </summary>
public sealed class RadialHomographyMapping : IPageMapping
{
    private readonly Homography _inverse;

    public RadialHomographyMapping(double centreX, double centreY, double scale, double k1, double k2, Homography normalisedToPage)
    {
        ArgumentNullException.ThrowIfNull(normalisedToPage);
        CentreX = centreX;
        CentreY = centreY;
        Scale = scale;
        K1 = k1;
        K2 = k2;
        NormalisedToPage = normalisedToPage;
        _inverse = normalisedToPage.Inverse();
    }

    public double CentreX { get; }

    public double CentreY { get; }

    public double Scale { get; }

    public double K1 { get; }

    public double K2 { get; }

    public Homography NormalisedToPage { get; }

    public string Model => "homography with radial distortion";

    public PointD Undistort(PointD image)
    {
        double x = (image.X - CentreX) / Scale, y = (image.Y - CentreY) / Scale;
        double r2 = (x * x) + (y * y);
        double f = 1 + (K1 * r2) + (K2 * r2 * r2);
        return new PointD(x * f, y * f);
    }

    public PointD ToPage(PointD image) => NormalisedToPage.Apply(Undistort(image));

    /// <summary>Where the same homography would put <paramref name="image"/> with no distortion term: what the lens correction changed.</summary>
    public PointD WithoutDistortion(PointD image) =>
        NormalisedToPage.Apply(new PointD((image.X - CentreX) / Scale, (image.Y - CentreY) / Scale));

    public PointD ToImage(PointD page)
    {
        var q = _inverse.Apply(page);
        double rho = Math.Sqrt((q.X * q.X) + (q.Y * q.Y));
        if (rho == 0)
        {
            return new PointD(CentreX, CentreY);
        }

        double r = rho;
        for (int i = 0; i < 30; i++)
        {
            double r2 = r * r;
            double g = (r * (1 + (K1 * r2) + (K2 * r2 * r2))) - rho;
            double slope = 1 + (3 * K1 * r2) + (5 * K2 * r2 * r2);
            if (slope == 0)
            {
                break;
            }

            double next = r - (g / slope);
            bool converged = Math.Abs(next - r) < 1e-13;
            r = next;
            if (converged)
            {
                break;
            }
        }

        double k = r / rho;
        return new PointD(CentreX + (Scale * q.X * k), CentreY + (Scale * q.Y * k));
    }

    public (double XX, double XY, double YX, double YY) Jacobian(PointD image)
    {
        const double h = 0.5;
        var px = ToPage(new PointD(image.X + h, image.Y));
        var mx = ToPage(new PointD(image.X - h, image.Y));
        var py = ToPage(new PointD(image.X, image.Y + h));
        var my = ToPage(new PointD(image.X, image.Y - h));
        return ((px.X - mx.X) / (2 * h), (py.X - my.X) / (2 * h), (px.Y - mx.Y) / (2 * h), (py.Y - my.Y) / (2 * h));
    }
}
