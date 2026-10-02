using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.StoreTargets;
using OpenCvSharp;

namespace GroupLab.Cli.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 348: one store-bought target being fingerprinted on a screen, a step at a time, on entry 344's engine. The
/// computer and the phone each draw it their own way and drive it the same way: the photo read and its corners found, the scale from the source
/// the person picks, the target straightened, its bulls confirmed, and the small reference file written. The photograph is read and let go:
/// what is kept is the fingerprint, the name, the size and the bulls (<see cref="TargetReference"/>).
/// </summary>
public sealed class FingerprintSession : IDisposable
{
    /// <summary>The longer side of the copies drawn on the screen, in pixels.</summary>
    public const int ShownSide = 1600;

    private readonly Func<IReadOnlyList<TargetDefinition>> sheets;
    private readonly double? mostMegapixels;
    private GrayImage? grey;
    private GrayImage? value;
    private ImageMetadata? metadata;
    private Mat? colour;
    private Mat? straight;
    private double cornerPixels = 3;

    /// <param name="sheets">The GroupLab sheets a sheet in the photograph is identified among.</param>
    /// <param name="mostMegapixels">The photograph is read at no more than this, as the phone reads a picture; null at its own size.</param>
    public FingerprintSession(Func<IReadOnlyList<TargetDefinition>> sheets, double? mostMegapixels = null)
    {
        this.sheets = sheets ?? throw new ArgumentNullException(nameof(sheets));
        this.mostMegapixels = mostMegapixels;
    }

    public FingerprintStep Step { get; private set; }

    /// <summary>The photograph's size as it is worked on, pixels.</summary>
    public int Width { get; private set; }

    public int Height { get; private set; }

    /// <summary>The photograph for the screen, a JPEG <see cref="ShownSide"/> on its longer side at most, and its pixels per photograph pixel.</summary>
    public byte[]? Shown { get; private set; }

    public double ShownScale { get; private set; } = 1;

    /// <summary>The target's four corners in the photograph's pixels: top left, top right, bottom right, bottom left.</summary>
    public PointD[] Corners { get; private set; } = [];

    /// <summary>Whether the corners were found by themselves, so the step says so rather than asking for all four.</summary>
    public bool CornersFound { get; private set; }

    public ScaleSource Source { get; set; } = ScaleSource.PrintedSize;

    /// <summary>The printed size typed, inches.</summary>
    public double? WidthInches { get; set; }

    public double? HeightInches { get; set; }

    /// <summary>The two ends of a measured length on the photograph, and how far apart they are, inches.</summary>
    public PointD? PointA { get; private set; }

    public PointD? PointB { get; private set; }

    public double? Distance { get; set; }

    /// <summary>A GroupLab sheet read in the photograph, null until it has been tried or where none was read.</summary>
    public (ITargetPlane Plane, double ResidualInches, double SheetInches)? Sheet { get; private set; }

    public bool SheetTried { get; private set; }

    /// <summary>The target straightened, once the corners are confirmed.</summary>
    public StraightenedTarget? Target { get; private set; }

    /// <summary>The straightened target for the screen, a JPEG, and its pixels per inch of the target.</summary>
    public byte[]? StraightShown { get; private set; }

    public double StraightShownScale { get; private set; } = 1;

    /// <summary>The bulls' centers, inches from the target's top left corner.</summary>
    public List<PointD> Bulls { get; } = [];

    public string Name { get; set; } = "";

    /// <summary>What the target has to do with the library already shipped, found when the bulls are confirmed.</summary>
    public FamilyFinding? Family { get; private set; }

    /// <summary>The reference last written, null before.</summary>
    public TargetReference? Reference { get; private set; }

    /// <summary>
    /// Reads the photograph and looks for the target's four corners; where they are not found, a rectangle inside the picture is offered
    /// to be dragged onto them. A sentence where the file is not a picture, else null.
    /// </summary>
    public string? Load(string path)
    {
        try
        {
            var (g, m) = ImageLoader.Load(path, mostMegapixels);
            var (v, _) = ImageLoader.LoadMaxChannel(path, mostMegapixels);
            using var full = Cv2.ImDecode(File.ReadAllBytes(path), ImreadModes.Color | ImreadModes.IgnoreOrientation);
            if (full.Empty())
            {
                return "That file is not a picture GroupLab can read.";
            }

            var c = new Mat();
            Cv2.Resize(full, c, new Size(g.Width, g.Height), 0, 0, InterpolationFlags.Area);
            Forget();
            (grey, value, metadata, colour) = (g, v, m, c);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or OpenCVException or UnauthorizedAccessException)
        {
            return "That file is not a picture GroupLab can read.";
        }

        Width = grey.Width;
        Height = grey.Height;
        (Shown, ShownScale) = Encode(colour);
        if (SheetOutline.Find(grey, out _) is { } quad)
        {
            Corners = [.. quad.Corners];
            CornersFound = true;
            cornerPixels = Math.Max(1.5, quad.EdgeRmsPixels);
        }
        else
        {
            double dx = Width * 0.12, dy = Height * 0.12;
            Corners = [new(dx, dy), new(Width - dx, dy), new(Width - dx, Height - dy), new(dx, Height - dy)];
            CornersFound = false;
            cornerPixels = 3;
        }

        PointA = PointB = null;
        Sheet = null;
        SheetTried = false;
        Target = null;
        StraightShown = null;
        Bulls.Clear();
        Family = null;
        Reference = null;
        return null;
    }

    /// <summary>A corner dragged to where the person put it, in the photograph's pixels, kept inside it.</summary>
    public void MoveCorner(int corner, PointD to)
    {
        if (corner < 0 || corner >= Corners.Length)
        {
            return;
        }

        Corners[corner] = new PointD(Math.Clamp(to.X, 0, Width - 1), Math.Clamp(to.Y, 0, Height - 1));
        cornerPixels = 3;
    }

    /// <summary>The nearest corner within <paramref name="reach"/> photograph pixels of <paramref name="at"/>, or null.</summary>
    public int? CornerAt(PointD at, double reach)
    {
        int? best = null;
        double nearest = reach;
        for (int i = 0; i < Corners.Length; i++)
        {
            double d = Apart(Corners[i], at);
            if (d <= nearest)
            {
                (best, nearest) = (i, d);
            }
        }

        return best;
    }

    /// <summary>A tap on the photograph while two points are the scale: the first end, then the second, then the first again.</summary>
    public void Place(PointD at)
    {
        if (PointA is null || PointB is not null)
        {
            (PointA, PointB) = (at, null);
        }
        else
        {
            PointB = at;
        }
    }

    /// <summary>How many of the two ends are placed.</summary>
    public int Placed => PointA is null ? 0 : PointB is null ? 1 : 2;

    /// <summary>Looks for a GroupLab sheet in the photograph (slow: it is a whole sheet's registration). A sentence either way.</summary>
    public string ReadSheet()
    {
        if (grey is null || value is null || metadata is null)
        {
            return FingerprintWords.NeedPhoto;
        }

        Sheet = TargetReferenceMaker.ReadGroupLabSheet(grey, value, metadata, sheets());
        SheetTried = true;
        return Sheet is null ? FingerprintWords.SheetNotRead : FingerprintWords.SheetRead;
    }

    /// <summary>What the step still needs before it can move on, or null when it has it.</summary>
    public string? Missing() => Step switch
    {
        FingerprintStep.Photo => Shown is null ? FingerprintWords.NeedPhoto : null,
        FingerprintStep.Scale => Source switch
        {
            ScaleSource.PrintedSize => WidthInches is > 0 && HeightInches is > 0 ? null : FingerprintWords.NeedSize,
            ScaleSource.GroupLabSheet => Sheet is null ? FingerprintWords.NeedSheet : null,
            _ => Placed == 2 && Distance is > 0 && Apart(PointA!.Value, PointB!.Value) >= 10 ? null : FingerprintWords.NeedPoints,
        },
        FingerprintStep.Bulls => Bulls.Count == 0 ? FingerprintWords.NeedBull : null,
        FingerprintStep.Send => string.IsNullOrWhiteSpace(Name) ? FingerprintWords.NeedName : null,
        _ => null,
    };

    /// <summary>
    /// Moves to the next step, doing that step's work: leaving Straighten straightens the target and finds its bulls; leaving the bulls checks
    /// it against the library. A sentence where it cannot, and it stays.
    /// </summary>
    public string? Next()
    {
        if (Missing() is { } missing)
        {
            return missing;
        }

        switch (Step)
        {
            case FingerprintStep.Straighten:
                if (Straighten() is { } refused)
                {
                    return refused;
                }

                break;
            case FingerprintStep.Bulls:
                Family = TargetReferenceMaker.CheckFamily(straight!, Dpi, StoreTargetLibrary.All);
                break;
            case FingerprintStep.Send:
                return null;
        }

        Step++;
        return null;
    }

    /// <summary>Back a step; nothing done on the way is forgotten.</summary>
    public void Back()
    {
        if (Step > FingerprintStep.Photo)
        {
            Step--;
        }
    }

    /// <summary>The straightened picture's pixels an inch.</summary>
    public double Dpi { get; private set; }

    private string? Straighten()
    {
        StraightenedTarget target;
        try
        {
            target = Source switch
            {
                ScaleSource.PrintedSize => TargetStraightening.FromPrintedSize(Corners, WidthInches!.Value, HeightInches!.Value, cornerPixels),
                ScaleSource.GroupLabSheet => TargetStraightening.FromGroupLabSheet(Sheet!.Value.Plane, Corners, Sheet.Value.ResidualInches, Sheet.Value.SheetInches, printerMeasured: false),
                _ => TargetStraightening.FromTwoPoints(Corners, PointA!.Value, PointB!.Value, Distance!.Value, Width, Height, metadata),
            };
        }
        catch (ArgumentException)
        {
            return FingerprintWords.NotATarget;
        }

        if (!(target.WidthInches is > 0.5 and < 200) || !(target.HeightInches is > 0.5 and < 200))
        {
            return FingerprintWords.NotATarget;
        }

        var (picture, dpi) = TargetReferenceMaker.Straighten(colour!, target);
        straight?.Dispose();
        straight = picture;
        Dpi = dpi;
        Target = target;
        (StraightShown, double scale) = Encode(picture);
        StraightShownScale = scale * dpi;
        Bulls.Clear();
        // Numbered as they are read: by rows half an inch deep, top first, then left to right.
        Bulls.AddRange(StoreFingerprintBuilder.Bulls(picture, dpi).OrderBy(b => Math.Round(b.Y * 2)).ThenBy(b => b.X));
        Family = null;
        return null;
    }

    /// <summary>A ring's radius on the screen, inches: a quarter inch, or more on a large target so it can be hit.</summary>
    public double RingInches => Target is null ? 0.25 : Math.Max(0.25, 0.03 * Math.Min(Target.WidthInches, Target.HeightInches));

    /// <summary>The bull whose ring is under <paramref name="inches"/>, or null.</summary>
    public int? BullAt(PointD inches)
    {
        for (int i = 0; i < Bulls.Count; i++)
        {
            if (Apart(Bulls[i], inches) <= RingInches * 1.4)
            {
                return i;
            }
        }

        return null;
    }

    public void RemoveBull(int bull)
    {
        if (bull >= 0 && bull < Bulls.Count)
        {
            Bulls.RemoveAt(bull);
        }
    }

    /// <summary>A bull added where the person put it, inside the target.</summary>
    public void AddBull(PointD inches)
    {
        if (Target is { } t && inches.X >= 0 && inches.Y >= 0 && inches.X <= t.WidthInches && inches.Y <= t.HeightInches)
        {
            Bulls.Add(inches);
        }
    }

    /// <summary>The printed size the straightened target measures, as the reference names it.</summary>
    public string Size => Target is { } t ? FingerprintWords.Size(t.WidthInches, t.HeightInches) : "";

    /// <summary>The file's suggested name.</summary>
    public string FileName => FingerprintWords.FileName(Name) + ".glref";

    /// <summary>Entry 348 section 3: the notice where the target matches a library artwork, at another size or at this one; null where it matches none.</summary>
    public string? FamilySaid => Family switch
    {
        null or { Existing: null } => null,
        { Duplicate: true } => $"This looks like {Family.Existing.ShortName}, already in GroupLab's library at this size, so a later GroupLab already recognizes it.",
        _ => string.Create(CultureInfo.InvariantCulture,
            $"This looks like {Family.Existing.ShortName} at {Family.SizeRatio:0.00} times its size. It joins that family, so when a picture cannot show which size it is, GroupLab asks."),
    };

    /// <summary>The fingerprint made, and the reference written to <paramref name="path"/>: never the photograph.</summary>
    public TargetReference Write(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (straight is null || Target is null)
        {
            throw new InvalidOperationException("the target has not been straightened");
        }

        string id = FingerprintWords.FileName(Name);
        var fingerprint = StoreFingerprintBuilder.Make(id, straight, Dpi, [.. Bulls]);
        var reference = new TargetReference(new StoreTarget(id, "", Name.Trim(), Size, "", Family?.Family), fingerprint, Target.Source, Target.Uncertainty, Target.Says);
        File.WriteAllText(path, reference.Write());
        Reference = reference;
        return reference;
    }

    private static double Apart(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

    private static (byte[] Jpeg, double Scale) Encode(Mat picture)
    {
        double scale = Math.Min(1, ShownSide / (double)Math.Max(picture.Width, picture.Height));
        using var small = new Mat();
        if (scale < 1)
        {
            Cv2.Resize(picture, small, new Size(0, 0), scale, scale, InterpolationFlags.Area);
        }
        else
        {
            picture.CopyTo(small);
        }

        Cv2.ImEncode(".jpg", small, out byte[] bytes, new ImageEncodingParam(ImwriteFlags.JpegQuality, 88));
        return (bytes, small.Width / (double)picture.Width);
    }

    private void Forget()
    {
        colour?.Dispose();
        straight?.Dispose();
        colour = null;
        straight = null;
        grey = value = null;
    }

    public void Dispose() => Forget();
}
