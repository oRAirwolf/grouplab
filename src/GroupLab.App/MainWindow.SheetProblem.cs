using System.Security.Cryptography;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Publication;
using GroupLab.Core.Registration;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 356 sections 6 and 7, board "B, final candidate: reading harder, and not a GroupLab sheet": a picture that
/// looks like a GroupLab sheet and whose codes would not read. The picture with its codes outlined and the stage list, "Looks like a GroupLab
/// sheet" ticked so the reason for the error shows; the title, the reason and the printed name where it is known; Choose the sheet in
/// amber first, Try again, reading harder where that has not run, Mark it by hand; under a rule, "Not a GroupLab sheet?" and the calm
/// question; and the links Show what went wrong and Send it to the project. Dismissed, it leaves an amber bar that brings it back, and Show
/// work names it.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>The amber bar a dismissed sheet problem leaves under the header.</summary>
    private readonly Border problemBar = new() { IsVisible = false, Classes = { AppStyles.ProblemBar } };

    /// <summary>The sheet problem open now or behind the bar: the picture's images and what the opening found.</summary>
    private SheetProblem? sheetProblem;

    /// <summary>Whether the amber bar is showing, for the headless tests.</summary>
    internal bool ProblemBarShown => problemBar.IsVisible;

    /// <summary>Whether reading harder has run on the picture open now, so its button is offered only once.</summary>
    private bool readHarderRan;

    /// <summary>What a sheet problem is about.</summary>
    private sealed record SheetProblem(GrayImage Grey, GrayImage Value, ImageMetadata Metadata, SheetIdentity Identity, SheetLook Look, string? PrintedName);

    private Border BuildProblemBar()
    {
        var mark = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = "!", FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Warn } },
            Classes = { AppStyles.Pill, AppStyles.Warn },
            Padding = new Thickness(0),
        };
        var choose = new Button { Content = OpeningWords.ChooseSheet, Classes = { AppStyles.Primary } };
        AutomationProperties.SetName(choose, OpeningWords.ChooseSheet);
        choose.Click += (_, _) => ChooseFromProblem();
        var all = new Button { Content = OpeningWords.AllChoices };
        AutomationProperties.SetName(all, OpeningWords.AllChoices);
        all.Click += (_, _) => ReopenSheetProblem();
        var row = new DockPanel();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { choose, all } };
        DockPanel.SetDock(mark, Dock.Left);
        DockPanel.SetDock(buttons, Dock.Right);
        row.Children.Add(mark);
        row.Children.Add(buttons);
        row.Children.Add(new TextBlock { Text = OpeningWords.BarSays, Margin = new Thickness(Tokens.Space12, 0), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
        problemBar.Child = row;
        return problemBar;
    }

    /// <summary>A new picture, or a sheet that reads: no problem is left over from the last.</summary>
    private void ForgetSheetProblem()
    {
        if (sheetProblem is not null && ProblemOpen)
        {
            CloseProblem();
        }

        sheetProblem = null;
        problemBar.IsVisible = false;
        ShowWorkAttention();
    }

    /// <summary>The SHA-256 of the picture open now, which is how "not a GroupLab sheet" is remembered; null where it cannot be read.</summary>
    private string? PictureHash()
    {
        try
        {
            return session.State.ImagePath is { } path && File.Exists(path) ? Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// The opening's problem case: unless the person has said this picture is not a GroupLab sheet, in which case it is asked about calmly
    /// as it was then.
    /// </summary>
    private void ShowSheetProblem(GrayImage g, GrayImage v, ImageMetadata m, SheetIdentity identity, SheetLook look, string? printedName = null)
    {
        if (PictureHash() is { } hash && settingsStore.LoadNotGroupLab(hash))
        {
            DiagnosticLog.Info("opening.remembered-not-grouplab");
            AskWhichTargetIsThis(g, v, m);
            return;
        }

        sheetProblem = new SheetProblem(g, v, m, identity, look, printedName);
        pendingDetection = (g, v, m);
        problem.Text = "";
        ReopenSheetProblem();
    }

    /// <summary>The problem dialog of board B, for the sheet problem open now.</summary>
    private void ReopenSheetProblem()
    {
        if (sheetProblem is not { } p)
        {
            return;
        }

        problemBar.IsVisible = false;
        bool unknown = p.Identity.DefinitionId is not null;
        string title = unknown ? OpeningWords.UnknownTitle : OpeningWords.CodesTitle;
        int of = Math.Max(OpeningWords.CornerCodes, p.Look.Codes.Count);
        int read = unknown ? Math.Max(1, p.Identity.CodesRead) : 0;
        status.Text = title + ".";

        var list = new List<(string Label, Action Act)> { (OpeningWords.ChooseSheet, ChooseFromProblem) };
        if (CanReadHarder)
        {
            list.Add((OpeningWords.ReadHarder, () => _ = ReadHarderAsync()));
        }

        list.Add((OpeningWords.MarkByHand, MarkByHand));
        list.Add((OpeningWords.StoreOrDrawn, NotAGroupLabSheet));
        list.Add((OpeningWords.ShowWhatWentWrong, ShowWhatWentWrong));
        bool sendable = ReceiverOpen && settingsStore.LoadSending().Choice != SendingChoice.Always;
        if (sendable)
        {
            list.Add((OpeningWords.SendToProject, SendFromProblem));
        }

        // The picture with the codes outlined, and the stage list with "Looks like a GroupLab sheet" ticked.
        var left = new StackPanel { Width = 170, Spacing = Tokens.Space8 };
        left.Children.Add(CodesPicture(p, 170));
        foreach (var (mark, words, kind) in new (string, string, string?)[]
        {
            ("✓", OpeningWords.PictureOpened, AppStyles.Good),
            ("✓", OpeningWords.LooksLikeGroupLab, AppStyles.Good),
            ("✕", OpeningWords.CornerCodesStage(read, of), AppStyles.Alert),
            ("○", OpeningWords.WhichSheet, AppStyles.Faint),
            ("○", OpeningWords.FindingHoles, AppStyles.Faint),
        })
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8 };
            line.Children.Add(new TextBlock { Text = mark, Classes = { kind! } });
            line.Children.Add(new TextBlock { Text = words, Classes = { kind == AppStyles.Good ? AppStyles.Secondary : kind! } });
            left.Children.Add(line);
        }

        var right = new StackPanel { Spacing = Tokens.Space12 };
        right.Children.Add(new TextBlock
        {
            Text = unknown ? OpeningWords.UnknownReason(p.Identity.DefinitionId!) : OpeningWords.CodesReason(read, of),
            TextWrapping = TextWrapping.Wrap,
            Classes = { AppStyles.Secondary },
        });
        if (p.PrintedName is { } name)
        {
            right.Children.Add(new TextBlock { Text = OpeningWords.PrintedName(name), TextWrapping = TextWrapping.Wrap });
        }

        var big = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = Tokens.Space8, LineSpacing = Tokens.Space8, Margin = new Thickness(0, Tokens.Space8, 0, 0) };
        big.Children.Add(ProblemButton(OpeningWords.ChooseSheet, primary: true));
        if (CanReadHarder)
        {
            big.Children.Add(ProblemButton(OpeningWords.ReadHarder));
        }

        big.Children.Add(ProblemButton(OpeningWords.MarkByHand));
        right.Children.Add(big);
        var notOurs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space12 };
        notOurs.Children.Add(new TextBlock { Text = OpeningWords.NotGroupLab, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } });
        var storeOrDrawn = ProblemButton(OpeningWords.StoreOrDrawn);
        storeOrDrawn.MinHeight = 34;
        notOurs.Children.Add(storeOrDrawn);
        right.Children.Add(new Border { Child = notOurs, Classes = { AppStyles.JudgementCard } });
        var links = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18 };
        links.Children.Add(ProblemButton(OpeningWords.ShowWhatWentWrong, link: ""));
        if (sendable)
        {
            links.Children.Add(ProblemButton(OpeningWords.SendToProject, link: ""));
        }

        right.Children.Add(links);
        var body = new DockPanel();
        DockPanel.SetDock(left, Dock.Left);
        left.Margin = new Thickness(0, 0, 22, 0);
        body.Children.Add(left);
        body.Children.Add(right);
        OpenProblem(title, ProblemCard(title, 720, body), list, dismissed: () =>
        {
            problemBar.IsVisible = sheetProblem is not null;
            ShowWorkAttention();
        });
        ShowWorkAttention();
        DiagnosticLog.Info("opening.problem", ("unknown", unknown), ("markers", p.Look.Markers), ("codes", p.Look.Codes.Count), ("harder", readHarderRan));
    }

    /// <summary>The picture, small, with every code located on it outlined, or its four corners where none was.</summary>
    private Control CodesPicture(SheetProblem p, double width)
    {
        double scale = width / Math.Max(1, p.Grey.Width);
        double height = Math.Min(260, p.Grey.Height * scale);
        scale = Math.Min(scale, height / Math.Max(1, p.Grey.Height));
        double w = p.Grey.Width * scale, h = p.Grey.Height * scale;
        var canvasLayer = new Avalonia.Controls.Canvas { Width = w, Height = h };
        if (this.canvas.Frame.Picture is { } picture)
        {
            canvasLayer.Children.Add(new Image { Source = picture, Width = w, Height = h, Stretch = Stretch.Fill });
        }

        IBrush outline = Brushes.OrangeRed;
        var boxes = p.Look.Codes.Count > 0
            ? p.Look.Codes.Select(b => (Left: b.Min(c => c.X) * scale, Top: b.Min(c => c.Y) * scale, Right: b.Max(c => c.X) * scale, Bottom: b.Max(c => c.Y) * scale)).ToList()
            : [(4.0, 4.0, 30.0, 30.0), (w - 30, 4.0, w - 4, 30.0), (4.0, h - 30, 30.0, h - 4), (w - 30, h - 30, w - 4, h - 4)];
        foreach (var (l, t, r, b) in boxes)
        {
            var box = new Rectangle { Width = Math.Max(6, r - l), Height = Math.Max(6, b - t), Stroke = outline, StrokeThickness = 2 };
            Avalonia.Controls.Canvas.SetLeft(box, l);
            Avalonia.Controls.Canvas.SetTop(box, t);
            canvasLayer.Children.Add(box);
        }

        AutomationProperties.SetName(canvasLayer, "The picture, its corner codes outlined");
        return new Border { Child = canvasLayer, CornerRadius = new CornerRadius(4), ClipToBounds = true, HorizontalAlignment = HorizontalAlignment.Left };
    }

    /// <summary>
    /// The problem dialog over the picture open now, as opening it would show it, for the screenshot walk: its codes looked for, none read.
    /// </summary>
    internal void ShowSheetProblemForScreens(string? printedName = null)
    {
        if (grey is null || valueImage is null || metadata is null)
        {
            return;
        }

        var look = SheetLook.Of(grey, new GroupLab.Cli.Imaging.OpenCvSharpBackend());
        ShowSheetProblem(grey, valueImage, metadata, new SheetIdentity(null, null, 0, 0, "no code on the sheet could be read"), look, printedName);
    }

    /// <summary>The calm "Which target is this?" over the picture open now, for the screenshot walk.</summary>
    internal void ShowWhichTargetForScreens()
    {
        ForgetSheetProblem();
        if (grey is not null && valueImage is not null && metadata is not null)
        {
            AskWhichTargetIsThis(grey, valueImage, metadata);
        }
    }

    /// <summary>The window's settings, for the headless tests.</summary>
    internal AppSettingsStore Settings => settingsStore;

    /// <summary>The amber bar's Choose the sheet, for the headless tests.</summary>
    internal void ChooseSheetFromBar() => ChooseFromProblem();

    /// <summary>Choose the sheet: the sheets by name, the printed name first where it is known.</summary>
    private void ChooseFromProblem()
    {
        if (sheetProblem is not { } p)
        {
            return;
        }

        CloseProblem();
        problemBar.IsVisible = true;
        OfferTheSheet(p.Grey, p.Value, p.Metadata, p.Identity.Failure ?? "", p.Identity.DefinitionId);
        if (p.PrintedName is { } name && pendingSheets.ToList().FindIndex(d => d.Name == name || GroupLab.Core.Gltd.Binary.GltdBinary.Encode(d).Encoding?.DefinitionId == name) is int at and >= 0)
        {
            sheetChoice.SelectedIndex = at;
        }
    }

    /// <summary>Not a GroupLab sheet: remembered for this picture and counted, no picture sent, then the calm question.</summary>
    private void NotAGroupLabSheet()
    {
        if (sheetProblem is not { } p)
        {
            return;
        }

        if (PictureHash() is { } hash)
        {
            settingsStore.SaveNotGroupLab(hash);
        }

        DiagnosticLog.Info("opening.not-grouplab", ("count", settingsStore.LoadNotGroupLabCount()), ("markers", p.Look.Markers), ("codes", p.Look.Codes.Count));
        sheetProblem = null;
        problemBar.IsVisible = false;
        ShowWorkAttention();
        AskWhichTargetIsThis(p.Grey, p.Value, p.Metadata);
    }

    /// <summary>Show what went wrong: the work bar, at the stage that failed.</summary>
    private void ShowWhatWentWrong()
    {
        problemBar.IsVisible = sheetProblem is not null;
        SetShowWork(true);
        timelineExpander.IsExpanded = true;
    }

    /// <summary>Send it to the project, where sending is not already automatic: at the level chosen before, or asked now.</summary>
    private void SendFromProblem()
    {
        problemBar.IsVisible = sheetProblem is not null;
        if (settingsStore.LoadSending().Level is { } level)
        {
            _ = SendThisTargetAsync(level);
            return;
        }

        ShowProblem("Send it to the project?", "The picture, what GroupLab found and the log from this session go to the project, to make reading sheets like it better.",
            ("Testing only", () => _ = SendThisTargetAsync(ConsentLevel.Testing)),
            ("May also be published", () => _ = SendThisTargetAsync(ConsentLevel.Publishable)),
            ("Not now", () => { }));
    }

    /// <summary>Whether "Try again, reading harder" is offered: only where a harder reading exists that has not run on this picture.</summary>
    private bool CanReadHarder => HarderReadingExists && !readHarderRan;

    /// <summary>Whether this build has a reading harder than the first; section 6's passes are the part after this one.</summary>
    private const bool HarderReadingExists = false;

    private Task ReadHarderAsync() => Task.CompletedTask;
}
