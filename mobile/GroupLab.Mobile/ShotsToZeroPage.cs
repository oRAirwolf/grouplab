using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 259 screen 3, "its own page, A" (Alan's choice), suggested by Jylee: a card with the click value as chips
/// (0.1 mil, 1/4 MOA, 1/8 MOA, Other), "Your spread" from the open result with its shot count, and "Adjusting" as chips (Both ways, Up and
/// down); then a table of 90, 95 and 99 in 100 against "Within 1 click" (amber) and "Closest click"; the chance against shots on a log axis,
/// both curves; one sentence on why the closest click takes so many more shots; and the seed and the draws, with Calculate again. The same
/// core as the desktop (<see cref="ShotsToZero"/>), worked out off the interface thread with progress and Cancel.
/// </summary>
internal sealed class ShotsToZeroPage : UserControl
{
    /// <summary>Entry 294 section 1: the desktop's list (0.1 mil, 0.05 mil, 1/4 MOA, 1/8 MOA) and Other, typed in mil or MOA.</summary>
    private static readonly (string Words, double Value, AngularUnit Unit)[] Clicks = [.. ScopeClicks.Common, ("Other", 0, AngularUnit.Moa)];

    private readonly MarkingState state;
    private UnitSettings units;
    private readonly StackPanel column = new() { Spacing = 12 };
    private readonly StackPanel answer = new() { Spacing = 10 };
    private readonly TextBox typed = Screens.Numeric(new() { PlaceholderText = "click, e.g. 0.2", MinHeight = Screens.Touch, Width = 120 });
    private readonly TextBox seed = Screens.Numeric(new() { Text = "41", MinHeight = Screens.Touch, Width = 90 });
    private readonly ShotsCurve curve = new() { Height = 200 };
    private int click;
    private bool typedMil;
    private bool bothWays = true;
    private CancellationTokenSource? work;

    public ShotsToZeroPage(MarkingState state, UnitSettings units, Action back)
    {
        // Entry 273: a tap on any number switches units everywhere; this page shows them again.
        void Follow() => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.units = Phone.Settings.LoadUnits();
            Build();
        });
        AttachedToVisualTree += (_, _) => Shell.UnitsChanged += Follow;
        DetachedFromVisualTree += (_, _) => Shell.UnitsChanged -= Follow;

        this.state = state;
        this.units = units;
        if (state.Rifle is { ClickValue: > 0 } rifle)
        {
            int match = ScopeClicks.IndexOf(rifle.ClickValue, rifle.ClickUnit);
            click = match >= 0 ? match : Clicks.Length - 1;
            if (match < 0)
            {
                typed.Text = rifle.ClickValue.ToString("0.###", CultureInfo.InvariantCulture);
                typedMil = rifle.ClickUnit == AngularUnit.Mrad;
            }
        }
        else
        {
            // Entry 294 section 1: with no rifle, the scope unit in Settings offers its usual click first.
            var usual = ScopeClicks.Usual(units.Aiming(state.Rifle).Angular);
            click = Math.Max(0, ScopeClicks.IndexOf(usual.Value, usual.Unit));
            typedMil = usual.Unit == AngularUnit.Mrad;
        }

        column.Children.Add(Screens.Title("Shots Needed to Zero"));
        column.Children.Add(Screens.Dim("Suggested by Jylee."));
        column.Children.Add(new ContentControl());
        column.Children.Add(answer);
        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
        Build();
    }

    private void Build()
    {
        var setup = new StackPanel { Spacing = 10 };
        setup.Children.Add(Screens.Dim("Your scope's click"));
        var chips = new WrapPanel();
        for (int i = 0; i < Clicks.Length; i++)
        {
            int which = i;
            chips.Children.Add(Chip(Clicks[i].Words, click == i, () =>
            {
                click = which;
                Build();
            }));
        }

        setup.Children.Add(chips);
        if (Clicks[click].Value == 0)
        {
            var unit = new WrapPanel { Children = { Screens.Detach(typed), Chip("mil", typedMil, () => { typedMil = true; Build(); }), Chip("MOA", !typedMil, () => { typedMil = false; Build(); }) } };
            typed.LostFocus -= Typed;
            typed.LostFocus += Typed;
            setup.Children.Add(unit);
        }

        var zero = Zeroing.For(state);
        setup.Children.Add(Screens.Dim("Your spread"));
        setup.Children.Add(Screens.Line(zero is null
            ? $"Needs at least {GroupAnalysis.MinimumShotsForDispersion} shots on bulls or a point of aim, so the group's own spread is known."
            : $"Sigma {units.Length(zero.SigmaInches)} an axis, from {zero.Shots} shots."));
        setup.Children.Add(Screens.Dim("Adjusting"));
        setup.Children.Add(new WrapPanel
        {
            Children =
            {
                Chip("Both ways", bothWays, () => { bothWays = true; Build(); }),
                Chip("Up and down", !bothWays, () => { bothWays = false; Build(); }),
            },
        });
        column.Children[2] = Screens.Card(setup);
        Calculate();
    }

    private void Typed(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Calculate();

    private (double Value, AngularUnit Unit)? Click() => Clicks[click].Value > 0
        ? (Clicks[click].Value, Clicks[click].Unit)
        : Screens.Read(typed.Text) is { } v && v > 0 ? (v, typedMil ? AngularUnit.Mrad : AngularUnit.Moa) : null;

    private void Calculate()
    {
        answer.Children.Clear();
        work?.Cancel();
        if (Zeroing.For(state) is not { } zero)
        {
            return;
        }

        if (state.ShotDistanceInches is not { } distance || distance <= 0)
        {
            answer.Children.Add(Screens.Line("Enter the distance on Capture to work this out: a click is an angle, and the group's spread becomes one only at a distance."));
            return;
        }

        if (Click() is not { } c)
        {
            answer.Children.Add(Screens.Line("Type your scope's click value to work this out."));
            return;
        }

        double sigmaClicks = ShotsToZero.SigmaClicks(zero.SigmaInches, distance, c.Unit, c.Value);
        int chosenSeed = int.TryParse(seed.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : 41;
        var bar = new ProgressBar { IsIndeterminate = true, MinHeight = 6 };
        var cancel = Screens.Choice("Cancel", () => work?.Cancel());
        answer.Children.Add(Screens.Card(Screens.Line("Working it out."), bar, cancel));
        var token = new CancellationTokenSource();
        work = token;
        int df = zero.DegreesOfFreedom;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        _ = Task.Run(() => ShotsToZero.Work(sigmaClicks, df, false, chosenSeed, cancellation: token.Token), token.Token).ContinueWith(done =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested || !done.IsCompletedSuccessfully)
                {
                    answer.Children.Clear();
                    answer.Children.Add(Screens.Line("Canceled."));
                    answer.Children.Add(Screens.Choice("Calculate again", Calculate));
                    return;
                }

                DiagnosticLog.Info("phone.shotstozero", ("ms", clock.ElapsedMilliseconds), ("shots", zero.Shots));
                Show(done.Result);
            });
        }, TaskScheduler.Default);
    }

    private void Show(ShotsToZeroAnswer result)
    {
        answer.Children.Clear();
        static string Count(int? n) => n switch { null => "over 1,000", { } shots => shots.ToString("N0", CultureInfo.CurrentCulture) };
        var within = bothWays ? result.WithinOneClick : result.WithinOneClickOneAxis;
        var closest = bothWays ? result.ClosestClick : result.ClosestClickOneAxis;
        var table = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,*,*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), RowSpacing = 8 };
        void Cell(string text, int row, int col, bool amber = false, bool dim = false, bool teal = false)
        {
            var block = dim ? Screens.Dim(text) : Screens.Line(text);
            if (amber)
            {
                block.Foreground = new SolidColorBrush(Color.FromRgb(232, 150, 46));
                block.FontWeight = FontWeight.SemiBold;
            }

            // Entry 269: the closest click is teal everywhere, as the chart draws it.
            if (teal)
            {
                block.Foreground = new SolidColorBrush(Color.FromRgb(42, 157, 143));
            }

            if (col > 0)
            {
                block.HorizontalAlignment = HorizontalAlignment.Right;
            }

            Grid.SetRow(block, row);
            Grid.SetColumn(block, col);
            table.Children.Add(block);
        }

        Cell("Shots for", 0, 0, dim: true);
        Cell("90 in 100", 0, 1, dim: true);
        Cell("95", 0, 2, dim: true);
        Cell("99", 0, 3, dim: true);
        Cell("Within 1 click", 1, 0, amber: true);
        Cell(Count(within.Ninety), 1, 1, amber: true);
        Cell(Count(within.NinetyFive), 1, 2, amber: true);
        Cell(Count(within.NinetyNine), 1, 3, amber: true);
        Cell("Closest click", 2, 0, teal: true);
        Cell(Count(closest.Ninety), 2, 1, teal: true);
        Cell(Count(closest.NinetyFive), 2, 2, teal: true);
        Cell(Count(closest.NinetyNine), 2, 3, teal: true);
        answer.Children.Add(Screens.Card(table));

        curve.Points = result.Curve;
        curve.InvalidateVisual();
        answer.Children.Add(Screens.Card(Screens.Dim("The chance against shots, both ways: amber within 1 click, teal the closest click."), Screens.Detach(curve)));
        answer.Children.Add(Screens.Line("The closest click comes slowly: where the true zero lies near the line between two clicks, only a very large group tells which side it is on. Within one click comes far sooner, and is what most zeroing needs."));
        string moves = result.ShotsCanMove == 0 ? "another seed would not move these counts" : $"another seed could move a count by up to {result.ShotsCanMove}";
        answer.Children.Add(Screens.Dim(string.Create(CultureInfo.CurrentCulture, $"{result.Trials:N0} draws of the spread the group could really have, seed {result.Seed}; {moves}.")));
        answer.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { Screens.Dim("Seed"), Screens.Detach(seed) } });
        answer.Children.Add(Screens.Primary("Calculate again", Calculate));
    }

    private static Button Chip(string words, bool on, Action chosen)
    {
        var chip = new Button { Content = words, MinHeight = Screens.Touch, MinWidth = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
        if (on)
        {
            chip.Classes.Add(GroupLab.App.Theme.AppStyles.Chosen);
        }

        chip.Click += (_, _) => chosen();
        return chip;
    }
}
