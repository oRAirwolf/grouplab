using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Records;

/// <summary>A session file as read: the marking with no image path yet, the sheet, the image, and who wrote which revision.</summary>
public sealed record SessionPackageContents(MarkingState State, TargetDefinition? Definition, byte[] Image, string ImageExtension, string Device, int Revision,
    string WrittenUtc, IReadOnlyList<string> Notes);

/// <summary>A session file that cannot be read, with the reason in words a person can act on.</summary>
public sealed class SessionPackageException(string message) : Exception(message);

/// <summary>
/// A session as one file, docs/ANDROID.md section 8 stage A and NOTES-FROM-PLANNING.md entry 219 item A5: shared by hand between the phone
/// and the desktop through the share sheet, a file manager, email or USB. A zip of exactly four entries: <c>session.json</c>, which says
/// the revision and the device that wrote it (section 8's rule, so an edit on two devices is never silently lost); <c>marking.json</c>, the
/// desktop's own marking file; <c>sheet.gltd.json</c>, the sheet it was analyzed against, where there is one; and the image.
/// <para>
/// <b>Opening one is reading a stranger's file.</b> Only those names are read, each within its size, and nothing in it is acted on. The
/// image is whatever the writer put there: the writer is responsible for leaving out anything it should not carry, which is why
/// <see cref="Write"/> takes pixels already re-encoded, never a photograph's own bytes with their metadata.
/// </para>
/// </summary>
public static class SessionPackage
{
    public const string Extension = ".grouplab";

    public const string Schema = "grouplab-session-1";

    private const long MostJsonBytes = 4 * 1024 * 1024;

    private const long MostImageBytes = 40 * 1024 * 1024;

    private static readonly string[] ImageNames = ["image.jpg", "image.png"];

    /// <summary>Writes the package. <paramref name="image"/> is an encoded image with no metadata; its kind is named by <paramref name="imageExtension"/>.</summary>
    public static void Write(Stream to, MarkingState state, TargetDefinition? definition, UnitSettings units, byte[] image, string imageExtension, string device,
        int revision, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(image);
        string ext = imageExtension.TrimStart('.').ToLowerInvariant() switch
        {
            "png" => "png",
            _ => "jpg",
        };
        using var zip = new ZipArchive(to, ZipArchiveMode.Create, leaveOpen: true);
        Entry(zip, "session.json", new JsonObject
        {
            ["schema"] = Schema,
            ["revision"] = revision,
            ["device"] = device.Length > 80 ? device[..80] : device,
            ["written"] = utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
        }.ToJsonString());
        Entry(zip, "marking.json", MarkingFile.Write(state with { ImagePath = "image." + ext }, units));
        if (definition is not null)
        {
            Entry(zip, "sheet.gltd.json", Encoding.UTF8.GetString(CanonicalJsonWriter.Write(definition)));
        }

        var picture = zip.CreateEntry("image." + ext, CompressionLevel.NoCompression);
        using var stream = picture.Open();
        stream.Write(image);
    }

    /// <summary>Reads a package, refusing anything that is not one with the reason.</summary>
    public static SessionPackageContents Read(Stream from)
    {
        ArgumentNullException.ThrowIfNull(from);
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(from, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            throw new SessionPackageException("This is not a GroupLab session file.");
        }

        using (zip)
        {
            var session = Json(zip, "session.json") ?? throw new SessionPackageException("This is not a GroupLab session file: it has no session record.");
            if ((string?)session["schema"] != Schema)
            {
                throw new SessionPackageException("This session file was written by a newer GroupLab. Update GroupLab to open it.");
            }

            string marking = Text(zip, "marking.json") ?? throw new SessionPackageException("This session file has no marking in it.");
            MarkingState state;
            IReadOnlyList<string> notes;
            try
            {
                (state, notes) = MarkingFile.Read(marking);
            }
            catch (MarkingFileException e)
            {
                throw new SessionPackageException("The session file's marking could not be read: " + e.Message);
            }

            TargetDefinition? definition = null;
            if (Text(zip, "sheet.gltd.json") is { } sheet)
            {
                definition = GltdJsonReader.Read(Encoding.UTF8.GetBytes(sheet)).Definition;
            }

            var entry = ImageNames.Select(zip.GetEntry).FirstOrDefault(e => e is not null)
                ?? throw new SessionPackageException("This session file has no image in it.");
            byte[] image = Bytes(entry, MostImageBytes) ?? throw new SessionPackageException("The session file's image is larger than GroupLab opens.");
            int revision = session["revision"] is JsonValue r && r.TryGetValue(out int n) && n >= 1 ? n : 1;
            string device = (string?)session["device"] ?? "";
            return new SessionPackageContents(state with { ImagePath = null }, definition, image, Path.GetExtension(entry.Name), device.Length > 80 ? device[..80] : device,
                revision, (string?)session["written"] ?? "", notes);
        }
    }

    /// <summary>The image written beside the package's marking in <paramref name="folder"/>, and the marking pointed at it.</summary>
    public static MarkingState Unpack(SessionPackageContents contents, string folder)
    {
        ArgumentNullException.ThrowIfNull(contents);
        Directory.CreateDirectory(folder);
        string image = Path.Combine(folder, "target" + contents.ImageExtension);
        File.WriteAllBytes(image, contents.Image);
        return contents.State with { ImagePath = image };
    }

    private static void Entry(ZipArchive zip, string name, string text)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
        writer.Write(text);
    }

    private static JsonObject? Json(ZipArchive zip, string name)
    {
        try
        {
            return Text(zip, name) is { } text ? JsonNode.Parse(text) as JsonObject : null;
        }
        catch (JsonException)
        {
            throw new SessionPackageException($"This session file's {name} could not be read.");
        }
    }

    private static string? Text(ZipArchive zip, string name) =>
        zip.GetEntry(name) is { } entry
            ? (Bytes(entry, MostJsonBytes) is { } bytes ? Encoding.UTF8.GetString(bytes) : throw new SessionPackageException($"This session file's {name} is larger than GroupLab reads."))
            : null;

    /// <summary>An entry's bytes, read no further than <paramref name="most"/>, whatever the entry claims its size is; null when it is larger.</summary>
    private static byte[]? Bytes(ZipArchiveEntry entry, long most)
    {
        if (entry.Length > most)
        {
            return null;
        }

        using var stream = entry.Open();
        using var copy = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (copy.Length + read > most)
            {
                return null;
            }

            copy.Write(buffer, 0, read);
        }

        return copy.ToArray();
    }
}
