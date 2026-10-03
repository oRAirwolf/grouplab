using System.Globalization;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Registration;

/// <summary>
/// "Try again, reading harder", NOTES-FROM-PLANNING.md entry 356 section 6: what the first reading does not, in order, each step stopping at
/// the first code that names a sheet.
/// <list type="number">
/// <item>The whole picture at full resolution and larger, with the working-size cap lifted to <see cref="HarderMostPixels"/>, a memory
/// limit rather than a time one: the first reading skips a doubling that would pass <see cref="SheetIdentification.MaximumWorkingSide"/>.</item>
/// <item>Each code on its own, where the picture shows one (located with or without decoding) and in each corner of the picture: lit evenly
/// by local contrast normalization against glare and shade, at <see cref="Thresholds"/>, a located code squared from its own four corners,
/// which flattens a curled corner's code, and read at each of <see cref="Enlargements"/>.</item>
/// <item>The sheet's layout used to find its codes: where the markers say each code must be, turned square on, is read as item 2 reads a code,
/// lit evenly and cut and enlarged; and the codes read are put together, so a share of a code that is one of several joins the others.</item>
/// <item>The printed name: among the sheets whose markers fit the picture's, the one whose printed identifier and title match the picture's,
/// drawn as the sheet prints them, clearly better than the rest.</item>
/// </list>
/// A second decoder library was looked for: OpenCV's WeChat reader with its neural network models is Apache 2.0 and could ship, but its four
/// model files are not in the repository and would be fetched from the network, so it is not used (entry 356, section 6 item 5).
/// </summary>
public static partial class SheetIdentification
{
    /// <summary>The most pixels a harder reading resamples a picture to: about 100 MB as one byte a pixel, twice that held at once.</summary>
    public const long HarderMostPixels = 100_000_000;

    /// <summary>The enlargements a code on its own is read at.</summary>
    public static IReadOnlyList<double> Enlargements { get; } = [1.0, 2.0, 3.0, 4.0];

    /// <summary>The levels, around the local mean, a code lit evenly is cut at: darker, the mean itself, lighter; and as it is, unthresholded.</summary>
    public static IReadOnlyList<int?> Thresholds { get; } = [null, 128, 100, 156];

    /// <summary>A step of the harder reading, as the screen names it while it runs.</summary>
    public static IReadOnlyList<string> HarderSteps { get; } =
    [
        "Reading the whole picture at full size and larger",
        "Reading each code on its own, lit evenly and squared",
        "Putting the codes read together",
        "Looking for the sheet's printed name",
    ];

    /// <summary>
    /// The harder reading. <paramref name="step"/> is told each step's index as it starts; <paramref name="cancellation"/> is checked before
    /// every read. Returns the sheet as <see cref="Identify"/> does, with <see cref="SheetIdentity.ByPrintedName"/> where the name found it.
    /// </summary>
    public static SheetIdentity IdentifyHarder(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend, TraceRecorder trace,
        Action<int>? step = null, CancellationToken cancellation = default, long mostPixels = HarderMostPixels)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(trace);
        var inv = CultureInfo.InvariantCulture;
        using var stage = trace.Begin("S0.identify-harder");
        var payloads = new List<byte[]>();
        int read = 0;
        string? unknown = null;

        SheetIdentity? Try(string how)
        {
            var frames = payloads.Select(p => GltdBinary.Decode([p])).Where(d => d.DefinitionId is not null).ToList();

            // Item 3: shares of one code that is one of several decode only together.
            if (frames.Count == 0 && payloads.Count > 1 && GltdBinary.Decode(payloads) is { DefinitionId: not null } together)
            {
                frames.Add(together);
            }

            var ids = frames.Select(f => f.DefinitionId!).Distinct(StringComparer.Ordinal).ToList();
            if (ids.Count != 1)
            {
                return null;
            }

            var tiles = frames.Select(f => (int)f.TileIndex).Distinct().ToList();
            if (tiles.Count != 1)
            {
                return null;
            }

            if (Named(candidates, frames, ids[0]) is not { } named)
            {
                // The codes read, and name a sheet nobody here has: said plainly at the end, unless the printed name finds it.
                unknown = ids[0];
                return null;
            }

            stage.Parameter("definition", ids[0]);
            stage.Metric("codes decoded", frames.Count, "count");
            stage.Done(StageStatus.Ok, string.Create(inv, $"{ids[0]}, {how}{FromCodesWords(named.FromItsCodes)}"));
            return new SheetIdentity(named.Definition, ids[0], tiles[0], read, null) { FromItsCodes = named.FromItsCodes, ReadHarder = true };
        }

        // 1. The whole picture, full size and larger, within the memory limit.
        step?.Invoke(0);
        foreach (double scale in new[] { 1.0, 1.5, 2.0 })
        {
            cancellation.ThrowIfCancellationRequested();
            if ((long)(image.Width * scale) * (long)(image.Height * scale) > mostPixels)
            {
                stage.Detail(string.Create(inv, $"at {scale:0.#} times: skipped, more than {mostPixels / 1_000_000} megapixels"));
                continue;
            }

            var found = backend.ReadCodes(image, scale);
            read += found.Count;
            payloads.AddRange(found);
            stage.Detail(string.Create(inv, $"at {scale:0.#} times the whole picture: {found.Count} codes read"));
            if (Try(string.Create(inv, $"read at {scale:0.#} times full resolution")) is { } whole)
            {
                return whole;
            }
        }

        // 2. Each code on its own: located codes squared from their corners, and the picture's four corners, lit evenly, cut at several levels,
        // enlarged.
        step?.Invoke(1);
        cancellation.ThrowIfCancellationRequested();
        var located = backend.LocateCodes(image);
        stage.Detail(string.Create(inv, $"{located.Count} codes located"));
        var views = new List<GrayImage>();
        foreach (var box in located)
        {
            if (Squared(image, box) is { } squared)
            {
                views.Add(squared);
            }
        }

        // Item 3: where the sheets the markers fit put their codes, each turned square on; the first reading read these once, as they came.
        var placed = Capture.LiveSheet.CodeViews(image, candidates, backend);
        stage.Detail(string.Create(inv, $"{placed.Count} codes placed by the markers"));
        foreach (var view in placed)
        {
            if (!views.Any(v => v.Width == view.Image.Width && v.Height == view.Image.Height && v.Pixels.AsSpan().SequenceEqual(view.Image.Pixels)))
            {
                views.Add(view.Image);
            }
        }

        int corner = Math.Min(image.Width, image.Height) / 3;
        foreach (var (x, y) in new[] { (0, 0), (image.Width - corner, 0), (0, image.Height - corner), (image.Width - corner, image.Height - corner) })
        {
            views.Add(Crop(image, x, y, corner, corner));
        }

        foreach (var view in views)
        {
            var even = EvenlyLit(view);
            foreach (int? level in Thresholds)
            {
                var cut = level is { } l ? Cut(even, l) : even;
                foreach (double enlarge in Enlargements)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if ((long)(cut.Width * enlarge) * (long)(cut.Height * enlarge) > mostPixels / 4)
                    {
                        continue;
                    }

                    var found = backend.ReadCutOut(cut, enlarge);
                    if (found.Count == 0)
                    {
                        continue;
                    }

                    read += found.Count;
                    payloads.AddRange(found);
                    if (Try("from a code on its own, lit evenly") is { } single)
                    {
                        return single;
                    }

                    break;
                }
            }
        }

        // 3. Together: every share read so far.
        step?.Invoke(2);
        stage.Detail(string.Create(inv, $"{payloads.Count} payloads read in all"));
        if (Try("from the codes read together") is { } joined)
        {
            return joined;
        }

        // 4. The printed name, among the sheets the markers fit.
        step?.Invoke(3);
        cancellation.ThrowIfCancellationRequested();
        if (Capture.LiveSheet.ByPrintedName(image, candidates, backend) is { } byName)
        {
            string id = GltdBinary.Encode(byName).Encoding?.DefinitionId ?? byName.Name;
            stage.Parameter("definition", id);
            stage.Done(StageStatus.Ok, $"{id}, by its printed name, the codes still unread");
            return new SheetIdentity(byName, id, 0, read, null) { ReadHarder = true, ByPrintedName = true };
        }

        if (unknown is not null)
        {
            return Failed(stage, unknown, read, $"the codes read, and name {unknown}, a sheet GroupLab does not have");
        }

        return Failed(stage, null, read, read == 0 ? "no code on the sheet could be read, even reading harder" : "no code on the sheet held a valid GroupLab frame, even reading harder");
    }

    /// <summary>A located code turned square on from its four corners, with a quiet margin round it; null where its corners make no square.</summary>
    private static GrayImage? Squared(GrayImage image, IReadOnlyList<PointD> box)
    {
        if (box.Count != 4)
        {
            return null;
        }

        double side = Enumerable.Range(0, 4).Max(i => Math.Sqrt(Math.Pow(box[i].X - box[(i + 1) % 4].X, 2) + Math.Pow(box[i].Y - box[(i + 1) % 4].Y, 2)));
        if (side < 16)
        {
            return null;
        }

        double margin = side / 6;
        int size = (int)Math.Ceiling(side + (2 * margin));
        PointD[] square = [new(margin, margin), new(margin + side, margin), new(margin + side, margin + side), new(margin, margin + side)];
        return HomographyEstimate.Fit([.. box], square) is { } toSquare ? PortableImaging.WarpPerspective(image, toSquare, size, size) : null;
    }

    private static GrayImage Crop(GrayImage image, int left, int top, int width, int height)
    {
        var pixels = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            Array.Copy(image.Pixels, ((top + y) * image.Width) + left, pixels, y * width, width);
        }

        return new GrayImage(width, height, pixels);
    }

    /// <summary>
    /// Lit evenly: each pixel set against the mean and spread of the pixels round it, a window an eighth of the view's shorter side, so a
    /// code half in glare and half in shade has one black and one white.
    /// </summary>
    internal static GrayImage EvenlyLit(GrayImage view)
    {
        int w = view.Width, h = view.Height, r = Math.Max(4, Math.Min(w, h) / 16);
        var sum = new long[(w + 1) * (h + 1)];
        var squares = new long[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            long row = 0, rowSquares = 0;
            for (int x = 0; x < w; x++)
            {
                int v = view.Pixels[(y * w) + x];
                row += v;
                rowSquares += v * v;
                sum[((y + 1) * (w + 1)) + x + 1] = sum[(y * (w + 1)) + x + 1] + row;
                squares[((y + 1) * (w + 1)) + x + 1] = squares[(y * (w + 1)) + x + 1] + rowSquares;
            }
        }

        var pixels = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            int y0 = Math.Max(0, y - r), y1 = Math.Min(h, y + r + 1);
            for (int x = 0; x < w; x++)
            {
                int x0 = Math.Max(0, x - r), x1 = Math.Min(w, x + r + 1);
                long n = (long)(x1 - x0) * (y1 - y0);
                long s = sum[(y1 * (w + 1)) + x1] - sum[(y0 * (w + 1)) + x1] - sum[(y1 * (w + 1)) + x0] + sum[(y0 * (w + 1)) + x0];
                long q = squares[(y1 * (w + 1)) + x1] - squares[(y0 * (w + 1)) + x1] - squares[(y1 * (w + 1)) + x0] + squares[(y0 * (w + 1)) + x0];
                double mean = (double)s / n, spread = Math.Sqrt(Math.Max(0, ((double)q / n) - (mean * mean)));
                pixels[(y * w) + x] = (byte)Math.Clamp(128 + (64 * (view.Pixels[(y * w) + x] - mean) / Math.Max(8, spread)), 0, 255);
            }
        }

        return new GrayImage(w, h, pixels);
    }

    private static GrayImage Cut(GrayImage view, int level) =>
        new(view.Width, view.Height, [.. view.Pixels.Select(p => p >= level ? (byte)255 : (byte)0)]);
}
