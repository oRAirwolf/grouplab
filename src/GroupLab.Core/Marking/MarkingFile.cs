using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GroupLab.Core.Imaging;

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
            shotDistanceInches = state.ShotDistanceInches,
            displayUnits = displayUnits is { } units ? new { linear = units.Linear.ToString(), angular = units.Angular.ToString(), distance = units.Distance.ToString() } : null,
            scale = ScaleDocument(state.Scale),
            scaleDescription = report.Scale,
            scaleAssumesSquareOn = report.ScaleAssumesSquareOn,
            registration = state.RegistrationSummary,
            pointOfAim = state.PointOfAim,
            bulls = state.Bulls.Select(b => new { b.Index, b.Label, image = b.Image, b.Scoring }),
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
                oversize = s.Oversize is { } flag ? new { holes = flag.Holes, tentative = flag.Tentative } : null,
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
            s["oversize"] is JsonObject flag ? new DetectedOversize((double)flag["holes"]!, (bool?)flag["tentative"] ?? false) : null)).ToImmutableList();
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
            file["calibre"] is { } calibre ? new Calibre((string?)calibre["name"] ?? "", (double)calibre["diameterInches"]!) : null,
            (double?)file["shotDistanceInches"],
            Detection: file["detection"] is JsonObject detection
                ? new DetectionRecord(
                    detection["calibre"] is { } used ? new Calibre((string?)used["name"] ?? "", (double)used["diameterInches"]!) : null,
                    (double?)detection["holeSizeInches"])
                : null,
            Dismissed: file["reviewKept"] is JsonArray kept ? [.. kept.Select(k => (string)k!)] : null);
        return (state, notes);
    }

    private static object? ScaleDocument(ScaleReference? scale) => scale switch
    {
        LengthReference length => new { kind = "length", a = length.A, b = length.B, inches = length.Inches },
        RectangleReference rectangle => new { kind = "rectangle", corners = rectangle.Corners, widthInches = rectangle.WidthInches, heightInches = rectangle.HeightInches },
        SheetReference sheet => new { kind = "sheet", summary = sheet.Summary },
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
