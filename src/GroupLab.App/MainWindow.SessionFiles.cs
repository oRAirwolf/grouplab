using System.Globalization;
using System.Text.Json.Nodes;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Records;

namespace GroupLab.App;

/// <summary>
/// Session files, NOTES-FROM-PLANNING.md entry 219 item A5 and docs/ANDROID.md section 8 stage A: a session shared by hand between the
/// phone and the desktop as one <c>.grouplab</c> file, holding the marking, the sheet and the picture re-encoded with nothing else in it.
/// Opening one is never a merge: it becomes a session of its own in Session records, so an edit made on two devices is never silently
/// lost, and the file says which device wrote which revision.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>Where opened session files keep their pictures, beside the settings, one folder each.</summary>
    private string SharedSessionsFolder => Path.Combine(Path.GetDirectoryName(settingsStore.Path) ?? ".", "shared-sessions");

    /// <summary>How the desktop names itself in a session file: the program and the system, never the machine's own name.</summary>
    private static string DeviceWords => $"GroupLab {AppInfo.Version} on " + (OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsMacOS() ? "macOS" : "Linux");

    private async Task ShareSessionDialog()
    {
        if (session.State.ImagePath is not { } image || !File.Exists(image) || CleanImage.From(image) is not { } clean)
        {
            problem.Text = "There is no picture open to share. Open a target, or reopen a session whose picture is still where it was.";
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save a session file to share",
            SuggestedFileName = (plotDefinition?.Name ?? "session") + " " + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + SessionPackage.Extension,
            DefaultExtension = SessionPackage.Extension.TrimStart('.'),
            FileTypeChoices = [new FilePickerFileType("GroupLab session file") { Patterns = ["*" + SessionPackage.Extension] }],
        });
        if (file?.TryGetLocalPath() is not { } path)
        {
            return;
        }

        int revision = Revision(image) + 1;
        await using (var to = File.Create(path))
        {
            SessionPackage.Write(to, session.State, plotDefinition, units, clean.Bytes, clean.Extension, DeviceWords, revision, DateTime.UtcNow);
        }

        status.Text = $"Saved a session file to share, revision {revision}. It holds the marks, the sheet and the picture, and nothing else from the photograph.";
        DiagnosticLog.Info("session.share", ("revision", revision), ("shots", session.State.Shots.Count));
    }

    private async Task OpenSessionFileDialog()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a GroupLab session file",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("GroupLab session files") { Patterns = ["*" + SessionPackage.Extension] }],
        });
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
        {
            Leaving(() => OpenSessionFile(path));
        }
    }

    /// <summary>Opens a session file as a new session: its picture unpacked beside the settings, its marking loaded, and saved in Session records.</summary>
    internal void OpenSessionFile(string path)
    {
        SessionPackageContents contents;
        try
        {
            using var from = File.OpenRead(path);
            contents = SessionPackage.Read(from);
        }
        catch (Exception ex) when (ex is SessionPackageException or IOException or UnauthorizedAccessException)
        {
            problem.Text = ex.Message;
            DiagnosticLog.Info("session.file", ("opened", false), ("reason", ex.GetType().Name));
            return;
        }

        string folder = Path.Combine(SharedSessionsFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        var state = SessionPackage.Unpack(contents, folder);
        File.WriteAllText(Path.Combine(folder, "package.json"), new JsonObject { ["revision"] = contents.Revision, ["device"] = contents.Device }.ToJsonString());
        bool detect = DetectOnOpen;
        DetectOnOpen = false;
        OpenImage(state.ImagePath!);
        DetectOnOpen = detect;
        plotDefinition = contents.Definition;
        registrationResidual = null;
        detectedState = null;
        session.Load(state);
        currentSession = null;
        destination = Destination.Analyse;
        SetAnalysing(true);
        SaveSession();
        status.Text = $"Opened a session file from {(contents.Device.Length > 0 ? contents.Device : "another device")}, revision {contents.Revision}. It is saved in Session records as a session of its own.";
        DiagnosticLog.Info("session.file", ("opened", true), ("revision", contents.Revision), ("shots", state.Shots.Count));
    }

    /// <summary>The revision a picture's session file had when it was opened here, or none where it did not come from one.</summary>
    private int Revision(string image)
    {
        string? folder = Path.GetDirectoryName(image);
        if (folder is null || !folder.StartsWith(SharedSessionsFolder, StringComparison.Ordinal) || !File.Exists(Path.Combine(folder, "package.json")))
        {
            return 0;
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "package.json")))?["revision"] is JsonValue r && r.TryGetValue(out int n) ? n : 0;
        }
        catch (System.Text.Json.JsonException)
        {
            return 0;
        }
    }
}
