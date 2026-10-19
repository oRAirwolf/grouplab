using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// Point blank, Ballistics' third view, NOTES-FROM-PLANNING.md entry 455: the distances over which at least the chosen share of shots lands
/// in the zone, from the shooter's mean radius and the load's velocity SD, with the zero chosen three ways, what ends it, how to set it in
/// clicks, the wind beside it, a card to print and the zero kept on the rifle. The figures and words are <see cref="PointBlankReport"/>'s,
/// the same on the phone.
/// </summary>
public sealed partial class MainWindow
{
    private static readonly string[] PointFromChoices = ["typed here", "the group open in the analysis", "a saved session"];

    private readonly ComboBox pointFrom = new() { ItemsSource = PointFromChoices, SelectedIndex = 0, MinWidth = 220 };
    private readonly ComboBox pointSession = new() { MinWidth = 220, IsVisible = false };
    private readonly TextBox pointMeanRadius = Field("0.75");
    private readonly ComboBox pointRadiusUnit = new() { ItemsSource = new[] { "MOA", "mil", "in at 100 yd" }, SelectedIndex = 0, MinWidth = 120 };
    private readonly TextBlock pointFromWords = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly TextBox pointCertainty = Field("90");
    private readonly ComboBox pointTarget = new() { MinWidth = 220 };
    private readonly TextBox pointWidth = Field("8");
    private readonly TextBox pointHeight = Field("8");
    private readonly TextBlock pointSizeUnit = new() { VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Label } };
    private readonly TextBlock pointSource = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly TextBlock pointLaw = new() { Text = PointBlankTargets.CheckTheLaw, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly TextBox pointEnergy = Field("");
    private readonly TextBox pointLikely = Field("200");
    private readonly TextBox pointWindowFrom = Field("150");
    private readonly TextBox pointWindowTo = Field("300");
    private readonly TextBox pointWind = Field("10");
    private readonly TextBox pointAngle = Field("0");
    private readonly TextBox leastExpansion = Field();
    private readonly StackPanel pointLines = new() { Spacing = Tokens.Space8 };
    private readonly TextBlock pointLead = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.HeadlineFigure } };
    private readonly PointBlankGraph pointGraph = new() { Height = 220, IsVisible = false };
    private readonly Dictionary<PointBlankMode, Button> pointModeButtons = [];
    private readonly Button pointKeep = new() { IsVisible = false };
    private readonly Button pointUndo = new() { IsVisible = false };
    private readonly Button pointCard = new() { Content = "Print a card…", IsVisible = false };
    private PointBlankMode pointMode = PointBlankMode.Longest;
    private PointBlankResult? pointResult;
    private PointBlankRequest? pointRequest;

    private void BuildPointBlank(StackPanel left, StackPanel middle)
    {
        pointTarget.ItemTemplate = PointBlankPieces.Template;
        pointTarget.ItemsSource = PointBlankPieces.Choices;
        pointTarget.SelectedIndex = PointBlankTargets.All.ToList().FindIndex(t => t.Name == "Whitetail deer");
        pointTarget.SelectionChanged += (_, _) => ShowPointTarget();
        pointFrom.SelectionChanged += (_, _) => ShowPointFrom();
        pointSession.SelectionChanged += (_, _) => ShowPointFrom();
        Avalonia.Automation.AutomationProperties.SetName(pointTarget, "What you are shooting at");
        Avalonia.Automation.AutomationProperties.SetName(pointRadiusUnit, "Mean radius unit");
        Avalonia.Automation.AutomationProperties.SetName(pointSession, "The saved session");

        var presets = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space4 };
        foreach (string p in new[] { "90", "95", "99" })
        {
            var button = new Button { Content = p + " %", Name = "PointCertainty" + p };
            button.Click += (_, _) => pointCertainty.Text = p;
            presets.Children.Add(button);
        }

        left.Children.Add(Section("point.precision", "Your mean radius", true, () => $"{Or(pointMeanRadius)} {pointRadiusUnit.SelectedItem} · {pointFrom.SelectedItem}",
            SettingRow(FieldLabel("From"), pointFrom),
            pointSession,
            SettingRow(FieldLabel("Your mean radius"), pointMeanRadius, pointRadiusUnit),
            pointFromWords,
            Line(PointBlankReport.FromThePosition)));
        left.Children.Add(Section("point.certainty", "Certainty", true, () => Or(pointCertainty) + " %",
            SettingRow(FieldLabel("Shots in the zone, %"), pointCertainty),
            presets,
            Line("Up to 99.9 %. No shot can be promised, so 100 % is not taken.")));
        left.Children.Add(Section("point.target", "What you are shooting at", true, () => pointTarget.SelectedItem is PointBlankPieces.Choice choice ? choice.Words : "",
            SettingRow(FieldLabel("Target"), pointTarget),
            SettingRow(FieldLabel("Size, width by height"), pointWidth, pointHeight, pointSizeUnit),
            pointSource,
            pointLaw,
            SettingRow(Measured("Energy floor, blank for none", BallisticMeasure.Energy), pointEnergy)));
        left.Children.Add(Section("point.zero", "The zero", false, () => pointMode switch
            {
                PointBlankMode.MostLikely => $"most likely at {Or(pointLikely)} {UnitSettings.Symbol(units.Distance)}",
                PointBlankMode.Window => $"window {Or(pointWindowFrom)} to {Or(pointWindowTo)} {UnitSettings.Symbol(units.Distance)}",
                _ => "the longest dead-on hold",
            },
            SettingRow(Distanced("My most likely distance"), pointLikely),
            SettingRow(Distanced("A window from"), pointWindowFrom),
            SettingRow(Distanced("to"), pointWindowTo)));
        left.Children.Add(Section("point.shot", "The shot", false, () => $"{Or(pointWind)} {Symbol(BallisticMeasure.WindSpeed)} crosswind · {Or(pointAngle, "0")}°",
            SettingRow(Measured("Crosswind, full value", BallisticMeasure.WindSpeed), pointWind),
            SettingRow(FieldLabel("Shot angle, degrees"), pointAngle),
            Line("The wind is not part of point blank range: its drift at the far end is shown beside it.")));

        middle.Children.Add(Heading("Point blank range"));
        middle.Children.Add(Line("How far you can hold dead on and still put the share of shots you choose into what you are shooting at, from your own mean radius and the load's velocity spread. Pick the zero three ways; your rifle's present zero is always shown beside it."));
        var modes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0 };
        foreach (var (mode, words) in new[] { (PointBlankMode.Longest, "Longest dead-on hold"), (PointBlankMode.MostLikely, "My most likely distance"), (PointBlankMode.Window, "A window of distances") })
        {
            var button = new Button { Content = words, Name = "PointMode" + mode };
            button.Click += (_, _) => ChoosePointMode(mode);
            pointModeButtons[mode] = button;
            modes.Children.Add(button);
        }

        middle.Children.Add(modes);
        pointLead.FontSize = Tokens.LeadValueSize;
        pointLead.FontFamily = Mono;
        var card = new Border { Child = new StackPanel { Spacing = Tokens.Space8, Children = { pointLead, pointLines } }, IsVisible = false, Classes = { AppStyles.ResultCard } };
        pointLines.Children.CollectionChanged += (_, _) => card.IsVisible = pointLines.Children.Count > 0;
        middle.Children.Add(card);
        middle.Children.Add(pointGraph);
        pointKeep.Click += (_, _) => KeepPointBlankZero();
        pointUndo.Click += (_, _) => UndoPointBlankZero();
        pointCard.Click += async (_, _) => await SavePointBlankCard();
        middle.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { pointCard, pointKeep, pointUndo } });

        unitBoxes.Add((pointWind, BallisticMeasure.WindSpeed));
        unitBoxes.Add((pointEnergy, BallisticMeasure.Energy));
        unitBoxes.Add((leastExpansion, BallisticMeasure.Speed));
        ChoosePointMode(PointBlankMode.Longest);
        ShowPointTarget();
        ShowPointFrom();
    }

    private void ChoosePointMode(PointBlankMode mode)
    {
        pointMode = mode;
        foreach (var (each, button) in pointModeButtons)
        {
            button.Classes.Set(AppStyles.Chosen, each == mode);
        }

        if (mode != PointBlankMode.Longest && ballisticSections.FirstOrDefault(s => s.Title == "The zero") is { IsOpen: false } zero)
        {
            zero.Open(true, save: false);
        }
    }

    /// <summary>The chosen target's size in the fields, its source under them, and the energy floor its rule of thumb gives.</summary>
    private void ShowPointTarget()
    {
        int i = pointTarget.SelectedIndex;
        var target = i >= 0 && i < PointBlankPieces.Targets.Count ? PointBlankPieces.Targets[i] : null;
        var zone = target?.Zone ?? (i == PointBlankPieces.OwnCircle ? PointBlankZone.Circle(8) : PointBlankZone.Rectangle(8, 12));
        bool circle = zone.Shape == HitShape.Circle;
        pointSizeUnit.Text = UnitSettings.Symbol(units.Linear);
        pointWidth.Text = UnitSettings.FromInches(zone.WidthInches, units.Linear).ToString("0.#", CultureInfo.InvariantCulture);
        pointHeight.Text = UnitSettings.FromInches(zone.HeightInches, units.Linear).ToString("0.#", CultureInfo.InvariantCulture);
        pointHeight.IsVisible = !circle;

        // A sourced size is the source's: an estimate or your own can be changed.
        pointWidth.IsReadOnly = pointHeight.IsReadOnly = target is { IsEstimate: false };
        pointSource.Text = target?.Source ?? "Your own size.";
        pointLaw.IsVisible = target is { Kind: not PointBlankKind.Practical };
        pointEnergy.Text = target?.EnergyFloorFtLb is { } floor ? BallisticMeasures.Text(floor, BallisticMeasure.Energy, units) : "";
    }

    /// <summary>The mean radius from where it is chosen to come from, written into the field, with where it came from under it.</summary>
    private void ShowPointFrom()
    {
        pointSession.IsVisible = pointFrom.SelectedIndex == 2;
        pointMeanRadius.IsReadOnly = pointFrom.SelectedIndex != 0;
        if (pointFrom.SelectedIndex == 0)
        {
            pointFromWords.Text = "Typed, from the position you will shoot from.";
            return;
        }

        if (pointFrom.SelectedIndex == 2)
        {
            var list = sessions?.List() ?? [];
            if (pointSession.ItemsSource is null || pointSession.ItemCount != list.Count)
            {
                pointSession.ItemsSource = list.Select(s => $"{s.SheetName}, {s.ShotDate ?? s.CreatedUtc[..10]}").ToList();
                pointSession.Tag = list.Select(s => s.Id).ToList();
            }
        }

        var state = pointFrom.SelectedIndex == 1 ? session.State
            : pointSession.Tag is List<long> ids && pointSession.SelectedIndex >= 0 && sessions?.Get(ids[pointSession.SelectedIndex]) is { } record
                ? MarkingFile.Read(record.MarkingJson).State : null;
        if (state is null)
        {
            pointFromWords.Text = pointFrom.SelectedIndex == 2 ? "Choose a saved session." : "Open a group in the analysis.";
            return;
        }

        var (group, refusal) = HitFromGroup.Of(state);
        if (group is null)
        {
            pointFromWords.Text = refusal;
            return;
        }

        // Mean radius from the per-axis sigma: MR = sigma times the square root of pi over 2.
        double moa = group.Precision.SigmaMrad * Math.Sqrt(Math.PI / 2) / BallisticSolver.MilPerMoa;
        pointMeanRadius.Text = (pointRadiusUnit.SelectedIndex switch { 1 => moa * BallisticSolver.MilPerMoa, 2 => moa * PointBlankModel.InchesPerMoaPerYard * 100, _ => moa })
            .ToString("0.00", CultureInfo.InvariantCulture);
        pointFromWords.Text = $"From {group.Shots} shots at {units.DistanceText(group.DistanceInches)}.";
    }

    private double? Yards(TextBox box) => Number(box) is { } shown ? UnitSettings.DistanceToInches(shown, units.Distance) / 36 : null;

    /// <summary>Works out the point blank range and shows it, or says which field it still needs.</summary>
    internal void FillPointBlank()
    {
        pointLines.Children.Clear();
        pointLead.Text = "";
        pointGraph.IsVisible = pointCard.IsVisible = pointKeep.IsVisible = false;
        pointResult = null;
        var rifle = ChosenRifle;
        var load = ChosenLoad;
        var missing = SolverUse.Missing(rifle, load);
        void Say(string words) => pointLines.Children.Add(Line(words));
        if (missing.Count > 0)
        {
            Say("The solver needs " + Joined(missing) + ".");
            MarkNeeded(missing);
            return;
        }

        if (Number(pointMeanRadius) is not { } radius)
        {
            Say("Enter your mean radius.");
            return;
        }

        if (!double.TryParse(pointCertainty.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double percent))
        {
            Say("Enter a certainty, for example 90.");
            return;
        }

        var (share, refusal) = PointBlankModel.Certainty(percent);
        if (share is not { } certainty)
        {
            Say(refusal!);
            return;
        }

        int i = pointTarget.SelectedIndex;
        var target = i >= 0 && i < PointBlankPieces.Targets.Count ? PointBlankPieces.Targets[i] : null;
        double? width = Number(pointWidth), height = Number(pointHeight);
        if (width is null || (pointHeight.IsVisible && height is null))
        {
            Say("Enter the size of what you are shooting at.");
            return;
        }

        double w = UnitSettings.ToInches(width.Value, units.Linear), h = pointHeight.IsVisible ? UnitSettings.ToInches(height!.Value, units.Linear) : w;
        var zone = pointHeight.IsVisible ? PointBlankZone.Rectangle(w, h) : PointBlankZone.Circle(w);
        string name = target?.Name ?? (i == PointBlankPieces.OwnCircle ? "Your own circle" : "Your own rectangle");

        var input = SolverUse.Input(rifle, load, Air())! with { AngleDegrees = double.TryParse(pointAngle.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double angle) ? angle : 0 };
        double wind = Number(pointWind) is { } shownWind ? BallisticMeasures.ToImperial(shownWind, BallisticMeasure.WindSpeed, units) : 0;
        var model = new PointBlankModel(input, PointBlankModel.ToMoa(radius, (PointBlankPrecisionUnit)pointRadiusUnit.SelectedIndex), load!.MuzzleVelocitySdFps ?? 0, wind);
        var request = new PointBlankRequest(zone, name, certainty, pointMode, Yards(pointLikely) ?? 200, Yards(pointWindowFrom) ?? 150, Yards(pointWindowTo) ?? 300,
            Number(pointEnergy) is { } floor ? BallisticMeasures.ToImperial(floor, BallisticMeasure.Energy, units) : null, load.MinExpansionFps);
        var result = PointBlankReport.Work(model, request, rifle!, units);
        pointResult = result;
        pointRequest = request;
        DiagnosticLog.Info("ballistics.pointblank", ("mode", pointMode.ToString()), ("certainty", certainty), ("end", result.Ending.EndYards));

        pointLead.Text = result.Headline;
        UnitTap.Attach(pointLead, "Dead on");
        Say(result.Certainty + ".");
        if (result.WindowSays is { } window)
        {
            pointLines.Children.Add(new TextBlock { Text = window, TextWrapping = TextWrapping.Wrap, FontWeight = result.Window is { Reaches: false } ? FontWeight.SemiBold : FontWeight.Normal });
        }

        pointLines.Children.Add(new TextBlock { Text = result.HowToSet, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        Say(result.Ends);
        if (result.Expansion is { } expansion)
        {
            Say(expansion);
        }

        foreach (var hold in result.Holds)
        {
            pointLines.Children.Add(new TextBlock { Text = hold.Says, FontFamily = Mono, Classes = { AppStyles.Secondary } });
        }

        Say(result.Wind + (result.WindWarning is { } warning ? " " + warning : ""));
        Say(result.CurrentZero);
        Say(result.ClassicLine);
        if (target is { Kind: not PointBlankKind.Practical })
        {
            Say(PointBlankTargets.CheckTheLaw);
        }

        Say(PointBlankReport.NotCovered);
        pointGraph.Show(model, zone, result);
        pointGraph.IsVisible = pointCard.IsVisible = true;
        ShowKeep(rifle!);
    }

    /// <summary>Keep and put back, named for what they do to this rifle.</summary>
    private void ShowKeep(Rifle rifle)
    {
        pointKeep.Content = $"Keep this zero on {rifle.Name}";
        pointKeep.IsVisible = pointResult?.FarZeroYards is not null;
        ToolTip.SetTip(pointKeep, pointResult?.KeepSays);
        var previous = rifle.PreviousZeros.FirstOrDefault();
        pointUndo.IsVisible = previous is not null;
        pointUndo.Content = previous?.ZeroDistanceYards is { } yards ? $"Put back the {PointBlankReport.Whole(yards, units)} zero" : "Put back the zero before";
    }

    /// <summary>Entry 455: the chosen zero onto the rifle record as its zero distance, the old one kept to put back.</summary>
    internal void KeepPointBlankZero()
    {
        if (ChosenRifle is not { } rifle || pointResult?.FarZeroYards is not { } far || pointRequest is null)
        {
            return;
        }

        string why = $"point blank, {PointBlankReport.Share(pointRequest.Certainty)} in {pointRequest.ZoneName}";
        book = book.With(rifle.WithZero(Math.Round(far), why, DateTimeOffset.UtcNow));
        SaveBook();
        DiagnosticLog.Info("records.zero.kept", ("from", rifle.ZeroDistanceYards), ("to", Math.Round(far)));
        status.Text = $"{rifle.Name} is now zeroed at {PointBlankReport.Whole(Math.Round(far), units)}; every ballistics figure for it follows.";
        AfterZeroChange(rifle.Name);
    }

    internal void UndoPointBlankZero()
    {
        if (ChosenRifle is not { PreviousZeros.Count: > 0 } rifle)
        {
            return;
        }

        book = book.With(rifle.UndoZero());
        SaveBook();
        status.Text = $"{rifle.Name}'s zero is back as it was.";
        AfterZeroChange(rifle.Name);
    }

    private void AfterZeroChange(string rifle)
    {
        fillingBallistics = true;
        ballisticRifle.ItemsSource = new[] { "No rifle" }.Concat(book.Rifles.Select(r => r.Name)).ToList();
        ballisticRifle.SelectedIndex = book.Rifles.FindIndex(r => r.Name == rifle) + 1;
        fillingBallistics = false;
        ShowBallisticRecords();
        FillPointBlank();
    }

    /// <summary>The card as a 203 dpi picture and a PDF, for the M220's app or any printer at 100 percent, as the scale labels are saved.</summary>
    private async Task SavePointBlankCard()
    {
        if (pointResult is not { } result || pointRequest is not { } request || ChosenRifle is not { } rifle || ChosenLoad is not { } load)
        {
            return;
        }

        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Choose where to save the point blank card", AllowMultiple = false });
        if (folder.FirstOrDefault()?.TryGetLocalPath() is not { } into)
        {
            return;
        }

        var lines = PointBlankCard.Lines(rifle, load, result, request, units, DateOnly.FromDateTime(DateTime.Now));
        var (pngs, pdf) = PrinterAppFiles.Make([PointBlankCard.Page(lines)], 203);
        string stem = System.IO.Path.Combine(into, PointBlankCard.FileName(rifle));
        try
        {
            File.WriteAllBytes(stem + ".png", pngs[0]);
            File.WriteAllBytes(stem + ".pdf", pdf);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            status.Text = "The card could not be saved: " + e.Message;
            return;
        }

        DiagnosticLog.Info("ballistics.pointblank.card", ("saved", true));
        status.Text = $"Saved the {PointBlankCard.WidthMm} by {PointBlankCard.HeightMm} mm card in {into}. Print it at 100 percent, on the M220 from its app or on any printer.";
    }

    /// <summary>What the point blank answer says, for the headless tests.</summary>
    internal IEnumerable<string> PointBlankText => new[] { pointLead.Text ?? "" }.Concat(pointLines.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? ""));

    internal PointBlankResult? PointBlankShown => pointResult;

    internal void SetPointBlank(string meanRadius, string certainty, string target, PointBlankMode mode = PointBlankMode.Longest)
    {
        pointFrom.SelectedIndex = 0;
        pointMeanRadius.Text = meanRadius;
        pointCertainty.Text = certainty;
        pointTarget.SelectedIndex = PointBlankPieces.Targets.ToList().FindIndex(t => t?.Name == target);
        ChoosePointMode(mode);
    }
}
