using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 357 section 1: "Send everything I open". Every picture opened goes when it is left (another opened, the
/// sheet cleared, the window closed), read or not, with everything GroupLab worked out on it; a picture open when GroupLab stopped without
/// closing goes at the next start; and a picture accepted goes at Accept and analyze, linked to its first version by a picture code they
/// share. Nothing here does anything unless <see cref="SharingSwitches.EverythingOpen"/> is on and the person chose that level: with the
/// switch off the window sends exactly what it sent before the entry.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The picture "Do not send this picture" was chosen for, until it is left.</summary>
    private string? doNotSend;

    /// <summary>The picture whose finished version has gone, so leaving it sends nothing more.</summary>
    private string? acceptedSentFor;

    /// <summary>The problems shown on this picture and what was chosen, for the package.</summary>
    private readonly List<JsonObject> problemsShown = [];

    /// <summary>The last reading's stage records on this picture.</summary>
    private TraceRecorder? lastTrace;

    /// <summary>Where the picture open now is noted, so one open when GroupLab stopped without closing is sent at the next start.</summary>
    private string OpenPictureNote => PendingFolder + ".open.json";

    /// <summary>Whether every picture opened goes: the switch is on, the receiver is open, and the person chose it with a level.</summary>
    internal bool SendingEverything => ReceiverOpen && SharingSwitches.EverythingOpen
        && settingsStore.LoadSending() is { Choice: SendingChoice.Everything, Level: not null };

    /// <summary>Whether "Do not send this picture" has been chosen for the picture open now, for the tests.</summary>
    internal bool NotSendingThisPicture => doNotSend is not null && doNotSend == session.State.ImagePath;

    /// <summary>"Do not send this picture": nothing of the picture open now goes, until it is left.</summary>
    internal void DoNotSendThisPicture()
    {
        doNotSend = session.State.ImagePath;
        DiagnosticLog.Info("send.not-this-picture");
        status.Text = SharingWords.NotSendingThis + ".";
    }

    /// <summary>
    /// Leaving the picture open now: under "Send everything I open" its package is kept to send, in the state it was left in, and sent at
    /// once unless the window is closing, when the next start sends it.
    /// </summary>
    private void LeavePicture(bool closing)
    {
        string? path = session.State.ImagePath;
        try
        {
            if (path is null || !SendingEverything || doNotSend == path || acceptedSentFor == path || !File.Exists(path))
            {
                return;
            }

            var level = settingsStore.LoadSending().Level!.Value;
            string state = session.State.Shots.Count == 0 ? "unread" : "stopped-at-review";
            if (PackageFor(level, state) is not { } package)
            {
                return;
            }

            Sender.Keep(package, DateTime.UtcNow);
            DiagnosticLog.Info("send.left", ("state", state));
            if (!closing)
            {
                _ = RetryPendingAsync();
            }
        }
        finally
        {
            ForgetOpenPicture();
            doNotSend = null;
            acceptedSentFor = null;
            problemsShown.Clear();
            lastTrace = null;
        }
    }

    /// <summary>A picture just opened: noted, under "Send everything I open", so it is not lost if GroupLab stops without closing.</summary>
    private void PictureOpened(string path)
    {
        if (!SendingEverything)
        {
            return;
        }

        try
        {
            File.WriteAllText(OpenPictureNote, new JsonObject { ["path"] = path, ["opened"] = DateTime.UtcNow.ToString("O") }.ToJsonString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "send.note", ex);
        }
    }

    private void ForgetOpenPicture()
    {
        try
        {
            File.Delete(OpenPictureNote);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "send.note", ex);
        }
    }

    /// <summary>
    /// At the start: a picture that was open when GroupLab last stopped without closing goes now, as unread, with the stopped run's log.
    /// The note is forgotten either way, and nothing goes unless the person still sends everything.
    /// </summary>
    internal void SendPictureLeftOpen()
    {
        if (!File.Exists(OpenPictureNote))
        {
            return;
        }

        string? path = null;
        try
        {
            path = (string?)JsonNode.Parse(File.ReadAllText(OpenPictureNote))?["path"];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "send.note", ex);
        }

        ForgetOpenPicture();
        if (path is null || !SendingEverything || !File.Exists(path) || HasCard(path))
        {
            return;
        }

        var terms = ReceiverTerms.Current;
        byte[] file = File.ReadAllBytes(path);
        var (image, name, _) = TargetPackages.PrepareImage(file, "target" + Path.GetExtension(path), terms.MaxImageBytes, LosslessPng);
        if (image is null)
        {
            return;
        }

        string? previous = ReportPackage.LogsFor(DiagnosticLog.Current, null).PreviousLog;
        var submission = Submission(file, "unread");
        submission["stopped"] = "GroupLab stopped without closing while this picture was open";
        var package = TargetPackages.Build(image, name, [], [], [], [], new JsonObject { ["text"] = ReportPackage.EnvironmentText(RenderScaling) },
            LogText(previous), settingsStore.LoadSending().Level!.Value, terms, submission);
        Sender.Keep(package, DateTime.UtcNow);
        DiagnosticLog.Info("send.left", ("state", "unread"), ("after", "stop"));
    }

    /// <summary>
    /// The package's submission block, entry 357 section 1: the state, the picture code the versions share, the first version's reference
    /// where one has come back, and how far the picture got: the stage records, where it stopped and why, and the problems shown.
    /// </summary>
    private JsonObject Submission(byte[] file, string state)
    {
        string code = TargetPackages.PictureCode(settingsStore.LoadPictureSalt(), file);
        return new JsonObject
        {
            ["state"] = state,
            ["picture"] = code,
            ["follows"] = settingsStore.LoadPictureReference(code),
            ["opening"] = LastOpening?.ToString(),
            ["registration"] = session.State.RegistrationSummary,
            ["stages"] = CrashReporter.StagesOf(lastTrace),
            ["problems"] = new JsonArray([.. problemsShown.Select(p => (JsonNode?)p.DeepClone())]),
        };
    }

    /// <summary>
    /// Entry 357 section 2: a picture GroupLab could not read. Its stage records are kept for the package, and under "Send everything I
    /// open", with automatic reports that carry the log, it is a report of its own, matched to the picture's submission by its picture code.
    /// </summary>
    private void NoteFailedRead(string failure, TraceRecorder trace)
    {
        lastTrace = trace;
        if (!SendingEverything || !ErrorsOpen || !SharingSwitches.FullLogOpen || settingsStore.LoadErrorChoice() != ErrorReportChoice.Always
            || settingsStore.LoadErrorWording() < ErrorReports.FullLogWording || session.State.ImagePath is not { } path || !File.Exists(path)
            || doNotSend == path)
        {
            return;
        }

        string code = TargetPackages.PictureCode(settingsStore.LoadPictureSalt(), File.ReadAllBytes(path));
        if (ErrorReports.RecordReadFailure(DiagnosticLog.Current.Directory, failure, CrashReporter.StagesOf(trace), code) is not null)
        {
            _ = SendWaitingErrorsAsync();
        }
    }

    /// <summary>A problem shown on this picture, and later the choice made, for the package.</summary>
    private void NoteProblem(string title, string? choice)
    {
        if (choice is null)
        {
            problemsShown.Add(new JsonObject { ["title"] = title });
        }
        else if (problemsShown.LastOrDefault(p => (string?)p["title"] == title && p["choice"] is null) is { } shown)
        {
            shown["choice"] = choice;
        }
    }

    /// <summary>Keeps the first reference the project gives each picture code, so a later version names it.</summary>
    private void RememberPicture(string packageJson, string reference)
    {
        try
        {
            if ((string?)JsonNode.Parse(packageJson)?["submission"]?["picture"] is { Length: 32 } code)
            {
                settingsStore.SavePictureReference(code, reference);
            }
        }
        catch (JsonException)
        {
        }
    }

    /// <summary>A log file, read without locking it and with paths taken out, the last 1.5 MB.</summary>
    private static string LogText(string? file)
    {
        if (file is null || !File.Exists(file))
        {
            return "";
        }

        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            string text = DiagnosticLog.Scrub(reader.ReadToEnd());
            const int most = 1536 * 1024;
            return text.Length > most ? text[^most..] : text;
        }
        catch (IOException)
        {
            return "";
        }
    }
}
