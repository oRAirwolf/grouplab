using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// Several targets open at once, concept A as Alan chose it (planning, 2026-10-07): browser-style tabs across the top of the window, each
/// with the target's name, its state ("Analysis", or "Marking, not saved"), a saved or unsaved dot and a close button that asks first when
/// the tab has unsaved marks; a "+" tab that opens another target the way Open does; and Ctrl+Tab and Ctrl+Shift+Tab to move between them.
/// <para>
/// <b>How it is built.</b> The window was written for one target, and everything it holds for that target lives in its own fields. Rather
/// than rewrite sixteen thousand lines around a list, the window keeps showing one target, and a tab that is not showing keeps that target's
/// fields parked: its own marking session with its undo history, its picture at the zoom and place it was left, whether it was saved and
/// where, and the analysis it was on. Switching parks the target leaving and binds the one arriving, so nothing is lost and nothing mixes.
/// Which tab comes next, which one shows after a close, and whether a close asks are in <see cref="OpenTargets"/>, which is tested alone.
/// </para>
/// <para>
/// A detection running on the target showing has to finish or be cancelled before the tab is left, because it writes its result into the
/// target showing when it lands.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    private readonly OpenTargets targets = new();

    private readonly StackPanel tabRow = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };

    /// <summary>The controls of each tab, so a change to the target showing updates its tab without rebuilding the row.</summary>
    private readonly Dictionary<OpenTarget, (Button Tab, TextBlock Dot, TextBlock Name, TextBlock State)> tabControls = [];

    /// <summary>While a target is being bound in, the boxes it fills must not write back into its marking.</summary>
    private bool bindingTarget;

    /// <summary>The targets open, left to right, for the headless tests.</summary>
    internal IReadOnlyList<OpenTarget> Targets => targets.All;

    /// <summary>The target showing, for the headless tests.</summary>
    internal OpenTarget CurrentTarget => targets.Current;

    /// <summary>The tabs as the row shows them, for the headless tests: the name, the state, the dot's meaning and which one is chosen.</summary>
    internal IReadOnlyList<(string Name, string State, string Dot, bool Chosen)> TabsShown => [.. targets.All.Where(tabControls.ContainsKey).Select(t =>
    {
        var c = tabControls[t];
        return (c.Name.Text ?? "", c.State.Text ?? "", ToolTip.GetTip(c.Dot) as string ?? "", c.Tab.Classes.Contains(AppStyles.Chosen));
    })];

    /// <summary>The handler every session change goes through, whichever target's session is showing.</summary>
    private void OnSessionChanged(object? sender, EventArgs e)
    {
        Refresh();
        ShowUndoSteps();
        Avalonia.Threading.Dispatcher.UIThread.Post(AskWhichBulls);
    }

    /// <summary>The paper or the backing chosen, into the marking; not while a target's own choice is being shown as its tab comes forward.</summary>
    private void ChooseMaterial()
    {
        if (!bindingTarget)
        {
            session.SetMaterial(paperChoice.SelectedItem as string, backingChoice.SelectedItem as string);
        }
    }

    /// <summary>The row of tabs across the top of the window, with the "+" tab at its end.</summary>
    private Control BuildTargetTabs()
    {
        var add = new Button
        {
            Content = Icons.Draw(Icons.Add, 12),
            Padding = new Thickness(Tokens.Space8, Tokens.Space4),
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { AppStyles.IconButton },
        };
        ToolTip.SetTip(add, TargetTabWords.Add);
        AutomationProperties.SetName(add, TargetTabWords.Add);
        add.Click += (_, _) => AddTarget();
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { tabRow, add } };
        AddHandler(KeyDownEvent, OnTargetKey, RoutingStrategies.Tunnel);
        ShowTabs();
        return new Border
        {
            Child = new ScrollViewer { Content = row, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled },
            Padding = new Thickness(Tokens.Space8, Tokens.Space4, Tokens.Space8, 0),
            Classes = { AppStyles.Bar },
        };
    }

    /// <summary>Ctrl+Tab goes to the next tab and Ctrl+Shift+Tab to the one before, round from the end to the start.</summary>
    private void OnTargetKey(object? sender, KeyEventArgs e)
    {
        if (CommandKey.TabSwitch(e) is not { } backwards || targets.Count < 2 || ProblemOpen)
        {
            return;
        }

        ShowTarget(targets.Next(backwards));
        e.Handled = true;
    }

    /// <summary>Rebuilds the row: one tab for each target, the one showing chosen.</summary>
    private void ShowTabs()
    {
        UpdateCurrentTab(rebuild: false);
        tabRow.Children.Clear();
        tabControls.Clear();
        foreach (var target in targets.All)
        {
            tabRow.Children.Add(TabFor(target));
        }

        foreach (var target in targets.All)
        {
            ShowTab(target);
        }
    }

    private Control TabFor(OpenTarget target)
    {
        var dot = new TextBlock { Text = "●", FontSize = Tokens.BodySize, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Tokens.Space8, 0) };
        var name = new TextBlock { MaxWidth = 220, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.Medium };
        var state = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(Tokens.Space8, 0, 0, 0), Classes = { AppStyles.Secondary } };
        var tab = new Button
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { dot, name, state } },
            CornerRadius = new CornerRadius(Tokens.ButtonRadius.TopLeft, Tokens.ButtonRadius.TopRight, 0, 0),
            Margin = new Thickness(0, 0, 0, -1),
        };
        tab.Click += (_, _) => ShowTarget(target);
        var close = new Button
        {
            Content = Icons.Draw(Icons.Close, 10),
            Padding = new Thickness(Tokens.Space4),
            Margin = new Thickness(Tokens.Space4, 0, Tokens.Space12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { AppStyles.IconButton },
        };
        ToolTip.SetTip(close, TargetTabWords.Close);
        AutomationProperties.SetName(close, TargetTabWords.Close);
        close.Click += (_, _) => CloseTarget(target);
        tabControls[target] = (tab, dot, name, state);
        return new StackPanel { Orientation = Orientation.Horizontal, Children = { tab, close } };
    }

    /// <summary>Writes a tab's name, state and dot, and whether it is the one chosen.</summary>
    private void ShowTab(OpenTarget target)
    {
        if (!tabControls.TryGetValue(target, out var c))
        {
            return;
        }

        c.Name.Text = target.Label;
        c.State.Text = target.State;
        c.Dot.Classes.Set(AppStyles.Warn, target.Unsaved);
        c.Dot.Classes.Set(AppStyles.Faint, !target.Unsaved);
        ToolTip.SetTip(c.Dot, target.Unsaved ? TargetTabWords.UnsavedDot : TargetTabWords.SavedDot);
        c.Tab.Classes.Set(AppStyles.Chosen, ReferenceEquals(target, targets.Current));
        AutomationProperties.SetName(c.Tab, target.Spoken);
        ToolTip.SetTip(c.Tab, target.Spoken);
    }

    /// <summary>The target showing is read again into its tab: its name, whether it is analysed, whether it is saved. Every refresh calls it.</summary>
    private void UpdateCurrentTab(bool rebuild = true)
    {
        var current = targets.Current;
        var state = session.State;
        current.Name = state.SheetLabel ?? (state.ImagePath is { } path ? Path.GetFileName(path) : plotDefinition?.Name);
        current.Analysing = analysing;
        current.Unsaved = HasUnsavedWork;
        if (rebuild)
        {
            ShowTab(current);
        }
    }

    /// <summary>The "+" tab: a new target, shown, and the picture to open in it asked for, as Open asks.</summary>
    internal OpenTarget? AddTarget(bool choose = true)
    {
        if (detection is not null)
        {
            status.Text = TargetTabWords.WaitForDetection;
            return null;
        }

        var tab = targets.Add();
        ShowTarget(tab);
        DiagnosticLog.Info("target.tab.add", ("tabs", targets.Count));
        if (choose)
        {
            _ = OpenImageDialog();
        }

        return tab;
    }

    /// <summary>Switches to the tab at a position, for the headless tests.</summary>
    internal bool ShowTarget(int index) => ShowTarget(targets.All[index]);

    /// <summary>Shows a tab's target: the one showing is parked with its tab, and this one is bound in exactly as it was left.</summary>
    internal bool ShowTarget(OpenTarget tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (ReferenceEquals(tab, targets.Current))
        {
            if (destination != Destination.Analyse)
            {
                Go(Destination.Analyse);
            }

            return true;
        }

        if (detection is not null)
        {
            status.Text = TargetTabWords.WaitForDetection;
            return false;
        }

        if (ProblemOpen)
        {
            return false;
        }

        ParkCurrent();
        Bind(tab);
        DiagnosticLog.Info("target.tab.show", ("tab", targets.CurrentIndex + 1), ("tabs", targets.Count));
        return true;
    }

    /// <summary>Closes the tab at a position, for the headless tests.</summary>
    internal void CloseTarget(int index) => CloseTarget(targets.All[index]);

    /// <summary>
    /// The close button: a tab with unsaved marks is shown and asks first, the same question leaving a target asks anywhere else (save it,
    /// discard it, or stay). Under saving by itself, a target with a save waiting is saved rather than asked about, as closing the window does.
    /// </summary>
    internal void CloseTarget(OpenTarget tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        bool showing = ReferenceEquals(tab, targets.Current);
        if (!showing && !OpenTargets.CloseAsks(tab) && !SendingEverything)
        {
            if (tab.Parked is ParkedTarget parked)
            {
                parked.View.Picture?.Dispose();
            }

            tab.Parked = null;
            targets.Close(tab);
            DiagnosticLog.Info("target.tab.close", ("tabs", targets.Count));
            ShowTabs();
            return;
        }

        // The question is about the target in view, so a tab that has to ask, or whose picture is sent on leaving, is shown first.
        if (!showing && !ShowTarget(tab))
        {
            return;
        }

        if (detection is not null)
        {
            status.Text = TargetTabWords.WaitForDetection;
            return;
        }

        FlushWaitingSave();
        if (HasUnsavedWork && analysing)
        {
            // The question sits with the marks, so it is asked where it can be seen.
            SetAnalysing(false);
        }

        Leaving(DropCurrentTarget);
    }

    /// <summary>The target showing goes, its picture left as leaving any target leaves it, and the tab beside it is shown.</summary>
    private void DropCurrentTarget()
    {
        LeavePicture(closing: false);
        saveSoon?.Stop();
        var next = targets.Close(targets.Current);
        canvas.SetImage(null, null);
        DiagnosticLog.Info("target.tab.close", ("tabs", targets.Count));
        Bind(next);
    }

    /// <summary>Under saving by itself, a save that is waiting is done now, before the target leaves the screen.</summary>
    private void FlushWaitingSave()
    {
        if (saveSoon?.IsEnabled != true)
        {
            return;
        }

        saveSoon.Stop();
        if (HasUnsavedWork)
        {
            SaveSession();
        }
    }

    /// <summary>The target showing is parked with its tab.</summary>
    private void ParkCurrent()
    {
        FlushWaitingSave();
        leavingAsk.IsVisible = false;
        afterLeaving = null;
        discardConfirm.IsVisible = false;
        UpdateCurrentTab(rebuild: false);
        targets.Current.Parked = new ParkedTarget
        {
            Session = session,
            View = canvas.TakeView(),
            Grey = grey,
            ValueImage = valueImage,
            Metadata = metadata,
            DetectionMetadata = detectionMetadata,
            Artwork = artwork,
            StatedSize = statedSize,
            CurrentSession = currentSession,
            SavedMarking = savedMarking,
            SavedAtUtc = savedAtUtc,
            DetectedState = detectedState,
            PlotDefinition = plotDefinition,
            RegistrationResidual = registrationResidual,
            CalibreConfirmed = calibreConfirmed,
            Analysing = analysing,
            ReadHarderRan = readHarderRan,
            DistanceNotKnown = distanceNotKnown,
            RoundsNotKnown = roundsNotKnown,
            RoundsText = roundsFired.Text ?? "",
            SheetProblem = sheetProblem,
            ProblemBarShown = problemBar.IsVisible,
            PendingDetection = pendingDetection,
            SheetChooserShown = sheetChooser.IsVisible,
            PrinterPanelShown = printerPanel.IsVisible,
            PrintScaleText = printScale.Text,
            PrintScaleShown = printScale.IsVisible,
            CopyOfADesign = CopyOfADesignSeenBefore,
            Stages = [.. stages],
            StageShown = (int)Math.Round(stageSlider.Value),
            DoNotSend = doNotSend,
            AcceptedSentFor = acceptedSentFor,
            ProblemsShown = [.. problemsShown],
            LastTrace = lastTrace,
            Status = status.Text ?? "",
        };
    }

    /// <summary>Binds a tab's target into the window, exactly as it was parked, or empty where nothing has been opened in it yet.</summary>
    private void Bind(OpenTarget tab)
    {
        targets.Show(tab);
        var p = tab.Parked as ParkedTarget ?? ParkedTarget.Fresh();
        tab.Parked = null;
        bindingTarget = true;
        try
        {
            session.Changed -= OnSessionChanged;
            session = p.Session;
            session.Changed += OnSessionChanged;
            canvas.Session = session;
            canvas.ShowView(p.View);
            (canvas.StageImage as IDisposable)?.Dispose();
            canvas.StageImage = null;
            canvas.StageMarkers = [];
            canvas.StageCorners = [];
            grey = p.Grey;
            valueImage = p.ValueImage;
            metadata = p.Metadata;
            detectionMetadata = p.DetectionMetadata;
            artwork = p.Artwork;
            statedSize = p.StatedSize;
            currentSession = p.CurrentSession;
            savedMarking = p.SavedMarking;
            savedAtUtc = p.SavedAtUtc;
            detectedState = p.DetectedState;
            plotDefinition = p.PlotDefinition;
            registrationResidual = p.RegistrationResidual;
            calibreConfirmed = p.CalibreConfirmed;
            readHarderRan = p.ReadHarderRan;
            distanceNotKnown = p.DistanceNotKnown;
            roundsNotKnown = p.RoundsNotKnown;
            roundsFired.Text = p.RoundsText;
            calibreBox.Text = session.State.Calibre?.Name ?? "";
            paperChoice.SelectedItem = session.State.Paper;
            backingChoice.SelectedItem = session.State.Backing;
            sheetProblem = p.SheetProblem;
            problemBar.IsVisible = p.ProblemBarShown;
            pendingDetection = p.PendingDetection;
            sheetChooser.IsVisible = p.SheetChooserShown;
            printerPanel.IsVisible = p.PrinterPanelShown;
            printScale.Text = p.PrintScaleText;
            printScale.IsVisible = p.PrintScaleShown;
            CopyOfADesignSeenBefore = p.CopyOfADesign;
            doNotSend = p.DoNotSend;
            acceptedSentFor = p.AcceptedSentFor;
            problemsShown.Clear();
            problemsShown.AddRange(p.ProblemsShown);
            lastTrace = p.LastTrace;
            currentReview = null;
            lookingBack = false;
            bullTyped = "";
            ShowTrace(p.Stages);
            ShowStage(p.StageShown);
        }
        finally
        {
            bindingTarget = false;
        }

        destination = Destination.Analyse;
        SetAnalysing(p.Analysing);
        status.Text = p.Status;
        ShowUndoSteps();
        ShowWorkAttention();
        ShowTabs();
    }

    /// <summary>
    /// Closing the window with targets in other tabs: under saving with a button, each tab with unsaved marks is shown and asked about in
    /// turn, before the one showing. Under saving by itself every tab was saved as it was left, so there is nothing to ask.
    /// </summary>
    /// <returns>Whether a tab is being asked about, so the window stays open for now.</returns>
    private bool AskAboutOtherTabsOnClose()
    {
        if (!SaveByHand || sessions is null || targets.All.FirstOrDefault(t => !ReferenceEquals(t, targets.Current) && t.Unsaved) is not { } waiting)
        {
            return false;
        }

        if (!ShowTarget(waiting))
        {
            return false;
        }

        if (analysing)
        {
            SetAnalysing(false);
        }

        Leaving(() =>
        {
            // Answered: saved, or discarded, and either way the tab's work is settled, so it goes and the window tries to close again.
            targets.Current.Unsaved = false;
            DropCurrentTarget();
            Close();
        });
        return true;
    }

    /// <summary>Under "Send everything I open", the pictures in the other tabs are left too as the window closes, as the one showing is.</summary>
    private void LeaveOtherPicturesOnClose()
    {
        if (!SendingEverything)
        {
            return;
        }

        foreach (var tab in targets.All.Where(t => !ReferenceEquals(t, targets.Current)).ToList())
        {
            Bind(tab);
            LeavePicture(closing: true);
        }
    }

    /// <summary>Everything the window holds for one target while its tab is not showing.</summary>
    private sealed class ParkedTarget
    {
        public required MarkingSession Session { get; init; }

        public required CanvasView View { get; init; }

        public GrayImage? Grey { get; init; }

        public GrayImage? ValueImage { get; init; }

        public ImageMetadata? Metadata { get; init; }

        public ImageMetadata? DetectionMetadata { get; init; }

        public GrayImage? Artwork { get; init; }

        public StatedSheetSize? StatedSize { get; init; }

        public long? CurrentSession { get; init; }

        public string? SavedMarking { get; init; }

        public DateTime? SavedAtUtc { get; init; }

        public MarkingState? DetectedState { get; init; }

        public GroupLab.Core.Gltd.Model.TargetDefinition? PlotDefinition { get; init; }

        public double? RegistrationResidual { get; init; }

        public bool CalibreConfirmed { get; init; }

        public bool Analysing { get; init; }

        public bool ReadHarderRan { get; init; }

        public bool DistanceNotKnown { get; init; }

        public bool RoundsNotKnown { get; init; }

        public string RoundsText { get; init; } = "";

        public SheetProblem? SheetProblem { get; init; }

        public bool ProblemBarShown { get; init; }

        public (GrayImage Grey, GrayImage Value, ImageMetadata Metadata)? PendingDetection { get; init; }

        public bool SheetChooserShown { get; init; }

        public bool PrinterPanelShown { get; init; }

        public string? PrintScaleText { get; init; }

        public bool PrintScaleShown { get; init; }

        public string? CopyOfADesign { get; init; }

        public IReadOnlyList<StageRecord> Stages { get; init; } = [];

        public int StageShown { get; init; }

        public string? DoNotSend { get; init; }

        public string? AcceptedSentFor { get; init; }

        public IReadOnlyList<JsonObject> ProblemsShown { get; init; } = [];

        public TraceRecorder? LastTrace { get; init; }

        public string Status { get; init; } = NewTargetStatus;

        /// <summary>A tab with nothing opened in it yet.</summary>
        public static ParkedTarget Fresh() => new()
        {
            Session = new MarkingSession(),
            View = new CanvasView(null, null, 0, 0, 1, default, null, []),
        };
    }

    /// <summary>What the status line says on a target with nothing open, the words New target already uses.</summary>
    internal const string NewTargetStatus = "New target. Open a photograph or scan when you are ready.";
}
