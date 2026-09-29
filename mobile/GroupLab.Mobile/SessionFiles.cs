using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A5, docs/ANDROID.md section 8 stage A: a session shared by hand through Android's share sheet,
/// and a session file opened from the phone's files. The file is the desktop's own format (<see cref="SessionPackage"/>): the marking, the
/// sheet and the picture re-encoded with nothing else in it. Opening one makes a session of its own, never a merge.
/// </summary>
internal static class SessionFiles
{
    /// <summary>The authority the application's file provider answers to, in the manifest as well.</summary>
    /// <summary>
    /// The file provider's authority, the application's own id and <c>.files</c>, as the manifest declares it with <c>${applicationId}</c>,
    /// so the development build of entry 234 is its own application in this too.
    /// </summary>

    private static string Shared => Path.Combine(Phone.Platform.CacheFolder, "shared");

    private static string DeviceWords => Phone.Platform.DeviceWords;

    /// <summary>Writes the session to a file and offers it to the share sheet. Returns why not, or null when the sheet opened.</summary>
    public static string? Share(MarkingState state, TargetDefinition? definition, UnitSettings units)
    {
        if (state.ImagePath is not { } image || !File.Exists(image) || CleanImage.From(image) is not { } clean)
        {
            return "This session's picture is not on the phone, so there is nothing to share.";
        }

        // Only the newest shared file is kept; the share sheet has read it by the time another is made.
        if (Directory.Exists(Shared))
        {
            Directory.Delete(Shared, recursive: true);
        }

        Directory.CreateDirectory(Shared);
        int revision = Revision(image) + 1;
        string name = (definition?.Name ?? "session") + " " + DateTime.Now.ToString("yyyy-MM-dd HHmm", CultureInfo.InvariantCulture) + SessionPackage.Extension;
        string path = Path.Combine(Shared, name);
        using (var to = File.Create(path))
        {
            SessionPackage.Write(to, state, definition, units, clean.Bytes, clean.Extension, DeviceWords, revision, DateTime.UtcNow);
        }

        if (Phone.Platform.ShareFile(path, "application/octet-stream", "Share the session") is { } failed)
        {
            return failed;
        }

        DiagnosticLog.Info("session.share", ("revision", revision), ("shots", state.Shots.Count));
        return null;
    }

    /// <summary>
    /// Entry 258: the shots as a CSV file through Android's share sheet, the file the desktop's export writes: each shot's offset from its own
    /// bull, with a header that names the units and the distance, so it reads without GroupLab.
    /// </summary>
    public static string? ShareCsv(MarkingState state, TargetDefinition? definition)
    {
        if (!state.Shots.Any(s => s.IsShot))
        {
            return "There are no shots to share.";
        }

        if (Directory.Exists(Shared))
        {
            Directory.Delete(Shared, recursive: true);
        }

        Directory.CreateDirectory(Shared);
        string path = Path.Combine(Shared, (definition?.Name ?? "shots") + " " + DateTime.Now.ToString("yyyy-MM-dd HHmm", CultureInfo.InvariantCulture) + ".csv");
        File.WriteAllText(path, ShotCsv.Write(state));
        if (Phone.Platform.ShareFile(path, "text/csv", "Share the shots as CSV") is { } failed)
        {
            return failed;
        }

        DiagnosticLog.Info("session.csv", ("shots", state.Shots.Count(s => s.IsShot)));
        return null;
    }

    /// <summary>A session file read from a stream the picker gave, unpacked into its own folder and saved; the result to show, or why not.</summary>
    public static (PhoneResult? Result, string? Why) Open(Stream from, UnitSettings units)
    {
        SessionPackageContents contents;
        try
        {
            contents = SessionPackage.Read(from);
        }
        catch (SessionPackageException e)
        {
            return (null, e.Message);
        }

        string folder = Path.Combine(PhoneAnalysis.SessionsFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        var state = SessionPackage.Unpack(contents, folder);
        File.WriteAllText(Path.Combine(folder, "package.json"), new JsonObject { ["revision"] = contents.Revision, ["device"] = contents.Device }.ToJsonString());
        long? id = PhoneAnalysis.Save(state, contents.Definition, units, null);
        DiagnosticLog.Info("session.file", ("opened", true), ("revision", contents.Revision), ("shots", state.Shots.Count));
        return (new PhoneResult(state, contents.Definition, null, id), null);
    }

    /// <summary>The revision a picture's session file had when it was opened here, or none where it did not come from one.</summary>
    private static int Revision(string image)
    {
        string? file = Path.GetDirectoryName(image) is { } folder ? Path.Combine(folder, "package.json") : null;
        try
        {
            return file is not null && File.Exists(file) && JsonNode.Parse(File.ReadAllText(file))?["revision"] is JsonValue r && r.TryGetValue(out int n) ? n : 0;
        }
        catch (System.Text.Json.JsonException)
        {
            return 0;
        }
    }
}
