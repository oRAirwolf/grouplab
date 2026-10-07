using System.Text.Json.Nodes;
using Avalonia.Controls;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Updates;
using OpenCvSharp;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 3.5, question 81 A: the phone sends targets as the desktop does, the same package and the same
/// queue (<see cref="TargetSender"/>), under entry 357's rules: a picture GroupLab could not read is not sent, a picture with a bank card in
/// it is never sent (entry 365), and over Wi-Fi only unless mobile data is allowed in Settings. Entry 379 switched it on from nightly 173, once the stores' privacy answers
/// were updated; the switch (<see cref="SharingSwitches.TargetsFromPhone"/>) can still turn it off, and then nothing of it is shown or sent.
/// </summary>
internal static class PhoneSending
{
    private static string PendingFolder => Path.Combine(Path.GetDirectoryName(Phone.Settings.Path) ?? ".", "pending-targets");

    private static TargetSender Sender => new(TheOutsideWorld.Current, PendingFolder);

    /// <summary>Whether this phone may send now: the switch on, the receiver open, and Wi-Fi or mobile data allowed.</summary>
    private static bool MaySend => SharingSwitches.TargetsFromPhone && ReceiverTerms.Current.AppOpen
        && (Phone.Platform.Unmetered == true || Phone.Settings.LoadMobileData());

    /// <summary>
    /// After a target is read and shown: sent at once where the choice is to send every target, asked where it is Ask me each time, and
    /// nothing otherwise. The card for the result's column, or null where nothing is to be shown.
    /// </summary>
    public static Control? After(MarkingState detected, MarkingState shown)
    {
        if (!SharingSwitches.TargetsFromPhone || !ReceiverTerms.Current.AppOpen || shown.ImagePath is not { } image || !File.Exists(image) || shown.Shots.Count == 0)
        {
            return null;
        }

        var (choice, level) = Phone.Settings.LoadSending();
        var line = Screens.Line("");
        if (choice is SendingChoice.Always or SendingChoice.Everything && level is { } chosen)
        {
            _ = Send(detected, shown, chosen, line);
            return line;
        }

        if (choice != SendingChoice.Ask)
        {
            return null;
        }

        var card = new StackPanel { Spacing = 8 };
        void Go(ConsentLevel l)
        {
            card.Children.Clear();
            card.Children.Add(line);
            _ = Send(detected, shown, l, line);
        }

        card.Children.Add(Screens.Heading(SharingWords.TargetsQuestion));
        card.Children.Add(Screens.Dim(SharingWords.TargetsShort));
        if (level is { } known)
        {
            card.Children.Add(Screens.Primary("Send this target, " + SharingWords.LevelName(known).ToLowerInvariant(), () => Go(known)).Id("result-send"));
        }
        else
        {
            card.Children.Add(Screens.Primary("Send it, " + SharingWords.TestingOnly.Trim().ToLowerInvariant(), () => Go(ConsentLevel.Testing)).Id("result-send-testing"));
            card.Children.Add(Screens.Choice("Send it, " + SharingWords.MayBePublished.Trim().ToLowerInvariant(), () => Go(ConsentLevel.Publishable)).Id("result-send-publishable"));
        }

        card.Children.Add(Screens.Choice("Not this one", () =>
        {
            card.Children.Clear();
            DiagnosticLog.Info("send.declined");
        }).Id("result-send-not"));
        return Screens.Card(card);
    }

    /// <summary>On start and on Wi-Fi: whatever waits is tried again, and what has waited seven days is let go.</summary>
    public static async Task RetryAsync()
    {
        if (!MaySend)
        {
            return;
        }

        foreach (var outcome in await Sender.RetryAsync(ReceiverTerms.Current.AppReceiver, DateTime.UtcNow, CancellationToken.None).ConfigureAwait(false))
        {
            if (outcome.Result == SendResult.Sent && outcome.Reference is { } reference)
            {
                Phone.Settings.AddSent(reference);
            }
        }
    }

    private static async Task Send(MarkingState detected, MarkingState shown, ConsentLevel level, TextBlock line)
    {
        var package = await Task.Run(() => Package(detected, shown, level)).ConfigureAwait(true);
        if (package is null)
        {
            line.Text = "Not sent: this picture cannot be sent as it is.";
            return;
        }

        var sender = Sender;
        if (!MaySend)
        {
            // Entry 357: on mobile data, unless allowed, it waits for Wi-Fi.
            sender.Keep(package, DateTime.UtcNow);
            line.Text = "Kept to send on Wi-Fi.";
            DiagnosticLog.Info("send.kept", ("why", "metered"));
            return;
        }

        var outcome = await sender.SendAsync(package, ReceiverTerms.Current.AppReceiver, CancellationToken.None).ConfigureAwait(true);
        if (outcome.Result == SendResult.Sent && outcome.Reference is { } reference)
        {
            Phone.Settings.AddSent(reference);
        }

        line.Text = "";
        if (outcome.Result == SendResult.Refused)
        {
            ProblemSheet.Stop(line, line, "The target was not sent", outcome.Message);
        }
        else
        {
            line.Text = outcome.Message;
        }
    }

    /// <summary>The package the desktop sends for the same target, or null where the picture cannot go.</summary>
    internal static TargetPackage? Package(MarkingState detected, MarkingState state, ConsentLevel level)
    {
        if (state.ImagePath is not { } path || !File.Exists(path) || GroupLab.Cli.Library.ScaleMarkerFinder.ContainsCard(path))
        {
            return null;
        }

        var terms = ReceiverTerms.Current;
        byte[] file = File.ReadAllBytes(path);
        var (image, name, _) = TargetPackages.PrepareImage(file, "target" + Path.GetExtension(path), terms.MaxImageBytes, LosslessPng);
        if (image is null)
        {
            return null;
        }

        var all = GroupAnalysis.Analyse(state).Counted;
        var told = new JsonObject
        {
            ["calibreResolved"] = state.Calibre?.Name,
            ["calibreInches"] = state.Calibre?.DiameterInches,
            ["distanceInches"] = state.ShotDistanceInches,
            ["roundsFired"] = state.ExpectedShots,
            ["paper"] = state.Paper,
            ["backing"] = state.Backing,
        };
        var figures = new JsonObject
        {
            ["shots"] = all?.Shots,
            ["meanRadiusInches"] = all?.MeanRadius?.Value,
            ["sigmaInches"] = all?.Sigma?.Value,
            ["extremeSpreadInches"] = all?.ExtremeSpread?.Value,
            ["scale"] = state.Scale?.Description,
            ["registration"] = state.RegistrationSummary,
            ["printScale"] = (state.Scale as SheetReference)?.PrintScale,
            ["capture"] = System.Text.Json.JsonSerializer.SerializeToNode(MarkingFile.CaptureDocument(state.Capture)),
        };
        var environment = new JsonObject { ["text"] = $"GroupLab {AppInfo.ShortVersion} on a phone, {System.Runtime.InteropServices.RuntimeInformation.OSDescription}" };
        return TargetPackages.Build(image, name, [.. detected.Shots], [.. state.Shots], told, figures, environment, Log(), level, terms);
    }

    private static string Log()
    {
        if (DiagnosticLog.Current.FilePath is not { } file || !File.Exists(file))
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

    private static byte[]? LosslessPng(byte[] file)
    {
        using var mat = Cv2.ImDecode(file, ImreadModes.Unchanged | ImreadModes.IgnoreOrientation);
        return mat.Empty() ? null : mat.ImEncode(".png");
    }
}
