using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Marking;

/// <summary>A marking file GroupLab will not read, with the reason in words a person can act on.</summary>
public sealed class MarkingFileException(string message) : Exception(message);

/// <summary>
/// The marking file: GroupLab's own JSON record of a marking and its report, docs/PHASE1-BRIEF.md section 6 item 6, and not any other
/// application's format.
/// <para>
/// Version 2 records the convention its positions are in (NOTES-FROM-PLANNING.md entry 24 section 4 and entry 26 point 3): the stored
/// pixel frame, with the image's EXIF Orientation tag and the display rotation beside it, so reopening a file shows the sheet the way
/// it was left. It records each scale's values and not only its sentence (entry 24 section 7). A file whose convention is unknown is
/// refused. Version 1 is migrated: it was written from the same stored-pixel decode, so its positions stand, but it kept its scale
/// only as a sentence, so the scale has to be set again.
/// </para>
/// </summary>
public static class MarkingFile
{
    public const string Format = "grouplab-marking-2";
    public const string FormatVersion1 = "grouplab-marking-1";
    private const string FrameDescription = "x right and y down, in pixels, from the top left of the image file's stored pixel grid, with EXIF orientation not applied";

    /// <summary>
    /// Writes the marking and the units the screen showed. It no longer carries the screen's own hole-size flags: that check is gone, and the
    /// detector's flag rides on each shot as <see cref="MarkedShot.Oversize"/> (NOTES-FROM-PLANNING.md entry 87 section 1). Every value is canonical, lengths and the shot distance in inches, whatever the units; the units are recorded
    /// beside them so a reader can reproduce what was on screen (entry 25 section 1), and reading a file never changes them.
    /// </summary>
    public static string Write(MarkingState state, UnitSettings? displayUnits = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        var report = GroupAnalysis.Analyse(state);
        var labels = ShotLabels.For(state).ToDictionary(l => l.ShotId);
        var document = new
        {
            format = Format,
            image = state.ImagePath,
            imageFrame = new { name = ViewRotation.StoredPixelFrame, description = FrameDescription },
            exifOrientation = state.ExifOrientation,
            displayRotationDegrees = 90 * state.ViewQuarterTurns,
            calibre = state.Calibre is { } calibre ? new { name = calibre.Name, diameterInches = calibre.DiameterInches } : null,
            // NOTES-FROM-PLANNING.md entry 83 section 4: the review items a person chose to leave as they are, so a reopened marking does not ask again.
            reviewKept = (state.Dismissed ?? []).Order(StringComparer.Ordinal),
            // NOTES-FROM-PLANNING.md entry 80 section 5: what the detection ran with, which the calibre above need not match. Null when
            // nothing was detected; a detection without a calibre records that absence.
            detection = state.Detection is { } detection
                ? new
                {
                    calibre = detection.Calibre is { } used ? new { name = used.Name, diameterInches = used.DiameterInches } : null,
                    holeSizeInches = detection.HoleSizeInches,
                    description = detection.Describe(),
                }
                : null,
            // NOTES-FROM-PLANNING.md entry 157 section 3 item 5: what the photograph was taken with and how good it is, never where or when.
            capture = CaptureDocument(state.Capture),
            shotDistanceInches = state.ShotDistanceInches,
            displayUnits = displayUnits is { } units ? new { linear = units.Linear.ToString(), angular = units.Angular.ToString(), distance = units.Distance.ToString() } : null,
            scale = ScaleDocument(state.Scale),
            scaleDescription = report.Scale,
            scaleAssumesSquareOn = report.ScaleAssumesSquareOn,
            registration = state.RegistrationSummary,
            pointOfAim = state.PointOfAim,
            bulls = state.Bulls.Select(b => new { b.Index, b.Label, image = b.Image, b.Scoring }),
            // NOTES-FROM-PLANNING.md entry 94 section 2: which bulls hold which load. It lives in the session and never in the sheet, so it
            // has to survive here or a reopened marking loses the comparison the sheet was shot for.
            expectedShots = state.ExpectedShots,
            rifle = state.Rifle is { } rifle ? new { name = rifle.Name, clickValue = rifle.ClickValue, clickUnit = rifle.ClickUnit.ToString() } : null,
            barrel = state.Barrel,
            load = state.Load,
            // Entry 162 section 3.2: what the sheet was shot on, because what a hole measures depends on it.
            paper = state.Paper,
            backing = state.Backing,
            // Entry 113 section 4: how the sheet's shots are read against its bulls, when it is not one a bull.
            assignmentRule = state.Rule is { } rule
                ? new { nearestBull = rule.NearestOnly, perBull = rule.PerBull.OrderBy(p => p.Key).Select(p => new { bull = p.Key, shots = p.Value }) }
                : null,
            subgroups = state.Subgroups is { } map && !map.ByBull.IsEmpty
                ? map.ByBull.OrderBy(p => p.Key).Select(p => new { bull = p.Key, name = p.Value })
                : null,
            // NOTES-FROM-PLANNING.md entry 75: the name a person sees, the bull's, beside the id that keeps the file's identity.
            shots = state.Shots.Select(s => new
            {
                s.Id,
                label = labels[s.Id].Text,
                image = s.Image,
                targetInches = state.Scale?.ToTarget(s.Image),
                provenance = s.Provenance.ToString(),
                exclusion = s.Exclusion?.ToString(),
                s.NotAShot,
                s.Bull,
                s.BullChosen,
                s.MeasuredDiameterInches,
                // NOTES-FROM-PLANNING.md entry 131 section 2's marks, written only where they are set, so a marking made before the editor
                // existed and one made after it and left alone are the same file.
                flyer = s.Flyer ? true : (bool?)null,
                sighter = s.Sighter ? true : (bool?)null,
                s.ChosenDiameterInches,
                s.Note,
                size = s.Size is { } size
                    ? new
                    {
                        holes = size.Holes,
                        splitA = size.SplitA is { } sa ? new { x = sa.X, y = sa.Y } : null,
                        splitB = size.SplitB is { } sb ? new { x = sb.X, y = sb.Y } : null,
                    }
                    : null,
                oversize = s.Oversize is { } flag
                    ? new
                    {
                        holes = flag.Holes,
                        tentative = flag.Tentative,
                        splitA = flag.SplitA is { } a ? new { x = a.X, y = a.Y } : null,
                        splitB = flag.SplitB is { } b ? new { x = b.X, y = b.Y } : null,
                    }
                    : null,
            }),
            report,
        };
        return JsonSerializer.Serialize(document, Options);
    }

    /// <summary>Reads a marking file into a state, with notes for anything that could not be carried over. Throws <see cref="MarkingFileException"/> for a file it will not read.</summary>
    public static (MarkingState State, IReadOnlyList<string> Notes) Read(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new MarkingFileException("This is not a marking file: it is not valid JSON (" + ex.Message + ").");
        }

        if (root is not JsonObject file)
        {
            throw new MarkingFileException("This is not a marking file: it is not a JSON object.");
        }

        var notes = new List<string>();
        string? format = (string?)file["format"];
        ScaleReference? scale = null;
        int turns = 0;
        int? orientation = null;
        switch (format)
        {
            case Format:
                string? frame = (string?)file["imageFrame"]?["name"];
                if (frame != ViewRotation.StoredPixelFrame)
                {
                    throw new MarkingFileException($"This marking file's positions are in the image frame \"{frame ?? "(none)"}\", which this version of GroupLab does not know, so it cannot place its marks.");
                }

                orientation = (int?)file["exifOrientation"];
                int degrees = (int?)file["displayRotationDegrees"] ?? 0;
                if (degrees % 90 != 0)
                {
                    throw new MarkingFileException($"This marking file's display rotation, {degrees} degrees, is not a quarter turn.");
                }

                turns = ViewRotation.Normalise(degrees / 90);
                scale = ReadScale(file["scale"], notes);
                break;

            case FormatVersion1:
                notes.Add("This marking was saved by an earlier GroupLab. Its positions are in the stored pixel frame, which that version also used, so every mark stands. It recorded its scale only as a sentence, so set the scale again.");
                break;

            default:
                throw new MarkingFileException(format is null
                    ? "This is not a GroupLab marking file: it has no format."
                    : $"This marking file's format is \"{format}\", which this version of GroupLab does not read.");
        }

        var bulls = (file["bulls"] as JsonArray ?? []).Select(b => new BullAim((int)b!["index"]!, (string?)b["label"] ?? "", Point(b["image"])!.Value, (bool?)b["scoring"] ?? true)).ToImmutableList();
        var shots = (file["shots"] as JsonArray ?? []).Select(s => new MarkedShot(
            (int)s!["id"]!,
            Point(s["image"])!.Value,
            Enum.Parse<ShotProvenance>((string)s["provenance"]!),
            (string?)s["exclusion"] is { } reason ? Enum.Parse<ExclusionReason>(reason) : null,
            (bool?)s["notAShot"] ?? false,
            (int?)s["bull"],
            (bool?)s["bullChosen"] ?? false,
            (double?)s["measuredDiameterInches"],
            s["oversize"] is JsonObject flag
                ? new DetectedOversize((double)flag["holes"]!, (bool?)flag["tentative"] ?? false, Point(flag["splitA"]), Point(flag["splitB"]))
                : null,
            s["size"] is JsonObject size ? new MarkSize((double)size["holes"]!, Point(size["splitA"]), Point(size["splitB"])) : null)
        {
            Flyer = (bool?)s["flyer"] ?? false,
            Sighter = (bool?)s["sighter"] ?? false,
            ChosenDiameterInches = (double?)s["chosenDiameterInches"],
            Note = (string?)s["note"],
        }).ToImmutableList();
        var state = new MarkingState(
            (string?)file["image"],
            scale,
            Point(file["pointOfAim"]),
            bulls,
            shots,
            shots.Count == 0 ? 1 : shots.Max(s => s.Id) + 1,
            (string?)file["registration"],
            turns,
            orientation,
            // Entry 107 section 1: a marking saved under a calibre name keeps its diameter and is shown by it; the old name is not displayed.
            file["calibre"] is { } calibre ? Calibre.Of((double)calibre["diameterInches"]!) : null,
            (double?)file["shotDistanceInches"],
            Detection: file["detection"] is JsonObject detection
                ? new DetectionRecord(
                    detection["calibre"] is { } used ? Calibre.Of((double)used["diameterInches"]!) : null,
                    (double?)detection["holeSizeInches"])
                : null,
            Capture: file["capture"] is JsonObject capture && capture["quality"] is JsonObject quality
                ? new GroupLab.Core.Capture.CaptureRecord(
                    (string?)capture["lens"],
                    (double?)capture["focalLengthMm"],
                    (int?)capture["focalLength35mm"],
                    (double?)capture["offAxisDegrees"] ?? 0,
                    (string?)capture["focalSource"] ?? "",
                    (string?)capture["correction"] ?? "",
                    (double?)capture["k1"],
                    (double?)capture["k2"],
                    new GroupLab.Core.Capture.CaptureQuality(
                        (int?)quality["score"] ?? 0,
                        (string?)quality["words"] ?? "",
                        (double?)quality["blurInches"],
                        (double?)quality["focusPart"],
                        (double?)quality["clippedShare"],
                        (double?)quality["paperLevel"],
                        (double?)quality["exposurePart"],
                        (double?)capture["offAxisDegrees"] ?? 0,
                        (double?)quality["anglePart"] ?? 0,
                        (double?)quality["leastPixelsPerInch"] ?? 0,
                        (double?)quality["resolutionPart"] ?? 0,
                        (int?)quality["markingsRead"],
                        (int?)quality["markingsExpected"],
                        (double?)quality["markingsPart"]))
                : null,
            Dismissed: file["reviewKept"] is JsonArray kept ? [.. kept.Select(k => (string)k!)] : null,
            ExpectedShots: (int?)file["expectedShots"],
            Rifle: file["rifle"] is JsonObject r && Enum.TryParse<GroupLab.Core.Statistics.AngularUnit>((string?)r["clickUnit"], out var unit)
                ? new Rifle((string?)r["name"] ?? "", (double?)r["clickValue"] ?? 0.25, unit)
                : null,
            Barrel: (string?)file["barrel"],
            Load: (string?)file["load"],
            Paper: TargetMaterial.Paper((string?)file["paper"]),
            Backing: TargetMaterial.Backing((string?)file["backing"]),
            Subgroups: file["subgroups"] is JsonArray groups && groups.Count > 0
                ? new SubgroupMap(groups.ToImmutableDictionary(g => (int)g!["bull"]!, g => (string)g!["name"]!))
                : null,
            Rule: file["assignmentRule"] is JsonObject rule
                ? new AssignmentRule((bool?)rule["nearestBull"] ?? false, (rule["perBull"] as JsonArray ?? []).ToImmutableDictionary(p => (int)p!["bull"]!, p => (int)p!["shots"]!))
                : null);
        return (state, notes);
    }

    private static object? ScaleDocument(ScaleReference? scale) => scale switch
    {
        LengthReference length => new { kind = "length", a = length.A, b = length.B, inches = length.Inches },
        RectangleReference rectangle => new { kind = "rectangle", corners = rectangle.Corners, widthInches = rectangle.WidthInches, heightInches = rectangle.HeightInches },
        SheetReference sheet => new { kind = "sheet", summary = sheet.Summary, markersFound = sheet.MarkersFound, markersExpected = sheet.MarkersExpected, inches = sheet.RealInches ? "real" : "sheet", printScale = sheet.PrintScale, mapping = MappingDocument(sheet.Mapping) },
        _ => null,
    };

    /// <summary>
    /// A sheet's registration as numbers, NOTES-FROM-PLANNING.md entry 112 section 1: a session must reopen and read with no image, so the
    /// mapping from image pixels to the page is kept exactly, as the parameters each of the three models is built from. A marking without it,
    /// from before entry 112, still reads, with the note that the scale must be detected again.
    /// </summary>
    private static JsonObject? MappingDocument(IPageMapping mapping) => mapping switch
    {
        HomographyMapping h => new JsonObject { ["model"] = "homography", ["h"] = Matrix(h.ImageToPage) },
        RadialHomographyMapping r => new JsonObject
        {
            ["model"] = "radial", ["centreX"] = r.CentreX, ["centreY"] = r.CentreY, ["scale"] = r.Scale, ["k1"] = r.K1, ["k2"] = r.K2, ["h"] = Matrix(r.NormalisedToPage),
        },
        SurfaceMapping s => new JsonObject
        {
            ["model"] = "surface",
            ["parameters"] = JsonSerializer.SerializeToNode(s.Parameters, MappingOptions),
            ["page"] = new JsonArray(s.PageBounds.Left, s.PageBounds.Top, s.PageBounds.Right, s.PageBounds.Bottom),
        },
        _ => null,
    };

    /// <summary>
    /// What a photograph was taken with and how good it is, never where or when, NOTES-FROM-PLANNING.md entry 157 section 3 item 5: the
    /// session file's record, and entry 187 section 2 sends the same with a target, every part of the score, so real submissions can tune
    /// its levels later.
    /// </summary>
    public static object? CaptureDocument(GroupLab.Core.Capture.CaptureRecord? capture) => capture is null
        ? null
        : new
        {
            lens = capture.Lens,
            focalLengthMm = capture.FocalLengthMm,
            focalLength35mm = capture.FocalLength35mm,
            offAxisDegrees = capture.OffAxisDegrees,
            focalSource = capture.FocalSource,
            correction = capture.Correction,
            k1 = capture.K1,
            k2 = capture.K2,
            quality = new
            {
                score = capture.Quality.Score,
                words = capture.Quality.Words,
                blurInches = capture.Quality.BlurInches,
                focusPart = capture.Quality.FocusPart,
                clippedShare = capture.Quality.ClippedShare,
                paperLevel = capture.Quality.PaperLevel,
                exposurePart = capture.Quality.ExposurePart,
                anglePart = capture.Quality.AnglePart,
                leastPixelsPerInch = capture.Quality.LeastPixelsPerInch,
                resolutionPart = capture.Quality.ResolutionPart,
                markingsRead = capture.Quality.MarkingsRead,
                markingsExpected = capture.Quality.MarkingsExpected,
                markingsPart = capture.Quality.MarkingsPart,
                description = capture.Quality.Describe(),
            },
        };

    private static readonly JsonSerializerOptions MappingOptions = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    private static JsonArray Matrix(Homography h) =>
        new([.. Enumerable.Range(0, 9).Select(i => (JsonNode)JsonValue.Create(h[i / 3, i % 3]))]);

    private static Homography Matrix(JsonNode node) => new([.. node.AsArray().Select(v => (double)v!)]);

    private static IPageMapping? ReadMapping(JsonNode? node) => (string?)node?["model"] switch
    {
        "homography" => new HomographyMapping(Matrix(node!["h"]!)),
        "radial" => new RadialHomographyMapping((double)node!["centreX"]!, (double)node["centreY"]!, (double)node["scale"]!, (double)node["k1"]!, (double)node["k2"]!, Matrix(node["h"]!)),
        "surface" when node!["page"] is JsonArray page => new SurfaceMapping(node["parameters"].Deserialize<SurfaceModel>(MappingOptions)!, (double)page[0]!, (double)page[1]!, (double)page[2]!, (double)page[3]!),
        _ => null,
    };

    private static ScaleReference? ReadScale(JsonNode? node, List<string> notes)
    {
        switch ((string?)node?["kind"])
        {
            case null:
                return null;
            case "length":
                return new LengthReference(Point(node!["a"])!.Value, Point(node["b"])!.Value, (double)node["inches"]!);
            case "rectangle":
                return new RectangleReference([.. node!["corners"]!.AsArray().Select(c => Point(c)!.Value)], (double)node["widthInches"]!, (double)node["heightInches"]!);
            case "sheet" when ReadMapping(node!["mapping"]) is { } mapping:
                return new SheetReference(mapping, (string?)node["summary"] ?? "") { MarkersFound = (int?)node["markersFound"], MarkersExpected = (int?)node["markersExpected"], PrintScale = (double?)node["printScale"] };
            case "sheet":
                notes.Add("The sheet's registration is not stored in the file. Detect on the GroupLab sheet again to restore its scale.");
                return null;
            case var kind:
                throw new MarkingFileException($"This marking file's scale is of a kind, \"{kind}\", that this version of GroupLab does not know.");
        }
    }

    private static PointD? Point(JsonNode? node) => node is null ? null : new PointD((double)node["x"]!, (double)node["y"]!);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };
}
