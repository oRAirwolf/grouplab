using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// "Shots Needed to Zero", NOTES-FROM-PLANNING.md entry 252 sections 3 and 4, suggested by Jylee: in the full figures' disclosure, how many
/// shots a group needs before its centre, rounded to the nearest click, lands on the click nearest the true zero, and within one click of it,
/// at 90, 95 and 99 percent. Worked out off the interface thread, only when the disclosure is open and its inputs change, and kept for the
/// group and click value it was worked out for.
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel shotsToZero = new() { Spacing = Tokens.Space4 };
    private readonly ComboBox shotsClick = new() { ItemsSource = ClickChoices.Select(c => c.Words).ToList(), SelectedIndex = 0, MinWidth = 150 };
    private readonly TextBox shotsClickTyped = new() { Width = 90, IsVisible = false, PlaceholderText = "click" };
    private readonly CheckBox shotsExact = new() { Content = "Treat the measured sigma as exact" };
    private readonly TextBox shotsSeed = new() { Width = 110, Text = "41" };
    private readonly Button shotsAgain = new() { Content = "Calculate again" };
    private readonly Button shotsKeep = new() { Content = "Keep this click value on the rifle" };
    private readonly ShotsCurve shotsCurve = new() { Height = 170, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly Dictionary<string, ShotsToZeroAnswer> shotsCache = [];
    private CancellationTokenSource? shotsWork;
    private MarkingState? shotsState;
    private bool shotsBuilt;

    /// <summary>The click values a scope is usually made with, and a typed one in either unit.</summary>
    private static readonly (string Words, double Value, AngularUnit Unit)[] ClickChoices =
    [
        ("0.1 mil", 0.1, AngularUnit.Mrad), ("1/4 MOA", 0.25, AngularUnit.Moa), ("1/8 MOA", 0.125, AngularUnit.Moa),
        ("Typed, in mil", 0, AngularUnit.Mrad), ("Typed, in MOA", 0, AngularUnit.Moa),
    ];

    /// <summary>The work running now, for the headless tests to wait on.</summary>
    internal Task ShotsToZeroWork { get; private set; } = Task.CompletedTask;

    /// <summary>Scrolls the section into view, for the headless tests' photograph.</summary>
    internal void BringShotsToZeroIntoView() => shotsToZero.BringIntoView();

    internal IEnumerable<string> ShotsToZeroText => shotsToZero.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    private void BuildShotsToZero()
    {
        if (shotsBuilt)
        {
            return;
        }

        shotsBuilt = true;
        shotsClick.SelectionChanged += (_, _) =>
        {
            shotsClickTyped.IsVisible = ClickChoices[Math.Max(0, shotsClick.SelectedIndex)].Value == 0;
            RefreshShotsToZero();
        };
        shotsClickTyped.TextChanged += (_, _) => RefreshShotsToZero();
        shotsExact.IsCheckedChanged += (_, _) => RefreshShotsToZero();
        shotsSeed.TextChanged += (_, _) => RefreshShotsToZero();
        shotsAgain.Click += (_, _) => shotsSeed.Text = Random.Shared.Next(1, 100_000).ToString(CultureInfo.InvariantCulture);
        shotsKeep.Click += (_, _) => KeepClickOnRifle();
        fullFiguresPanel.Expanded += (_, _) => RefreshShotsToZero();
    }

    /// <summary>The section, at the end of the full figures, for the marking <paramref name="state"/>.</summary>
    private void ShowShotsToZero(MarkingState state)
    {
        BuildShotsToZero();
        shotsState = state;
        fullFigures.Children.Add(FieldLabel("Shots Needed to Zero"));
        fullFigures.Children.Add(shotsToZero);
        RefreshShotsToZero();
    }

    /// <summary>The click value the section uses: the rifle's own, or the one chosen here.</summary>
    private (double Value, AngularUnit Unit, string Words, bool FromRifle)? ShotsClick()
    {
        if (shotsState?.Rifle is { ClickValue: > 0 } rifle)
        {
            return (rifle.ClickValue, rifle.ClickUnit, $"{rifle.ClickValue.ToString("0.###", CultureInfo.InvariantCulture)} {(rifle.ClickUnit == AngularUnit.Mrad ? "mil" : "MOA")}, from {rifle.Name}", true);
        }

        var choice = ClickChoices[Math.Max(0, shotsClick.SelectedIndex)];
        double value = choice.Value > 0 ? choice.Value
            : double.TryParse(shotsClickTyped.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double typed) && typed > 0 ? typed : 0;
        return value > 0 ? (value, choice.Unit, $"{value.ToString("0.###", CultureInfo.InvariantCulture)} {(choice.Unit == AngularUnit.Mrad ? "mil" : "MOA")}", false) : null;
    }

    private void RefreshShotsToZero()
    {
        shotsToZero.Children.Clear();
        if (shotsState is not { } state || !fullFiguresPanel.IsExpanded)
        {
            return;
        }

        var click = ShotsClick();
        if (click is not { FromRifle: true })
        {
            shotsToZero.Children.Add(ShotsRow(Detail("Your scope's click"), shotsClick, shotsClickTyped));
        }

        if (Zeroing.For(state) is not { } zero)
        {
            shotsToZero.Children.Add(Line($"Needs at least {GroupAnalysis.MinimumShotsForDispersion} shots on bulls or a point of aim, so the group's own spread is known."));
            return;
        }

        if (state.ShotDistanceInches is not { } distance || distance <= 0)
        {
            shotsToZero.Children.Add(Line("Enter the shot distance on the marking to work this out: a click is an angle, and the group's spread becomes one only at a distance."));
            return;
        }

        if (click is not { } c)
        {
            shotsToZero.Children.Add(Line("Choose your scope's click value to work this out."));
            return;
        }

        double sigmaClicks = Angular.Constant(c.Unit) / 2 * Math.Atan(zero.SigmaInches / distance) / c.Value;
        int seed = int.TryParse(shotsSeed.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : 41;
        bool exact = shotsExact.IsChecked == true;
        string key = string.Create(CultureInfo.InvariantCulture, $"{sigmaClicks:R}|{zero.DegreesOfFreedom}|{exact}|{seed}");
        string from = string.Create(CultureInfo.InvariantCulture,
            $"From the group's sigma of {units.Length(zero.SigmaInches)} an axis, measured on {zero.Shots} shots, which is {sigmaClicks:0.0#} clicks at this distance with clicks of {c.Words}.");
        if (shotsCache.TryGetValue(key, out var answer))
        {
            ShowShotsAnswer(answer, from, c.FromRifle);
            return;
        }

        shotsToZero.Children.Add(Line("Working it out."));
        shotsWork?.Cancel();
        var work = new CancellationTokenSource();
        shotsWork = work;
        int df = zero.DegreesOfFreedom;
        ShotsToZeroWork = Task.Run(() => ShotsToZero.Work(sigmaClicks, df, exact, seed, cancellation: work.Token), work.Token).ContinueWith(done =>
        {
            if (done.IsCompletedSuccessfully)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    shotsCache[key] = done.Result;
                    if (!work.IsCancellationRequested)
                    {
                        shotsToZero.Children.Clear();
                        if (!c.FromRifle)
                        {
                            shotsToZero.Children.Add(ShotsRow(Detail("Your scope's click"), shotsClick, shotsClickTyped));
                        }

                        ShowShotsAnswer(done.Result, from, c.FromRifle);
                    }
                });
            }
        }, TaskScheduler.Default);
    }

    private void ShowShotsAnswer(ShotsToZeroAnswer answer, string from, bool fromRifle)
    {
        static string Count(int? n) => n switch { null => "more than 1,000 shots", 1 => "1 shot", { } shots => shots.ToString("N0", CultureInfo.InvariantCulture) + " shots" };
        static string Goals(ShotsFor f) => $"90 percent at {Count(f.Ninety)}, 95 at {Count(f.NinetyFive)}, 99 at {Count(f.NinetyNine)}";
        shotsToZero.Children.Add(Line(from));
        shotsToZero.Children.Add(new TextBlock { Text = $"On the closest click, both axes: {Goals(answer.ClosestClick)}.", TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        shotsToZero.Children.Add(new TextBlock { Text = $"Within 1 click, both axes: {Goals(answer.WithinOneClick)}.", TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        shotsToZero.Children.Add(Detail($"Windage or elevation alone: the closest click {Goals(answer.ClosestClickOneAxis)}; within 1 click {Goals(answer.WithinOneClickOneAxis)}."));
        shotsToZero.Children.Add(Note("The closest click comes slowly: where the true zero lies near the line between two clicks, only a very large group tells which side it is on, so its 95 and 99 percent often need more than 1,000 shots. Within one click comes far sooner, and is what most zeroing needs."));
        shotsCurve.Points = answer.Curve;
        shotsCurve.InvalidateVisual();
        shotsToZero.Children.Add(shotsCurve);
        shotsToZero.Children.Add(ShotsRow(shotsExact));
        if (answer.ExactSigma)
        {
            shotsToZero.Children.Add(Detail("Worked out exactly, with the measured sigma taken as known, so no simulation and no seed."));
        }
        else
        {
            string moves = answer.ShotsCanMove == 0
                ? "another seed would not move these counts"
                : $"another seed could move a count by up to {answer.ShotsCanMove} shot{(answer.ShotsCanMove == 1 ? "" : "s")}, because the trials are random";
            shotsToZero.Children.Add(Detail(string.Create(CultureInfo.InvariantCulture,
                $"{answer.Trials:N0} trials of the sigma the group could really have, seed {answer.Seed}; {moves}. The same seed gives the same answer.")));
            shotsToZero.Children.Add(ShotsRow(Detail("Seed"), shotsSeed, shotsAgain));
        }

        if (!fromRifle && shotsState?.Rifle is { } rifle && book.Rifles.Any(r => r.Name == rifle.Name))
        {
            shotsToZero.Children.Add(shotsKeep);
        }
    }

    /// <summary>Remembers the chosen click value on the marking's rifle, when the shooter asks.</summary>
    private void KeepClickOnRifle()
    {
        if (shotsState?.Rifle is not { } marked || book.Rifles.FirstOrDefault(r => r.Name == marked.Name) is not { } rifle || ShotsClick() is not { } click)
        {
            return;
        }

        book = book.With(rifle with { ClickValue = click.Value, ClickUnit = click.Unit });
        SaveBook();
        status.Text = $"Kept a click of {click.Words} on {rifle.Name}.";
    }

    private static StackPanel ShotsRow(params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8 };
        foreach (var child in children)
        {
            // The choices are kept between refreshes, so each leaves the row it was last in.
            (child.Parent as Panel)?.Children.Remove(child);
            child.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(child);
        }

        return row;
    }
}

/// <summary>The chance against shots, both axes, for the closest click and for within one click, on a scale of shots from 1 to 1,000.</summary>
internal sealed class ShotsCurve : Control
{
    public IReadOnlyList<(int Shots, double ClosestClick, double WithinOneClick)> Points { get; set; } = [];

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));
        if (Points.Count < 2)
        {
            return;
        }

        double left = 44, right = Bounds.Width - 12, top = 26, bottom = Bounds.Height - 22;
        double X(int shots) => left + ((right - left) * Math.Log10(shots) / 3);
        double Y(double p) => bottom - ((bottom - top) * p);
        var dim = new SolidColorBrush(palette.Dim);
        foreach (double p in new[] { 0.5, 0.9, 1.0 })
        {
            context.DrawLine(new Pen(new SolidColorBrush(palette.Line), 1), new Point(left, Y(p)), new Point(right, Y(p)));
            if (p < 1)
            {
                context.DrawText(new FormattedText($"{100 * p:0}%", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, dim), new Point(4, Y(p) - 8));
            }
        }

        foreach (int n in new[] { 1, 10, 100, 1000 })
        {
            context.DrawText(new FormattedText(n.ToString("N0", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, dim), new Point(X(n) - 8, bottom + 4));
        }

        void Curve(Func<(int Shots, double ClosestClick, double WithinOneClick), double> value, Color colour)
        {
            var pen = new Pen(new SolidColorBrush(colour), 2);
            for (int i = 1; i < Points.Count; i++)
            {
                context.DrawLine(pen, new Point(X(Points[i - 1].Shots), Y(value(Points[i - 1]))), new Point(X(Points[i].Shots), Y(value(Points[i]))));
            }
        }

        Curve(p => p.WithinOneClick, palette.Teal);
        Curve(p => p.ClosestClick, palette.Amber);
        context.DrawText(new FormattedText("amber: closest click    teal: within 1 click", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.DetailSize, dim), new Point(left, 4));
    }
}
