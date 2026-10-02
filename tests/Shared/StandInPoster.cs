using System.Globalization;
using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Tests.Support;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 348: a photograph of a store-bought target for the fingerprint steps' tests and the screenshot walk, drawn by
/// GroupLab and never a maker's artwork: a 12 by 18 in poster with a large bull in the middle and four small ones in its corners, each ringed,
/// with a red center in black, words and rings for features, laid on a dark board and photographed 18 degrees off square.
/// </summary>
internal static class StandInPoster
{
    public const double Width = 12, Height = 18;

    private const double Dpi = 100;

    /// <summary>The bulls' centers, inches from the poster's top left corner.</summary>
    public static IReadOnlyList<PointD> Bulls { get; } = [new(6, 8), new(2, 2.2), new(10, 2.2), new(2, 15.8), new(10, 15.8)];

    /// <summary>Writes the photograph to <paramref name="file"/> and gives the poster's four corners in it, top left first, clockwise.</summary>
    public static IReadOnlyList<PointD> Photograph(string file)
    {
        using var poster = new Mat((int)(Height * Dpi), (int)(Width * Dpi), MatType.CV_8UC3, new Scalar(225, 235, 240));
        int Px(double inches) => (int)Math.Round(inches * Dpi);
        Cv2.Rectangle(poster, new Rect(Px(0.3), Px(0.3), Px(Width - 0.6), Px(Height - 0.6)), Scalar.Black, Px(0.06));
        var rng = new Random(348);
        for (int i = 0; i < 40; i++)
        {
            var c = new Point(rng.Next(Px(1), Px(Width - 1)), rng.Next(Px(4), Px(Height - 4)));
            Cv2.Circle(poster, c, rng.Next(15, 60), new Scalar(rng.Next(0, 160), rng.Next(60, 200), rng.Next(0, 120)), rng.Next(2, 8), LineTypes.AntiAlias);
            Cv2.PutText(poster, "RANGE " + i.ToString(CultureInfo.InvariantCulture), new Point(rng.Next(Px(0.6), Px(Width - 3)), rng.Next(Px(3.5), Px(Height - 3))),
                HersheyFonts.HersheyDuplex, 0.9, Scalar.Black, 2, LineTypes.AntiAlias);
        }

        // Each bull: light rings about it, then a small black disc with a red center, so the poster's outline is still four straight sides
        // to the corner search and the red center is ringed by black ink to the bull finder.
        foreach (var (bull, i) in Bulls.Select((b, i) => (b, i)))
        {
            double r = i == 0 ? 2.6 : 0.9;
            var centre = new Point(Px(bull.X), Px(bull.Y));
            for (int ring = 1; ring <= 4; ring++)
            {
                Cv2.Circle(poster, centre, Px(r * ring / 4), new Scalar(205, 185, 165), 4, LineTypes.AntiAlias);
            }

            Cv2.Circle(poster, centre, Px(i == 0 ? 0.3 : 0.2), new Scalar(26, 23, 22), -1, LineTypes.AntiAlias);
            Cv2.Circle(poster, centre, Px(i == 0 ? 0.15 : 0.1), new Scalar(53, 74, 210), -1, LineTypes.AntiAlias);
        }

        // A camera 26 in from the board, turned 18 degrees about its upright axis, looking at the poster's middle.
        double a = 18 * Math.PI / 180, f = 2900, d = 26, cx = Width / 2, cy = Height / 2;
        double[] r3 = [Math.Cos(a), 0, Math.Sin(a), 0, 1, 0, -Math.Sin(a), 0, Math.Cos(a)];
        double[] t = [-(r3[0] * cx) - (r3[1] * cy), -(r3[3] * cx) - (r3[4] * cy), d - (r3[6] * cx) - (r3[7] * cy)];
        double[] k = [f, 0, 2000, 0, f, 1500, 0, 0, 1];
        double[] m = [r3[0], r3[1], t[0], r3[3], r3[4], t[1], r3[6], r3[7], t[2]];
        var h = new double[9];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                h[(i * 3) + j] = (k[i * 3] * m[j]) + (k[(i * 3) + 1] * m[3 + j]) + (k[(i * 3) + 2] * m[6 + j]);
            }
        }

        double[] toPicture = [h[0] / Dpi, h[1] / Dpi, h[2], h[3] / Dpi, h[4] / Dpi, h[5], h[6] / Dpi, h[7] / Dpi, h[8]];
        using var photo = new Mat(3000, 4000, MatType.CV_8UC3, new Scalar(52, 60, 70));
        using (var hm = Mat.FromPixelData(3, 3, MatType.CV_64FC1, toPicture))
        {
            Cv2.WarpPerspective(poster, photo, hm, photo.Size(), InterpolationFlags.Linear, BorderTypes.Transparent);
        }

        Cv2.ImWrite(file, photo, new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
        PointD Map(double x, double y)
        {
            double w = (h[6] * x) + (h[7] * y) + h[8];
            return new PointD(((h[0] * x) + (h[1] * y) + h[2]) / w, ((h[3] * x) + (h[4] * y) + h[5]) / w);
        }

        return [Map(0, 0), Map(Width, 0), Map(Width, Height), Map(0, Height)];
    }
}
