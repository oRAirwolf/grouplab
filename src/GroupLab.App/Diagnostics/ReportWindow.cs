using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GroupLab.App.Theme;

namespace GroupLab.App.Diagnostics;

/// <summary>
/// The report dialog, NOTES-FROM-PLANNING.md entry 41 section 6. It shows the user what is in the report before it goes anywhere: one plain
/// sentence saying what the package contains and what it does not, then the exact entries. Nothing is sent from here without a click, and
/// saving is the first choice rather than sending. "Show me the file" opens the folder the package was saved in.
/// </summary>
public sealed class ReportWindow : Window
{
    /// <summary>The plain sentence of section 6.</summary>
    internal const string Contents = "This report contains error details, your GroupLab log files, and information about your computer. It does not contain your photographs or any location information.";

    private readonly string? crash;
    private readonly string? runLog;
    private readonly string? previousLog;
    // Entry 194 section 2.3: a report made by hand may keep its description, at most 500 characters.
    private readonly TextBox description = new() { [Avalonia.Automation.AutomationProperties.NameProperty] = "What happened", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 90, MaxLength = 500, PlaceholderText = "What were you doing when it went wrong? Optional, up to 500 characters." };
    private readonly TextBox contact = new() { [Avalonia.Automation.AutomationProperties.NameProperty] = "Contact", PlaceholderText = "An email address, if you would like a reply. Optional." };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly Button reveal = new() { Content = "Show me the file", IsEnabled = false };
    private readonly Button send = new() { Content = "Send", Classes = { AppStyles.Primary } };
    private readonly TextBlock fallback = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly bool receiverOpen;
    private string? saved;

    /// <param name="crash">The crash record the report is about, or null for a report made without a crash.</param>
    /// <param name="receiverOpen">Entry 457 section 2: whether GroupLab's error receiver is open; while it is, Send sends the report through it,
    /// as the automatic error reports go, and the email and GitHub sentence is only the fallback.</param>
    public ReportWindow(string? crash, string? runLog, string? previousLog, bool receiverOpen)
    {
        this.crash = crash;
        this.runLog = runLog;
        this.previousLog = previousLog;
        this.receiverOpen = receiverOpen;
        Title = "GroupLab: report a problem";
        Width = 560;
        Height = 600;

        var panel = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space8 };
        panel.Children.Add(new TextBlock { Text = Contents, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(Label("In the report"));
        foreach (string entry in PlannedEntries)
        {
            panel.Children.Add(new TextBlock { Text = entry, FontFamily = Tokens.Mono, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        }

        panel.Children.Add(Label("What happened"));
        panel.Children.Add(description);
        panel.Children.Add(Label("Contact"));
        panel.Children.Add(contact);

        var save = new Button { Content = "Save only" };
        save.Click += async (_, _) => await SaveDialog();
        reveal.Click += (_, _) =>
        {
            if (saved is not null)
            {
                CrashReporter.Reveal(saved);
            }
        };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8 };
        buttons.Children.Add(save);
        buttons.Children.Add(reveal);
        send.IsVisible = receiverOpen;
        send.Click += async (_, _) => await Send();

        // Sending first, where it can be; saving is always there beside it.
        buttons.Children.Insert(0, send);
        panel.Children.Add(buttons);
        panel.Children.Add(status);

        // Entry 126 section 1.2: once the package is written, a person has to know where to send it. Both ways the support page names are
        // named here, so nobody has to go and look for them, and the build line is asked for because a report without it names no build.
        // Entry 457 section 2: only the way round when sending cannot be done, with the reason.
        fallback.Text = receiverOpen ? "" : NotOpen + " " + WhereToSendIt;
        fallback.IsVisible = !receiverOpen;
        panel.Children.Add(new TextBlock { Text = ContactStays, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        panel.Children.Add(fallback);
        Content = new ScrollViewer { Content = panel };
    }

    /// <summary>
    /// Where a written report goes, entry 126 section 1.2: the two ways the support page at grouplab.org names, and the one thing a report
    /// is useless without. The address lives in <see cref="SupportLink"/> and is written nowhere else.
    /// </summary>
    internal static string WhereToSendIt =>
        "When it is saved, send it either way: open an issue at " + Repository + ", or email it to " + SupportLink.Email
        + ". Either way, include the build line from the settings page, because a test build changes whenever the code does.";

    /// <summary>Why the fallback is shown when sending is closed.</summary>
    internal const string NotOpen = "Sending reports from GroupLab is not open yet, so save it instead.";

    /// <summary>What happens to a contact typed here.</summary>
    internal const string ContactStays = "Send carries what you write under What happened, the logs and the error; a contact stays in a saved report only.";

    /// <summary>Where issues go. The same address the settings page's link to the project opens.</summary>
    internal const string Repository = "https://github.com/oRAirwolf/grouplab";

    /// <summary>The entries the report will hold, as the user is shown them before anything is written.</summary>
    internal IReadOnlyList<string> PlannedEntries =>
    [
        .. crash is null ? [] : new[] { Path.GetFileName(crash) + ", what went wrong" },
        .. runLog is null ? [] : new[] { Path.GetFileName(runLog) + ", the log of the run" },
        .. previousLog is null || previousLog == runLog ? [] : new[] { Path.GetFileName(previousLog) + ", the log of the run before" },
        "environment.txt, the version of GroupLab, the operating system and the display",
        "description.txt, if you write what happened",
        "contact.txt, if you give a way to reach you",
    ];

    /// <summary>The status line, for the headless tests.</summary>
    internal string StatusText => status.Text ?? "";

    internal void SetDescription(string text) => description.Text = text;

    /// <summary>Writes the package to a path. A report about a crash marks the crash dealt with, so the next launch stops offering it.</summary>
    internal PackageResult? SaveTo(string path)
    {
        try
        {
            // Entry 192 section 3.4: every record on this computer, grouped, so a reader sees one error five times rather than five crashes.
            string environment = ReportPackage.EnvironmentText(CrashReporter.DisplayScale);
            if (crash is not null && Path.GetDirectoryName(crash) is { } records && CrashReporter.Summary(Directory.EnumerateFiles(records, "crash-*.json").Order(StringComparer.Ordinal)) is { Length: > 0 } summary)
            {
                environment += "\n\nErrors recorded on this computer, grouped:\n" + summary;
            }

            var result = ReportPackage.Build(path, crash, runLog, previousLog, environment, description.Text, contact.Text);
            saved = path;
            reveal.IsEnabled = true;
            status.Text = $"Saved to {path}." + (result.DroppedPreviousLog ? " The log of the run before was left out to keep the report small." : "");
            if (crash is not null)
            {
                CrashReporter.MarkHandled(crash);
            }

            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            status.Text = "The report could not be saved: " + ex.Message;
            DiagnosticLog.Exception(LogLevel.Warn, "report.save", ex);
            return null;
        }
    }

    /// <summary>
    /// Sends the report, entry 457 section 2: through GroupLab's own receivers, as the automatic error reports go, with what the person
    /// wrote, the error this report is about and the logs of its run, under the same rules (no picture, no path, nothing about where). The
    /// package is still saved first, into the log directory when the user has not saved it, so it is kept whatever happens; a failure shows
    /// the email and GitHub way round, with why.
    /// </summary>
    private async Task Send()
    {
        string path = saved ?? Path.Combine(DiagnosticLog.Current.Directory ?? Path.GetTempPath(), string.Create(System.Globalization.CultureInfo.InvariantCulture, $"grouplab-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip"));
        if (SaveTo(path) is not { } result)
        {
            return;
        }

        if (result.TooLargeToSend)
        {
            status.Text = $"The report is larger than the 2 MB that can be sent, so it was saved and not sent. It is at {path}.";
            return;
        }

        send.IsEnabled = false;
        status.Text = "Sending…";
        string said = await ErrorReports.SendDiagnosticsAsync(GroupLab.Core.Updates.TheOutsideWorld.Current, GroupLab.Core.Publication.ReceiverTerms.Current.CrashReceiver,
            GroupLab.Core.Publication.ReceiverTerms.Current.ErrorReceiver, description.Text, CancellationToken.None, crash);
        bool sent = said.StartsWith("Sent to GroupLab", StringComparison.Ordinal);
        status.Text = said;
        send.IsEnabled = !sent;
        if (sent && crash is not null)
        {
            CrashReporter.MarkHandled(crash);
        }

        fallback.Text = sent ? "" : "It did not go, so it is saved. " + WhereToSendIt;
        fallback.IsVisible = !sent;
    }

    private async Task SaveDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "save-report"));
        var desktop = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the report",
            SuggestedFileName = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"grouplab-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip"),
            DefaultExtension = "zip",
            SuggestedStartLocation = desktop,
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "save-report"), ("chosen", file is not null));
        if (file?.TryGetLocalPath() is { } path)
        {
            SaveTo(path);
        }
    }

    private static TextBlock Label(string text) => new() { Text = text.ToUpperInvariant(), Margin = new Thickness(0, Tokens.Space8, 0, 0), Classes = { AppStyles.Section } };
}
