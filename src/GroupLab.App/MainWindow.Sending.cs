using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Updates;
using OpenCvSharp;

namespace GroupLab.App;

/// <summary>
/// Sending a target to the project, NOTES-FROM-PLANNING.md entry 165. It is asked, never assumed: after Accept and analyze a short panel at
/// the foot of the figures offers to send the image, what GroupLab found, what the person corrected and the session's log; Settings has the
/// standing choice and the consent level; and the first time GroupLab opens after the receiver does, one screen asks once. Nothing is shown
/// and nothing is sent while the receiver is closed, <see cref="ReceiverTerms.AppOpen"/>. A package that cannot go now is kept and tried
/// again on the next start for seven days, and the person is told once, quietly.
/// </summary>
public sealed partial class MainWindow
{
    private readonly Border sendPanel = new() { IsVisible = false, Padding = new Thickness(Tokens.Space12), Classes = { AppStyles.JudgementCard } };
    private readonly TextBlock sentLine = new() { TextWrapping = TextWrapping.Wrap, IsVisible = false, Classes = { AppStyles.Secondary } };
    private readonly Border firstRun = new() { IsVisible = false };
    private readonly StackPanel sendingSettings = new() { Spacing = Tokens.Space8 };

    /// <summary>The image the question was answered for, so Not this time is final for that target.</summary>
    private string? sendAnsweredFor;

    /// <summary>The level chosen in the question, where Settings has none yet.</summary>
    private ConsentLevel? questionLevel;

    private Avalonia.Controls.Button? sendButton;

    /// <summary>Whether the receiver is open: the build's limits.json, which the tests can override.</summary>
    internal bool ReceiverOpen { get; set; } = ReceiverTerms.Current.AppOpen;

    private string PendingFolder => Path.Combine(Path.GetDirectoryName(settingsStore.Path) ?? ".",
        Path.GetFileNameWithoutExtension(settingsStore.Path) == "settings" ? "pending-targets" : Path.GetFileNameWithoutExtension(settingsStore.Path) + ".pending-targets");

    private TargetSender Sender => new(TheOutsideWorld.Current, PendingFolder);

    /// <summary>
    /// After Accept and analyze: sends without a question where the choice is Always, and otherwise shows the question at the foot of the
    /// figures, never over them. It never blocks or delays the analysis.
    /// </summary>
    private void OfferToSend()
    {
        sendPanel.IsVisible = false;
        if (!ReceiverOpen || session.State.ImagePath is not { } image || !File.Exists(image) || sendAnsweredFor == image)
        {
            return;
        }

        var (choice, level) = settingsStore.LoadSending();
        if (choice == SendingChoice.Never)
        {
            return;
        }

        if (choice == SendingChoice.Always && level is { } chosen)
        {
            sendAnsweredFor = image;
            _ = SendThisTargetAsync(chosen);
            return;
        }

        ShowQuestion(level);
    }

    private void ShowQuestion(ConsentLevel? level)
    {
        questionLevel = level;
        var terms = ReceiverTerms.Current;
        var body = new StackPanel { Spacing = Tokens.Space8 };
        body.Children.Add(new TextBlock { Text = "Help improve GroupLab's detection?", Classes = { AppStyles.Label } });
        body.Children.Add(Line("Send this target to the project: the image, what GroupLab found, what you corrected, and the log from this session. Nothing goes until you press Send."));
        var what = new StackPanel { Spacing = Tokens.Space4, IsVisible = false };
        foreach (string line in TargetPackages.WhatIsSent)
        {
            what.Children.Add(Line("• " + line));
        }

        var size = Line("");
        what.Children.Add(size);
        var consentWords = Line("");
        what.Children.Add(consentWords);
        var send = sendButton = Button("Send", () =>
        {
            if (questionLevel is { } chosen)
            {
                sendAnsweredFor = session.State.ImagePath;
                sendPanel.IsVisible = false;
                _ = SendThisTargetAsync(chosen);
            }
        });
        send.IsEnabled = level is not null;
        if (level is null)
        {
            // Entry 165 section 2: the person picks a level when they say yes, and nothing is chosen for them.
            var testing = new RadioButton { GroupName = "sendLevel", Content = "Testing only: used to improve detection and never published." };
            var publishable = new RadioButton { GroupName = "sendLevel", Content = "May also be published in GroupLab's public test data and research." };
            testing.IsCheckedChanged += (_, _) => Chosen(ConsentLevel.Testing, testing.IsChecked == true);
            publishable.IsCheckedChanged += (_, _) => Chosen(ConsentLevel.Publishable, publishable.IsChecked == true);
            body.Children.Add(testing);
            body.Children.Add(publishable);

            void Chosen(ConsentLevel chosen, bool on)
            {
                if (on)
                {
                    questionLevel = chosen;
                    send.IsEnabled = true;
                    consentWords.Text = "You agree to: " + terms.Text(chosen);
                }
            }
        }
        else
        {
            consentWords.Text = "You agree to: " + terms.Text(level.Value);
        }

        body.Children.Add(what);
        body.Children.Add(Row(Button("What gets sent", () =>
        {
            what.IsVisible = !what.IsVisible;
            size.Text = PackageFor(questionLevel ?? ConsentLevel.Testing) is { } package
                ? string.Create(CultureInfo.InvariantCulture, $"About {Math.Max(0.1, package.Bytes / 1048576.0):0.0} MB, the image as {package.ImageName}.")
                : "The image could not be read to send.";
        }), send, Button("Not this time", () =>
        {
            // Section 1 item 4: final for this target; the question does not come back for it.
            sendAnsweredFor = session.State.ImagePath;
            sendPanel.IsVisible = false;
            DiagnosticLog.Info("send.declined");
        })));
        sendPanel.Child = body;
        sendPanel.IsVisible = true;
    }

    /// <summary>The package for this target, or null where its image cannot be read or cannot go without loss.</summary>
    private TargetPackage? PackageFor(ConsentLevel level)
    {
        var state = session.State;
        if (state.ImagePath is not { } path || !File.Exists(path))
        {
            return null;
        }

        var terms = ReceiverTerms.Current;
        var (image, name, refusal) = TargetPackages.PrepareImage(File.ReadAllBytes(path), "target" + Path.GetExtension(path), terms.MaxImageBytes, LosslessPng);
        if (image is null)
        {
            sentLine.Text = "Not sent: " + refusal + ".";
            sentLine.IsVisible = true;
            return null;
        }

        var analysis = GroupAnalysis.Analyse(state);
        var all = analysis.AllShots;
        var told = new JsonObject
        {
            ["calibreTyped"] = calibreBox.Text,
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
            ["capture"] = state.Capture?.Quality.Describe(),
        };
        var environment = new JsonObject { ["text"] = ReportPackage.EnvironmentText(RenderScaling) };
        // What detection left, the state Discard edits returns to, before any person changed a mark.
        IReadOnlyList<MarkedShot> detected = detectedState is { } left ? [.. left.Shots] : [];
        return TargetPackages.Build(image, name, detected, [.. state.Shots], told, figures, environment, SessionLog(), level, terms);
    }

    /// <summary>This session's log, as the diagnostics already write it, with anything that looks like a path taken out, the last 1.5 MB.</summary>
    private static string SessionLog()
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

    /// <summary>Any image OpenCV reads, written again as PNG without loss and with no metadata at all.</summary>
    private static byte[]? LosslessPng(byte[] file)
    {
        using var mat = Cv2.ImDecode(file, ImreadModes.Unchanged | ImreadModes.IgnoreOrientation);
        return mat.Empty() ? null : mat.ImEncode(".png");
    }

    /// <summary>Sends this target, or keeps it to try again, and says what happened in one quiet line.</summary>
    private async Task SendThisTargetAsync(ConsentLevel level)
    {
        if (PackageFor(level) is not { } package)
        {
            return;
        }

        var outcome = await Sender.SendAsync(package, ReceiverTerms.Current.AppReceiver, CancellationToken.None);
        if (outcome.Result == SendResult.Sent && outcome.Reference is { } reference)
        {
            settingsStore.AddSent(reference);
        }

        sentLine.Text = outcome.Message;
        sentLine.IsVisible = true;
        toaster.Say(outcome.Message);
        FillSendingSettings();
    }

    /// <summary>On start: whatever is waiting is tried again, and what has waited seven days is let go, with a line in the log.</summary>
    internal async Task RetryPendingAsync()
    {
        if (!ReceiverOpen)
        {
            return;
        }

        foreach (var outcome in await Sender.RetryAsync(ReceiverTerms.Current.AppReceiver, DateTime.UtcNow, CancellationToken.None))
        {
            if (outcome.Result == SendResult.Sent && outcome.Reference is { } reference)
            {
                settingsStore.AddSent(reference);
            }
        }

        FillSendingSettings();
    }

    /// <summary>
    /// Entry 165 section 1.1: the first run question, once, and only while the receiver is open. Nothing is preselected; the person chooses
    /// Send every target automatically, Ask me each time or Never, and a level where they send at all.
    /// </summary>
    private Border BuildFirstRun()
    {
        firstRun.Background = new SolidColorBrush(Tokens.Scrim);
        return firstRun;
    }

    internal void ShowFirstRunIfDue()
    {
        if (!ReceiverOpen || settingsStore.LoadSending().Choice != SendingChoice.Unset)
        {
            return;
        }

        var terms = ReceiverTerms.Current;
        var card = new StackPanel { Spacing = Tokens.Space8, MaxWidth = 620, Margin = new Thickness(Tokens.Space24) };
        card.Children.Add(new TextBlock { Text = "Send your targets to help improve GroupLab?", Classes = { AppStyles.Title } });
        card.Children.Add(Line("Each target you analyze can go to the project, to test and improve detection. This is what goes:"));
        foreach (string line in TargetPackages.WhatIsSent)
        {
            card.Children.Add(Line("• " + line));
        }

        var testing = new RadioButton { GroupName = "firstRunLevel", Content = "Testing only. " + terms.TestingText };
        var publishable = new RadioButton { GroupName = "firstRunLevel", Content = "May be published. " + terms.PublishableText };
        foreach (var radio in new[] { testing, publishable })
        {
            radio.Classes.Add(AppStyles.Secondary);
            card.Children.Add(radio);
        }

        var why = Line("");
        card.Children.Add(why);
        void Choose(SendingChoice choice)
        {
            ConsentLevel? level = testing.IsChecked == true ? ConsentLevel.Testing : publishable.IsChecked == true ? ConsentLevel.Publishable : null;
            if (choice == SendingChoice.Always && level is null)
            {
                why.Text = "Choose testing only or may be published first, so every target goes with the consent you mean.";
                return;
            }

            settingsStore.SaveSending(choice, level);
            DiagnosticLog.Info("send.first-run", ("choice", choice.ToString()), ("level", level?.ToString()));
            firstRun.IsVisible = false;
            FillSendingSettings();
        }

        card.Children.Add(Row(Button("Send every target automatically", () => Choose(SendingChoice.Always)), Button("Ask me each time", () => Choose(SendingChoice.Ask)), Button("Never", () => Choose(SendingChoice.Never))));
        card.Children.Add(Line("You can change this at any time in Settings, under Sending targets."));
        firstRun.Child = new Border
        {
            Child = card,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { AppStyles.Side },
        };
        firstRun.IsVisible = true;
    }

    /// <summary>Entry 165 section 9: Settings' own Sending targets section, reading and writing the same setting as the first run screen.</summary>
    private void BuildSendingSettings(StackPanel column)
    {
        column.Children.Add(Ruled("Sending targets"));
        column.Children.Add(sendingSettings);
        FillSendingSettings();
    }

    internal void FillSendingSettings()
    {
        sendingSettings.Children.Clear();
        if (!ReceiverOpen)
        {
            sendingSettings.Children.Add(Line("Sending targets to the project is not open yet. When it is, GroupLab will ask once whether you want to."));
            return;
        }

        var (choice, level) = settingsStore.LoadSending();
        var terms = ReceiverTerms.Current;
        var choices = new StackPanel { Spacing = Tokens.Space4 };
        foreach (var (value, words) in new[] { (SendingChoice.Always, "Send every target automatically"), (SendingChoice.Ask, "Ask me each time"), (SendingChoice.Never, "Never") })
        {
            var radio = new RadioButton { GroupName = "sendingChoice", Content = words, IsChecked = choice == value };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settingsStore.LoadSending().Choice != value)
                {
                    settingsStore.SaveSending(value, settingsStore.LoadSending().Level);
                    DiagnosticLog.Info("send.choice", ("choice", value.ToString()));
                }
            };
            choices.Children.Add(radio);
        }

        sendingSettings.Children.Add(choices);
        sendingSettings.Children.Add(FieldLabel("Consent for targets sent from now on"));
        var levels = new StackPanel { Spacing = Tokens.Space4 };
        foreach (var (value, words) in new[] { (ConsentLevel.Testing, "Testing only. " + terms.TestingText), (ConsentLevel.Publishable, "May be published. " + terms.PublishableText) })
        {
            var radio = new RadioButton { GroupName = "sendingLevel", Content = new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap, MaxWidth = 640 }, IsChecked = level == value };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settingsStore.LoadSending().Level != value)
                {
                    // A change applies to targets sent from now on and never re-labels one already sent.
                    settingsStore.SaveSending(settingsStore.LoadSending().Choice, value);
                }
            };
            levels.Children.Add(radio);
        }

        sendingSettings.Children.Add(levels);
        sendingSettings.Children.Add(FieldLabel("What is sent"));
        foreach (string line in TargetPackages.WhatIsSent)
        {
            sendingSettings.Children.Add(Line("• " + line));
        }

        var sent = settingsStore.LoadSent();
        sendingSettings.Children.Add(Line(sent.Count == 0
            ? "No targets have been sent from this computer."
            : string.Create(CultureInfo.InvariantCulture, $"{sent.Count} target{(sent.Count == 1 ? " has" : "s have")} been sent from this computer: {string.Join(", ", sent)}. To have one removed, write to {SupportLink.Email} with its reference.")));

        var pending = Sender.Pending();
        if (pending.Count > 0)
        {
            sendingSettings.Children.Add(Line(string.Create(CultureInfo.InvariantCulture, $"{pending.Count} target{(pending.Count == 1 ? " is" : "s are")} waiting to be sent, and will be tried again at the next start.")));
            sendingSettings.Children.Add(Row(Button("Send them now", () => _ = RetryPendingAsync()), Button("Discard them", () =>
            {
                foreach (var waiting in Sender.Pending())
                {
                    Sender.Discard(waiting);
                }

                DiagnosticLog.Info("send.discarded", ("count", pending.Count));
                FillSendingSettings();
            })));
        }
    }

    /// <summary>What the question panel and the line under it say, for the headless tests.</summary>
    internal IEnumerable<string> SendText => sendPanel.IsVisible
        ? sendPanel.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Append(sentLine.Text ?? "")
        : [sentLine.Text ?? ""];

    internal bool QuestionShown => sendPanel.IsVisible;

    /// <summary>Whether the question's Send can be pressed: never before a level is chosen.</summary>
    internal bool SendEnabled => sendButton?.IsEnabled == true;

    internal bool FirstRunShown => firstRun.IsVisible;

    /// <summary>Presses a button on the question or the first run screen by its words, as a person would, for the headless tests.</summary>
    internal void PressSend(string words)
    {
        Control root = firstRun.IsVisible ? firstRun : sendPanel;
        foreach (var radio in root.GetLogicalDescendants().OfType<RadioButton>().Where(r => (r.Content as string)?.StartsWith(words, StringComparison.Ordinal) == true))
        {
            radio.IsChecked = true;
            return;
        }

        var button = root.GetLogicalDescendants().OfType<Avalonia.Controls.Button>().First(b => b.Content as string == words);
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
    }

    internal IEnumerable<string> SendingSettingsText => sendingSettings.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")
        .Concat(sendingSettings.GetLogicalDescendants().OfType<RadioButton>().Select(r => r.Content as string ?? (r.Content as TextBlock)?.Text ?? ""));
}

/// <summary>What sending one package came to.</summary>
internal enum SendResult
{
    Sent,
    Kept,
    Refused,
}

/// <summary>The outcome and the one line a person is told.</summary>
internal sealed record SendOutcome(SendResult Result, string? Reference, string Message);

/// <summary>
/// Sends packages and keeps those that could not go, NOTES-FROM-PLANNING.md entry 165 section 6. A package the receiver refused for a
/// reason that trying again cannot fix is let go at once and the reason shown; one that met no answer, a limit or a closed receiver is kept
/// in its own folder and tried again at the next start, for seven days, then let go with a line in the log.
/// </summary>
internal sealed class TargetSender(IOutsideWorld outside, string folder)
{
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(7);

    public async Task<SendOutcome> SendAsync(TargetPackage package, string address, CancellationToken token)
    {
        var outcome = await Post(package, address, token).ConfigureAwait(true);
        if (outcome.Result == SendResult.Kept)
        {
            Keep(package, DateTime.UtcNow);
        }

        return outcome;
    }

    private async Task<SendOutcome> Post(TargetPackage package, string address, CancellationToken token)
    {
        var answer = await outside.PostTargetAsync(address, package.Json, package.Image, package.ImageName, token).ConfigureAwait(true);
        if (answer is null)
        {
            DiagnosticLog.Warn("send.unanswered");
            return new SendOutcome(SendResult.Kept, null, "The target could not be sent just now; it is kept and will be tried again when GroupLab next starts.");
        }

        JsonNode? reply;
        try
        {
            reply = JsonNode.Parse(answer.Body);
        }
        catch (JsonException)
        {
            reply = null;
        }

        if (reply?["ok"]?.GetValueKind() == JsonValueKind.True && (string?)reply["id"] is { Length: > 0 } id)
        {
            DiagnosticLog.Info("send.sent", ("reference", id));
            return new SendOutcome(SendResult.Sent, id, $"Sent to the project. Its reference is {id}.");
        }

        bool retry = reply?["retry"]?.GetValueKind() == JsonValueKind.True || answer.Status is 429 or >= 500 || reply is null;
        string why = (string?)reply?["error"] ?? string.Create(CultureInfo.InvariantCulture, $"the receiver answered {answer.Status}");
        DiagnosticLog.Warn("send.refused", ("status", answer.Status), ("retry", retry));
        return retry
            ? new SendOutcome(SendResult.Kept, null, "The target could not be sent just now (" + why.TrimEnd('.') + "); it is kept and will be tried again when GroupLab next starts.")
            : new SendOutcome(SendResult.Refused, null, "The target was not sent: " + why);
    }

    private void Keep(TargetPackage package, DateTime now)
    {
        string into = Path.Combine(folder, now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(into);
        File.WriteAllBytes(Path.Combine(into, "image"), package.Image);
        File.WriteAllText(Path.Combine(into, "name.txt"), package.ImageName);
        File.WriteAllText(Path.Combine(into, "package.json"), package.Json);
    }

    /// <summary>The folders of the packages waiting, oldest first.</summary>
    public IReadOnlyList<string> Pending() => Directory.Exists(folder) ? [.. Directory.EnumerateDirectories(folder).Order(StringComparer.Ordinal)] : [];

    public void Discard(string pending) => Directory.Delete(pending, recursive: true);

    /// <summary>Tries every waiting package again, lets go of any seven days old, and returns what came of each tried.</summary>
    public async Task<IReadOnlyList<SendOutcome>> RetryAsync(string address, DateTime now, CancellationToken token)
    {
        var outcomes = new List<SendOutcome>();
        foreach (string pending in Pending())
        {
            string stamp = Path.GetFileName(pending).Split('-')[0];
            if (DateTime.TryParseExact(stamp, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var kept)
                && now - kept > KeptFor)
            {
                DiagnosticLog.Info("send.expired", ("days", (int)(now - kept).TotalDays));
                Discard(pending);
                continue;
            }

            try
            {
                var package = new TargetPackage(File.ReadAllBytes(Path.Combine(pending, "image")), File.ReadAllText(Path.Combine(pending, "name.txt")), File.ReadAllText(Path.Combine(pending, "package.json")));
                var outcome = await Post(package, address, token).ConfigureAwait(true);
                if (outcome.Result != SendResult.Kept)
                {
                    Discard(pending);
                }

                outcomes.Add(outcome);
            }
            catch (IOException ex)
            {
                DiagnosticLog.Exception(GroupLab.App.Diagnostics.LogLevel.Warn, "send.retry", ex);
            }
        }

        return outcomes;
    }
}
