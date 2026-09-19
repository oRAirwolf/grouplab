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
    private readonly TextBox description = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 90, PlaceholderText = "What were you doing when it went wrong? Optional." };
    private readonly TextBox contact = new() { PlaceholderText = "An email address, if you would like a reply. Optional." };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly Button reveal = new() { Content = "Show me the file", IsEnabled = false };
    private readonly Button send = new() { Content = "Send" };
    private readonly string sendUrl;
    private string? saved;

    /// <param name="crash">The crash record the report is about, or null for a report made without a crash.</param>
    /// <param name="sendUrl">Where a report is sent. Empty by default, and while it is empty there is no Send button (entry 41 section 7).</param>
    public ReportWindow(string? crash, string? runLog, string? previousLog, string sendUrl = "")
    {
        ArgumentNullException.ThrowIfNull(sendUrl);
        this.crash = crash;
        this.runLog = runLog;
        this.previousLog = previousLog;
        this.sendUrl = sendUrl.Trim();
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
        send.IsVisible = this.sendUrl.Length > 0;
        send.Click += async (_, _) => await Send();
        buttons.Children.Add(send);
        panel.Children.Add(buttons);
        panel.Children.Add(status);
        Content = new ScrollViewer { Content = panel };
    }

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
            var result = ReportPackage.Build(path, crash, runLog, previousLog, ReportPackage.EnvironmentText(CrashReporter.DisplayScale), description.Text, contact.Text);
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
    /// Sends the report, entry 41 section 7 and entry 45: the package is saved first, into the log directory when the user has not saved it,
    /// so the zip is kept whatever happens; a package over the cap is not sent; and there is one attempt, whose reference or error is shown.
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
        var outcome = await ReportUploader.SendAsync(sendUrl, path, AppInfo.Version);
        status.Text = outcome.Message;
        send.IsEnabled = !outcome.Sent;
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
