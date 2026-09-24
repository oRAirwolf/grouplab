using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Theme;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// The analysis screen's last two unbuilt parts, NOTES-FROM-PLANNING.md entry 113 section 1: the sheet's thumbnail, drawn from its definition
/// with every shot on it, where a click on a bull selects its shots; and the full CEP table with the bivariate fit, behind one disclosure that
/// remembers it was opened, as DESIGN.md section 19 describes. Everything in the panel was already in the engine; this is layout.
/// </summary>
public sealed partial class MainWindow
{
    private readonly SheetThumbnail thumbnail = new();
    private readonly StackPanel fullFigures = new() { Spacing = Tokens.Space4 };
    private readonly Expander fullFiguresPanel = new() { Header = "Full CEP table and the fitted ellipse", HorizontalAlignment = HorizontalAlignment.Stretch };
    private TargetDefinition? thumbnailDefinition;

    /// <summary>The name the panel's open state is remembered by, with the "why" disclosures.</summary>
    private const string FullFiguresItem = "full-figures";

    /// <summary>Puts the thumbnail at the head of the left column and the panel after the figures' disclosures; called once, by the constructor.</summary>
    private void BuildFigureExtras(StackPanel shotsColumn, StackPanel figures)
    {
        thumbnail.ShotsClicked += (_, ids) => PickShots(ids);
        ToolTip.SetTip(thumbnail, "The sheet from its definition, with every shot. Click a bull to select its shots.");
        shotsColumn.Children.Insert(0, thumbnail);
        fullFiguresPanel.Content = fullFigures;
        fullFiguresPanel.IsExpanded = WhyOpen(FullFiguresItem);
        fullFiguresPanel.Expanded += (_, _) => RememberFullFigures(true);
        fullFiguresPanel.Collapsed += (_, _) => RememberFullFigures(false);
        // Entry 169 section 1: the full tables sit with the flags, in the Advanced section.
        var holder = (Panel)flags.Parent!;
        holder.Children.Insert(holder.Children.IndexOf(flags) + 1, fullFiguresPanel);
    }

    private void RememberFullFigures(bool open)
    {
        whyOpen[FullFiguresItem] = open;
        settingsStore.SaveWhyOpen(FullFiguresItem, open);
    }

    /// <summary>
    /// The thumbnail's page, bulls and shots. Each shot sits at its bull's centre from the definition plus its offset from that bull, the
    /// same offset the plot draws, so the thumbnail needs no photograph; a marking with no definition has no thumbnail.
    /// </summary>
    private void ShowThumbnail(MarkingState state)
    {
        if (plotDefinition is not { } definition || state.Scale is null)
        {
            thumbnail.IsVisible = false;
            return;
        }

        thumbnail.IsVisible = true;
        if (!ReferenceEquals(definition, thumbnailDefinition))
        {
            (thumbnail.Artwork as IDisposable)?.Dispose();
            thumbnail.Artwork = PrintWindow.Preview(definition);
            thumbnailDefinition = definition;
        }

        thumbnail.PageWidthInches = definition.Page.Width / 254.0;
        thumbnail.PageHeightInches = definition.Page.Height / 254.0;
        var outer = definition.RingSets.ToDictionary(r => r.Key, r => r.Discs.Count > 0 ? r.Discs.Max(d => d.Diameter) / 508.0 : 0.2);
        thumbnail.Bulls = [.. definition.Bulls.Select((b, i) => new ThumbnailBull(i, new PointD(b.X / 254.0, b.Y / 254.0), outer.TryGetValue(b.RingSet, out double r) ? r : 0.2))];

        var shots = state.Shots.Where(s => s.IsShot && s.Bull is { } b && b < definition.Bulls.Count).ToList();
        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        thumbnail.Shots = offsets.Count == shots.Count
            ? [.. shots.Select((s, i) => new ThumbnailShot(s.Id, new PointD((definition.Bulls[s.Bull!.Value].X / 254.0) + offsets[i].X, (definition.Bulls[s.Bull.Value].Y / 254.0) + offsets[i].Y), s.Exclusion is not null))]
            : [];
        thumbnail.Selected = plotSelection;
        thumbnail.InvalidateVisual();
    }

    /// <summary>
    /// The full CEP table and the bivariate fit, for every shot the figures count and again without exclusions when there are any, so the
    /// panel keeps the rule every figure keeps (STATISTICS.md section 10).
    /// </summary>
    private void ShowFullFigures(MarkingState state)
    {
        fullFigures.Children.Clear();
        var shots = GroupShots(state);
        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        fullFiguresPanel.IsVisible = offsets.Count == shots.Count && offsets.Count >= GroupAnalysis.MinimumShotsForDispersion;
        if (!fullFiguresPanel.IsVisible)
        {
            return;
        }

        FullFigures(offsets, $"Every shot, {offsets.Count}");
        var kept = offsets.Where((_, i) => shots[i].Exclusion is null).ToList();
        if (kept.Count < offsets.Count)
        {
            if (kept.Count >= GroupAnalysis.MinimumShotsForDispersion)
            {
                FullFigures(kept, $"Without exclusions, {kept.Count}");
            }
            else
            {
                fullFigures.Children.Add(Line($"Without exclusions there are {kept.Count} shots, too few for these figures."));
            }
        }

        fullFigures.Children.Add(Note("The circular estimate assumes the group round and gives its interval. The correlated normal and Grubbs-Patnaik estimates follow the group's own shape, so they allow for a group that is not round, and have no interval here. All three are about the group's own center."));
    }

    private void FullFigures(IReadOnlyList<PointD> offsets, string heading)
    {
        fullFigures.Children.Add(FieldLabel(heading));
        var rayleigh = GroupStatistics.Rayleigh(offsets);
        var (xx, xy, yy) = GroupStatistics.Covariance(offsets);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("44,*,*,*") };
        void Cell(string text, int row, int column, bool heading)
        {
            var cell = new TextBlock
            {
                Text = text,
                FontFamily = heading ? Tokens.Sans : Mono,
                FontSize = Tokens.DetailSize,
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Right,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, Tokens.Space8, 2),
                Classes = { heading ? AppStyles.Dim : AppStyles.Secondary },
            };
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, column);
            grid.Children.Add(cell);
        }

        double[] levels = [0.5, 0.9, 0.95, 0.99];
        grid.RowDefinitions = new RowDefinitions(string.Join(",", Enumerable.Repeat("Auto", levels.Length + 1)));
        string[] heads = ["CEP", "circular, 95% interval", "correlated normal", "Grubbs-Patnaik"];
        for (int c = 0; c < heads.Length; c++)
        {
            Cell(heads[c], 0, c, heading: true);
        }

        for (int r = 0; r < levels.Length; r++)
        {
            double q = levels[r];
            var circular = rayleigh.Cep(q);
            Cell(string.Create(CultureInfo.InvariantCulture, $"{100 * q:0}"), r + 1, 0, heading: false);
            Cell($"{units.Number(circular.Value)} ({units.Number(circular.Lower)} to {units.Number(circular.Upper)})", r + 1, 1, heading: false);
            Cell(units.Number(GroupStatistics.CepCorrNormal(xx, xy, yy, q)), r + 1, 2, heading: false);
            Cell(units.Number(GroupStatistics.CepGrubbsPatnaik(xx, xy, yy, q)), r + 1, 3, heading: false);
        }

        fullFigures.Children.Add(grid);

        // The bivariate fit: the centre and each axis's spread with their intervals and the correlation, in the view's own axes as every figure
        // on the screen is, and the error ellipse, whose angle is given as the shape card gives it.
        var shown = offsets.Select(AsDisplayed).ToList();
        var (cx, cy) = GroupStatistics.CentreIntervals(shown);
        var (sx, sy) = GroupStatistics.AxisSd(shown);
        var (dxx, dxy, dyy) = GroupStatistics.Covariance(shown);
        var ellipse = GroupStatistics.ConfidenceEllipse(offsets, 0.95);
        double correlation = dxx > 0 && dyy > 0 ? -dxy / Math.Sqrt(dxx * dyy) : 0;
        string unit = UnitSettings.Symbol(units.Linear);
        foreach (string line in new[]
        {
            $"Center {units.Number(cx.Value)} across ({units.Number(cx.Lower)} to {units.Number(cx.Upper)}), {units.Number(-cy.Value)} up ({units.Number(-cy.Upper)} to {units.Number(-cy.Lower)}) {unit}, 95% intervals",
            $"sd across {units.Length(sx.Value)} ({units.Number(sx.Lower)} to {units.Number(sx.Upper)}), sd up and down {units.Length(sy.Value)} ({units.Number(sy.Lower)} to {units.Number(sy.Upper)})",
            string.Create(CultureInfo.InvariantCulture, $"correlation across with up {correlation:0.00}; error ellipse sd {units.Number(Math.Sqrt(ellipse.Shape.Major))} by {units.Length(Math.Sqrt(ellipse.Shape.Minor))}, major axis at {DisplayedAngle(ellipse.Shape.AngleDegrees):0} degrees"),
            $"95% of shots fall in an ellipse {units.Number(2 * ellipse.SemiMajor)} by {units.Length(2 * ellipse.SemiMinor)} across its axes",
        })
        {
            fullFigures.Children.Add(Detail(line));
        }
    }

    /// <summary>The settings store, for the headless tests.</summary>
    internal AppSettingsStore SettingsStore => settingsStore;

    /// <summary>The thumbnail, for the headless tests.</summary>
    internal SheetThumbnail Thumbnail => thumbnail;

    /// <summary>The full figures panel, for the headless tests.</summary>
    internal Expander FullFiguresPanel => fullFiguresPanel;

    internal IEnumerable<string> FullFiguresText => fullFigures.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}
