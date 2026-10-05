using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.ScaleMarkers;
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
    private List<PointD>? turnedBulls;

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

    /// <summary>Entry 365: the scale markers found in the photo, and what was said of any that could not be used.</summary>
    public MarkerFinding? Markers { get; private set; }

    public string? MarkersSaid { get; private set; }

    /// <summary>A bank card found in the photo, already blanked out of every copy here (entry 365 section D).</summary>
    public CardSighting? Card { get; private set; }

    /// <summary>The printer check that printed markers are corrected by, and the boards measured: set by the screen before a photo is loaded.</summary>
    public PrinterProfile? Printer { get; set; }

    public IReadOnlyList<ScaleBoard> Boards { get; set; } = [];

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
    /// Why the corners were not found, in a sentence for the screen, or null where they were (entry 362 section 2); and every method tried,
    /// for Show work.
    /// </summary>
    public string? CornersSaid { get; private set; }

    public IReadOnlyList<string> CornersTried { get; private set; } = [];

    /// <summary>
    /// Reads the photograph upright, as its orientation tag says it reads (entry 362 section 1), and looks for the target's four corners;
    /// where they are not found, the best guess at them is offered to be dragged onto them. A sentence where the file is not a picture, else null.
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

            // Every copy turned once, the same way, before anything else: the corners, the picture shown, the bulls and the fingerprint
            // all work on the target as it reads.
            using var stored = new Mat();
            Cv2.Resize(full, stored, new Size(g.Width, g.Height), 0, 0, InterpolationFlags.Area);
            var c = UprightMat.Apply(stored, m.Orientation);
            Forget();
            (grey, value, metadata, colour) = (Upright.Apply(g, m.Orientation), Upright.Apply(v, m.Orientation), m, c);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or OpenCVException or UnauthorizedAccessException)
        {
            return "That file is not a picture GroupLab can read.";
        }

        Width = grey.Width;
        Height = grey.Height;
        (Shown, ShownScale) = Encode(colour);
        FindCorners();
        ReadMarkers(choose: true);
        turnedBulls = null;
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

    /// <summary>The corners looked for on the picture as it now stands; where they are not found, the best guess.</summary>
    private void FindCorners(Mat? picture = null)
    {
        // A flatbed scan states its resolution and names no camera; a photograph with its details stripped, as some apps send it, does neither.
        var found = StoreTargetOutline.Find(picture ?? colour!, scan: metadata is { IsCamera: false, DpiX: >= 150 });
        Corners = found.Corners;
        CornersFound = found.Found;
        CornersSaid = found.Found ? null : found.Said;
        CornersTried = found.Tried;
        cornerPixels = found.Found ? 2 : 3;
    }

    /// <summary>
    /// Entry 365: the markers and any card in the photo. A card is blanked out of the picture, its grey copies and the one shown before anything
    /// else uses them; four brackets give the target's corners, so the corner finder's are replaced; and where markers were found they are the
    /// scale chosen, as the Size step says, when <paramref name="choose"/>.
    /// </summary>
    private void ReadMarkers(bool choose)
    {
        if (grey is null || value is null || colour is null)
        {
            return;
        }

        var (finding, said, sighting) = ScaleMarkerFinder.Read(grey, colour, Printer, Boards, value);
        Markers = finding;
        MarkersSaid = said;
        Card = sighting.Card;
        if (sighting.Blanked is not null)
        {
            (Shown, ShownScale) = Encode(colour);
        }

        if (finding?.NearCorners is not null)
        {
            // Entry 375: the target's own corners, from its paper edges between the brackets, in the codes' plane; never the cut L's corners.
            var (corners, found) = ScaleMarkerFinder.CornersNearBrackets(colour, finding);
            Corners = [.. corners];
            CornersFound = found;
            CornersSaid = found ? null : ScaleMarkerWords.BracketCornersNotSure;
            cornerPixels = found ? 2 : 3;
        }
        else if (finding is not null && finding.Used.Any(u => u.Kind is MarkerKind.InchBar or MarkerKind.MetricBar))
        {
            // A bar along the target's edge is not part of it: the corners are looked for again with the bars painted out.
            using var painted = colour.Clone();
            ScaleMarkerFinder.PaintOverBars(painted, finding);
            FindCorners(painted);
        }

        // Printed markers or a board are chosen by themselves; a card only by the person, since a box printed on a target can look like one.
        if (choose && finding is not null && finding.Used.Any(u => u.Kind != MarkerKind.Card))
        {
            Source = ScaleSource.Markers;
        }
    }

    /// <summary>The card alone, for the Size step's own choice of it.</summary>
    private MarkerFinding? CardOnly => Card is null ? null : ScaleMarkerReading.Read([], Card, null, []).Finding;

    /// <summary>
    /// Entry 362 section 5: the picture turned a quarter turn, for a photo whose orientation tag is missing or wrong or a target photographed
    /// sideways. The two ends of a measured length and any bulls already placed turn with it, and the corners are looked for again on the
    /// turned picture; where they are not found, the ones there were turn with it. The fingerprint, size and bulls are then made from the
    /// target as it reads upright, so the same target photographed any way round is recognized.
    /// </summary>
    public void Rotate(bool clockwise)
    {
        if (colour is null || grey is null || value is null)
        {
            return;
        }

        double w = Width, h = Height;
        var turned = UprightMat.Turn(colour, clockwise);
        colour.Dispose();
        colour = turned;
        grey = Upright.Turn(grey, clockwise);
        value = Upright.Turn(value, clockwise);
        Width = grey.Width;
        Height = grey.Height;
        (Shown, ShownScale) = Encode(colour);
        var before = Corners.Select(p => Upright.Turn(p, clockwise, w, h)).ToArray();
        FindCorners();
        if (!CornersFound && before.Length == 4)
        {
            // Turned, they are still clockwise but start from another corner: begin again from the one now at the top left.
            int first = Enumerable.Range(0, 4).MinBy(i => before[i].X + before[i].Y);
            Corners = [.. Enumerable.Range(0, 4).Select(i => before[(first + i) % 4])];
        }

        ReadMarkers(choose: false);
        PointA = PointA is { } a ? Upright.Turn(a, clockwise, w, h) : null;
        PointB = PointB is { } b ? Upright.Turn(b, clockwise, w, h) : null;
        Sheet = null;
        SheetTried = false;
        if (Target is { } t)
        {
            // Kept for the next straightening, which would otherwise look for the bulls afresh and lose the ones placed by hand.
            turnedBulls = [.. Bulls.Select(p => Upright.TurnInches(p, clockwise, t.WidthInches, t.HeightInches))];
            Bulls.Clear();
            Bulls.AddRange(turnedBulls);
            (WidthInches, HeightInches) = (HeightInches, WidthInches);
        }
        else if (WidthInches is not null || HeightInches is not null)
        {
            (WidthInches, HeightInches) = (HeightInches, WidthInches);
        }

        Target = null;
        StraightShown = null;
        straight?.Dispose();
        straight = null;
        Family = null;
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
        LastSnap = null;
    }

    /// <summary>The corner last snapped and where it was let go, for Undo; null when there is nothing to undo.</summary>
    public (int Corner, PointD From)? LastSnap { get; private set; }

    /// <summary>
    /// Entry 362 section 3: a corner let go is moved to the strongest corner in the picture within a short distance of it, a hundredth of the
    /// picture's longer side, and only where one clearly stands out there: at least four times the typical corner strength round it, and
    /// twice any other within the same distance. True where it moved; <see cref="UndoSnap"/> puts it back.
    /// </summary>
    public bool SnapCorner(int corner)
    {
        LastSnap = null;
        if (grey is null || corner < 0 || corner >= Corners.Length)
        {
            return false;
        }

        var at = Corners[corner];
        int reach = Math.Max(6, (int)Math.Round(0.01 * Math.Max(Width, Height)));
        int x0 = (int)Math.Round(at.X) - (2 * reach), y0 = (int)Math.Round(at.Y) - (2 * reach), size = (4 * reach) + 1;
        if (x0 < 0 || y0 < 0 || x0 + size > Width || y0 + size > Height)
        {
            return false;
        }

        using var whole = new Mat(Height, Width, MatType.CV_8UC1);
        whole.SetArray(grey.Pixels);
        using var crop = new Mat(whole, new Rect(x0, y0, size, size)).Clone();
        using var strength = new Mat();
        Cv2.CornerMinEigenVal(crop, strength, 5, 3);
        strength.GetArray(out float[] values);
        var sorted = (float[])values.Clone();
        Array.Sort(sorted);
        double typical = sorted[sorted.Length / 2];

        // The strongest within reach of where it was let go, and the strongest of the rest of that disc away from it.
        int best = -1, centre = 2 * reach;
        for (int i = 0; i < values.Length; i++)
        {
            int dx = (i % size) - centre, dy = (i / size) - centre;
            if ((dx * dx) + (dy * dy) <= reach * reach && (best < 0 || values[i] > values[best]))
            {
                best = i;
            }
        }

        if (best < 0)
        {
            return false;
        }

        int bx = best % size, by = best / size;
        double rival = 0;
        for (int i = 0; i < values.Length; i++)
        {
            int dx = (i % size) - centre, dy = (i / size) - centre, ox = (i % size) - bx, oy = (i / size) - by;
            if ((dx * dx) + (dy * dy) <= reach * reach && (ox * ox) + (oy * oy) > 9)
            {
                rival = Math.Max(rival, values[i]);
            }
        }

        double strongest = values[best];
        if (strongest < 4 * Math.Max(typical, 1e-12) || strongest < 2 * rival)
        {
            return false;
        }

        var refined = new[] { new Point2f(bx, by) };
        refined = Cv2.CornerSubPix(crop, refined, new Size(3, 3), new Size(-1, -1), new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.01));
        var to = new PointD(x0 + refined[0].X, y0 + refined[0].Y);
        if (Apart(to, at) < 0.5)
        {
            return false;
        }

        LastSnap = (corner, at);
        Corners[corner] = to;
        return true;
    }

    /// <summary>The last snap undone: the corner back where it was let go.</summary>
    public void UndoSnap()
    {
        if (LastSnap is { } snap && snap.Corner < Corners.Length)
        {
            Corners[snap.Corner] = snap.From;
        }

        LastSnap = null;
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
            ScaleSource.Markers => Markers is null ? MarkersSaid ?? ScaleMarkerWords.NeedMarkers : null,
            ScaleSource.Card => Card is null ? ScaleMarkerWords.NeedCard : null,
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
                ScaleSource.Markers => TargetStraightening.FromMarkers(Markers!, Corners, Width, Height, metadata),
                ScaleSource.Card => TargetStraightening.FromMarkers(CardOnly!, Corners, Width, Height, metadata),
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
        // Numbered as they are read: by rows half an inch deep, top first, then left to right. Bulls turned with the picture are kept.
        // Entry 363 section 3.1: the red finder's marks, and the aiming marks drawn as shapes round a point that it does not see.
        Bulls.AddRange(((IEnumerable<PointD>?)turnedBulls ?? AimMarks.Merge(StoreFingerprintBuilder.Bulls(picture, dpi), AimMarks.Find(picture, dpi))).OrderBy(b => Math.Round(b.Y * 2)).ThenBy(b => b.X));
        turnedBulls = null;
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
