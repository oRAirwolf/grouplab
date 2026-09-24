using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Publication;

/// <summary>What a person has chosen about sending targets to the project, NOTES-FROM-PLANNING.md entry 165 sections 1 and 1.1.</summary>
public enum SendingChoice
{
    /// <summary>Not yet chosen: the first run screen asks, once, when the receiver is open.</summary>
    Unset,

    /// <summary>Every target goes after Accept and analyze, with no further question.</summary>
    Always,

    /// <summary>The analysis asks, for each target.</summary>
    Ask,

    /// <summary>Never asked and never sent.</summary>
    Never,
}

/// <summary>The two consent levels of entry 165 section 2.</summary>
public enum ConsentLevel
{
    /// <summary>Level 1: used to test and improve detection, kept by the project, never published.</summary>
    Testing,

    /// <summary>Level 2: level 1, and it may be published in GroupLab's public test data and research articles under the GPL-3.0.</summary>
    Publishable,
}

/// <summary>
/// The receiver's terms, read from <c>website/api/limits.json</c>, which is built into the application so the words a person agrees to
/// here are the words the upload page shows and both receivers record: entry 165 section 2 and entry 159's one source rule.
/// </summary>
public sealed record ReceiverTerms(string ConsentVersion, string TestingText, string PublishableText, bool AppOpen, string AppReceiver, long MaxImageBytes, long MaxPackageBytes)
{
    private static readonly Lazy<ReceiverTerms> Built = new(() =>
    {
        using var stream = typeof(ReceiverTerms).Assembly.GetManifestResourceStream("GroupLab.limits.json")
            ?? throw new InvalidOperationException("limits.json is not built into the application");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var texts = root.GetProperty("consentTexts");
        return new ReceiverTerms(
            root.GetProperty("consentVersion").GetString()!,
            texts.GetProperty("testing").GetString()!,
            texts.GetProperty("publishable").GetString()!,
            root.GetProperty("appOpen").GetBoolean(),
            root.GetProperty("appReceiver").GetString()!,
            root.GetProperty("maxFileMegabytes").GetInt64() * 1024 * 1024,
            root.GetProperty("maxAppPackageMegabytes").GetInt64() * 1024 * 1024)
        {
            ErrorReportsOpen = root.GetProperty("errorReportsOpen").GetBoolean(),
            ErrorReceiver = root.GetProperty("errorReceiver").GetString()!,
            MaxErrorReportsPerDay = root.GetProperty("maxErrorReportsPerDay").GetInt32(),
        };
    });

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 194: whether the error report receiver and its worker are installed. Nothing is asked and nothing is
    /// sent while it is false, as with <see cref="AppOpen"/>.
    /// </summary>
    public bool ErrorReportsOpen { get; init; }

    /// <summary>Where error reports go: grouplab.org, never GitHub, which only the server's worker talks to.</summary>
    public string ErrorReceiver { get; init; } = "";

    /// <summary>Entry 194 section 2.5: the most reports one installation sends in a day.</summary>
    public int MaxErrorReportsPerDay { get; init; }

    /// <summary>The terms this build carries.</summary>
    public static ReceiverTerms Current => Built.Value;

    /// <summary>The words of a level, as the person agrees to them.</summary>
    public string Text(ConsentLevel level) => level == ConsentLevel.Testing ? TestingText : PublishableText;

    /// <summary>A level's name as the receiver records it.</summary>
    public static string Name(ConsentLevel level) => level == ConsentLevel.Testing ? "testing" : "publishable";
}

/// <summary>A package ready to send: the image as it will leave the machine, its name, and everything else as one JSON text.</summary>
public sealed record TargetPackage(byte[] Image, string ImageName, string Json)
{
    /// <summary>The approximate size in bytes, for "What gets sent".</summary>
    public long Bytes => Image.LongLength + Encoding.UTF8.GetByteCount(Json);
}

/// <summary>
/// The package a target is sent to the project in, NOTES-FROM-PLANNING.md entry 165 section 3: the image with its pixels untouched and no
/// location, time or serial in it; what GroupLab detected before any person changed it; what the person changed, the most valuable part,
/// because the difference between the two is labelled ground truth; what they told it; the analysis; the session's log; and the build. One
/// JSON text holds all but the image, with a manifest naming the image by its size and SHA-256, because the receiver writes it into the
/// submission's meta.json and the worker must see a folder of the shape it already takes.
/// </summary>
public static class TargetPackages
{
    public const string Schema = "grouplab-app-submission-1";

    /// <summary>The parts every package carries beside the manifest and the consent, as the receiver requires them.</summary>
    public static IReadOnlyList<string> Parts { get; } = ["detected", "corrected", "told", "analysis", "environment", "log"];

    /// <summary>What goes, in the words the question and Settings show.</summary>
    public static IReadOnlyList<string> WhatIsSent { get; } =
    [
        "The image, its pixels untouched, with every location, date, time and serial number taken out.",
        "What GroupLab found on it, before you changed anything.",
        "What you changed: every mark moved, added, deleted, split, reassigned or excluded.",
        "What you told it: the caliber, the distance, the rounds fired, the paper and the backing.",
        "The figures the analysis showed and how the scale was set.",
        "This session's log, with file names reduced to a code, and the version of GroupLab.",
    ];

    /// <summary>
    /// The image as it may leave the machine: a JPEG or PNG with its compressed pixels copied byte for byte and only the camera and exposure
    /// facts kept around them, <see cref="ImageScrubber"/>'s rule, which drops every location, time and serial; anything else, or one still
    /// over the receiver's limit, re-encoded losslessly as PNG by <paramref name="losslessPng"/>, never lossily. Null with the reason where
    /// even that does not fit.
    /// </summary>
    public static (byte[]? Image, string Name, string? Refusal) PrepareImage(byte[] file, string name, long maxBytes, Func<byte[], byte[]?> losslessPng)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(losslessPng);
        bool jpeg = file.Length > 3 && file[0] == 0xFF && file[1] == 0xD8 && file[2] == 0xFF;
        bool png = file.Length > 8 && file[0] == 0x89 && file[1] == (byte)'P' && file[2] == (byte)'N' && file[3] == (byte)'G';
        string stem = Path.GetFileNameWithoutExtension(name);
        if (jpeg || png)
        {
            var scrubbed = ImageScrubber.Scrub(file);
            if (scrubbed.Bytes.LongLength <= maxBytes)
            {
                return (scrubbed.Bytes, stem + (jpeg ? ".jpg" : ".png"), null);
            }
        }

        var lossless = losslessPng(file);
        if (lossless is null)
        {
            return (null, name, "the image could not be read to send");
        }

        return lossless.LongLength <= maxBytes
            ? (lossless, stem + ".png", null)
            : (null, name, string.Create(CultureInfo.InvariantCulture, $"the image is {lossless.LongLength / 1048576.0:0} MB even saved without loss, over the {maxBytes / 1048576} MB the project takes, and a smaller copy would not be the real pixels"));
    }

    /// <summary>Every mark of a marking, as the package records it: where, which bull, its measured size, and whether it counts.</summary>
    public static JsonArray Marks(IEnumerable<MarkedShot> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        var marks = new JsonArray();
        foreach (var s in shots.OrderBy(s => s.Id))
        {
            marks.Add(new JsonObject
            {
                ["id"] = s.Id,
                ["x"] = s.Image.X,
                ["y"] = s.Image.Y,
                ["bull"] = s.Bull,
                ["provenance"] = s.Provenance.ToString(),
                ["diameterInches"] = s.MeasuredDiameterInches,
                ["chosenDiameterInches"] = s.ChosenDiameterInches,
                ["sizeInHoles"] = s.Size?.Holes ?? s.Oversize?.Holes,
                ["excluded"] = s.Exclusion?.ToString(),
                ["notAShot"] = s.NotAShot,
                ["flyer"] = s.Flyer,
                ["sighter"] = s.Sighter,
            });
        }

        return marks;
    }

    /// <summary>
    /// What the person changed, entry 165 section 3 item 3: the final marks, each saying how it came to be against the detection, kept,
    /// moved, reassigned, excluded, marked not a shot, or added; and the detected marks no longer there, deleted or split into others.
    /// </summary>
    public static JsonObject Corrected(IReadOnlyList<MarkedShot> detected, IReadOnlyList<MarkedShot> final)
    {
        ArgumentNullException.ThrowIfNull(detected);
        ArgumentNullException.ThrowIfNull(final);
        var before = detected.ToDictionary(s => s.Id);
        var marks = Marks(final);
        foreach (var mark in marks.OfType<JsonObject>())
        {
            int id = (int)mark["id"]!;
            var changes = new List<string>();
            if (!before.TryGetValue(id, out var was))
            {
                changes.Add("added");
            }
            else
            {
                var now = final.First(s => s.Id == id);
                if (Math.Abs(now.Image.X - was.Image.X) > 1e-9 || Math.Abs(now.Image.Y - was.Image.Y) > 1e-9)
                {
                    changes.Add("moved");
                }

                if (now.Bull != was.Bull)
                {
                    changes.Add("reassigned");
                }

                if (now.Exclusion != was.Exclusion)
                {
                    changes.Add("excluded");
                }

                if (now.NotAShot != was.NotAShot)
                {
                    changes.Add("not a shot");
                }

                if (now.ChosenDiameterInches != was.ChosenDiameterInches)
                {
                    changes.Add("resized");
                }
            }

            mark["change"] = changes.Count == 0 ? "kept" : string.Join(", ", changes);
        }

        var gone = new JsonArray();
        foreach (var s in detected.Where(s => final.All(f => f.Id != s.Id)).OrderBy(s => s.Id))
        {
            gone.Add(new JsonObject { ["id"] = s.Id, ["x"] = s.Image.X, ["y"] = s.Image.Y });
        }

        return new JsonObject { ["marks"] = marks, ["removed"] = gone };
    }

    /// <summary>The final marks as the package's corrected list gives them back, so a test can hold the package to what the person ended with.</summary>
    public static IReadOnlyList<(int Id, PointD Image, int? Bull, string? Excluded, bool NotAShot)> Final(JsonObject corrected)
    {
        ArgumentNullException.ThrowIfNull(corrected);
        return [.. corrected["marks"]!.AsArray().OfType<JsonObject>().Select(m => ((int)m["id"]!, new PointD((double)m["x"]!, (double)m["y"]!), (int?)m["bull"], (string?)m["excluded"], (bool)m["notAShot"]!))];
    }

    /// <summary>The package, with its manifest naming the image by its size and SHA-256.</summary>
    public static TargetPackage Build(byte[] image, string imageName, IReadOnlyList<MarkedShot> detected, IReadOnlyList<MarkedShot> final, JsonObject told, JsonObject analysis, JsonObject environment, string log, ConsentLevel level, ReceiverTerms terms)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(terms);
        var package = new JsonObject
        {
            ["schema"] = Schema,
            ["consent"] = new JsonObject { ["version"] = terms.ConsentVersion, ["level"] = ReceiverTerms.Name(level), ["text"] = terms.Text(level) },
            ["manifest"] = new JsonObject
            {
                ["image"] = new JsonObject { ["name"] = imageName, ["bytes"] = image.Length, ["sha256"] = Convert.ToHexStringLower(SHA256.HashData(image)) },
                ["parts"] = new JsonArray([.. Parts.Select(p => (JsonNode?)p)]),
            },
            ["detected"] = new JsonObject { ["marks"] = Marks(detected) },
            ["corrected"] = Corrected(detected, final),
            ["told"] = told,
            ["analysis"] = analysis,
            ["environment"] = environment,
            ["log"] = log,
        };
        return new TargetPackage(image, imageName, package.ToJsonString());
    }
}
