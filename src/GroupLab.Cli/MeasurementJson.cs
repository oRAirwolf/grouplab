using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;

namespace GroupLab.Cli;

/// <summary>
/// The structured form of a measurement: the stage records of DETECTION-PIPELINE.md section 6.1 and the results they
/// produced. Lengths are page dmm unless a name says otherwise.
/// </summary>
internal static class MeasurementJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string Serialize(string image, string definition, ImageMetadata metadata, SheetMeasurement m)
    {
        object? mapping = m.Registration?.Mapping switch
        {
            HomographyMapping h => new { model = h.Model, imageToPage = Matrix(h.ImageToPage) },
            RadialHomographyMapping r => new { model = r.Model, r.CentreX, r.CentreY, r.Scale, r.K1, r.K2, normalisedToPage = Matrix(r.NormalisedToPage) },
            SurfaceMapping surface => new { model = surface.Model, parameters = surface.Parameters },
            _ => null,
        };
        var document = new
        {
            image,
            definition,
            metadata,
            failure = m.Failure,
            tileIndex = m.Fiducials?.TileIndex,
            markersExpected = m.Fiducials?.Expected,
            markersMatched = m.Fiducials?.Matches.Count,
            registration = m.Registration is { } f
                ? new { mapping, f.Markers, corners = f.Corners.Count, f.Inliers, f.RmsResidual, f.MaxResidual, f.HomographyRmsResidual, residuals = f.Corners }
                : null,
            scale = m.Scale is { } s ? new { s.PixelsPerDmmX, s.PixelsPerDmmY, s.PixelsPerDmmArea, s.NominalDpi, s.ScaleX, s.ScaleY, s.Scale } : null,
            lens = m.Lens,
            worstError = m.WorstError,
            meanError = m.MeanError,
            bulls = m.Bulls.Select(b => new { label = b.Name, declared = b.Declared, recovered = b.Recovered, b.Dx, b.Dy, b.Error, b.Iterations, b.Threshold, b.InkSpread, b.EdgePoints, b.Failure }),
            trace = m.Trace,
        };
        return JsonSerializer.Serialize(document, Options);
    }

    private static double[] Matrix(Homography h) => [.. Enumerable.Range(0, 9).Select(i => h[i / 3, i % 3])];
}
