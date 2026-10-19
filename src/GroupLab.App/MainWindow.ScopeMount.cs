using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// The rifle page's Scope and mount part, NOTES-FROM-PLANNING.md entry 456, from Alan's pissinhot.com calculators: rail cant (the elevation the
/// zero uses, the most cant that still zeroes, what is left and how far it reaches with a load), the sight scale factor worked out from a
/// tracking test, and Bryan Litz's Top Gun figure for each load. The words are Core's, the same on the phone.
/// </summary>
public sealed partial class MainWindow
{
    private Control ScopeAndMount(Rifle rifle)
    {
        var part = new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, Tokens.Space16, 0, 0) };
        part.Children.Add(new TextBlock { Text = PartHeading, Classes = { AppStyles.Section } });

        // The figures need a load's flight: the first that the solver can use, or one chosen.
        var usable = book.Loads.Where(l => SolverUse.Missing(rifle, l).Count == 0).ToList();
        var figures = new StackPanel { Spacing = Tokens.Space4 };
        void Show(Load? load)
        {
            figures.Children.Clear();
            if (rifle.ScopeTravel is not > 0)
            {
                figures.Children.Add(Line("Enter the scope's elevation travel above to see the elevation the zero uses and the most rail cant that still zeroes."));
                return;
            }

            if (load is null)
            {
                figures.Children.Add(Line(usable.Count == 0 && SolverUse.Missing(rifle, null) is var missing && missing.Count > 1
                    ? "The figures need " + Joined([.. missing.Where(m => !m.Contains("load", StringComparison.Ordinal))]) + ", and a load with its velocity and BC."
                    : "The figures need a load with its velocity and BC."));
                return;
            }

            var flight = BallisticSolver.Solve(SolverUse.Input(rifle, load, new AirInput())!, ScopeMount.TravelLooked, 25);
            var worked = ScopeMount.Figures(rifle, flight.ZeroAngleMoa, flight)!;
            foreach (string line in ScopeMount.Lines(rifle, load.Name, worked, units))
            {
                figures.Children.Add(new TextBlock { Text = line, TextWrapping = Avalonia.Media.TextWrapping.Wrap, FontWeight = worked.CannotZero && line.Contains("cannot", StringComparison.Ordinal) ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal });
            }
        }

        if (usable.Count > 1)
        {
            var choose = new ComboBox { ItemsSource = usable.Select(l => l.Name).ToList(), SelectedIndex = 0, MinWidth = 220 };
            Avalonia.Automation.AutomationProperties.SetName(choose, "The load the figures are for");
            choose.SelectionChanged += (_, _) => Show(usable.ElementAtOrDefault(choose.SelectedIndex));
            part.Children.Add(Row(Line("With"), choose));
        }

        Show(usable.FirstOrDefault());
        part.Children.Add(figures);
        part.Children.Add(Line("The zero's elevation is GroupLab's own launch angle from its solver, where a vacuum calculation adds a drop angle from the time of flight; the solver's includes the bullet slowing."));
        part.Children.Add(TrackingTest(rifle));
        part.Children.Add(TopGunLines(rifle));
        return part;
    }

    /// <summary>Entry 456 section 2: the sight scale factor worked out from a tall-target tracking test, and put into the field above.</summary>
    private Control TrackingTest(Rifle rifle)
    {
        bool mil = rifle.ClickUnit == AngularUnit.Mrad;
        var unit = new ComboBox { ItemsSource = new[] { "mil", "MOA" }, SelectedIndex = mil ? 0 : 1, MinWidth = 90 };
        var expected = Field((mil ? ScopeTracking.ExpectedMil : ScopeTracking.ExpectedMoa).ToString("0.0", CultureInfo.InvariantCulture));
        var reading = Field();
        var said = new StackPanel { Spacing = Tokens.Space4 };
        Avalonia.Automation.AutomationProperties.SetName(unit, "The test's unit");
        Avalonia.Automation.AutomationProperties.SetName(expected, "Expected, the lines' distance apart");
        Avalonia.Automation.AutomationProperties.SetName(reading, "The turret's reading");
        unit.SelectionChanged += (_, _) => expected.Text = (unit.SelectedIndex == 0 ? ScopeTracking.ExpectedMil : ScopeTracking.ExpectedMoa).ToString("0.0", CultureInfo.InvariantCulture);
        var work = Button("Work it out", () =>
        {
            said.Children.Clear();
            if (Number(expected) is not { } e || Number(reading) is not { } r)
            {
                said.Children.Add(Line("Enter the expected angle and the turret's reading."));
                return;
            }

            var result = ScopeTracking.Work(e, r);
            said.Children.Add(new TextBlock
            {
                Text = string.Create(CultureInfo.InvariantCulture, $"Sight scale factor {result.ScaleFactor:0.000}, an error of {result.ErrorPercent:+0.00;-0.00;0.00} %."),
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
            });
            said.Children.Add(Line(result.Verdict));
            said.Children.Add(Button("Use this factor", () =>
            {
                if (equipmentFields.TryGetValue("sightScaleFactor", out var box) && box is TextBox field)
                {
                    field.Text = result.ScaleFactor.ToString("0.000", CultureInfo.InvariantCulture);
                    status.Text = "The factor is in the form above; Save keeps it.";
                }
            }));
        });
        var body = new StackPanel
        {
            Spacing = Tokens.Space4,
            Children =
            {
                Line(ScopeTracking.Steps),
                Row(FieldLabel("Unit"), unit, FieldLabel("Expected"), expected, FieldLabel("Turret reading"), reading, work),
                said,
            },
        };
        return new Expander { Header = "Work out the sight scale factor from a tracking test", Content = body, Margin = new Thickness(0, Tokens.Space8, 0, 0) };
    }

    /// <summary>Entry 456 section 3: Top Gun for each load with what it needs, never as a pass or a fail.</summary>
    private Control TopGunLines(Rifle rifle)
    {
        var part = new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, Tokens.Space8, 0, 0) };
        part.Children.Add(new TextBlock { Text = "Top Gun", Classes = { AppStyles.Label } });
        if (rifle.WeightAsFiredLb is not > 0)
        {
            part.Children.Add(Line("Enter the rifle's weight as fired above to see Bryan Litz's Top Gun figure for each load."));
            return part;
        }

        int shown = 0;
        foreach (var load in book.Loads)
        {
            if (TopGun.For(rifle, load) is { } moa)
            {
                part.Children.Add(Line(TopGun.Says(moa, load.Name)));
                shown++;
            }
        }

        if (shown == 0)
        {
            part.Children.Add(Line("No load has its bullet weight and velocity yet."));
        }

        part.Children.Add(Line(TopGunNote));
        return part;
    }

    /// <summary>What Top Gun is, said once wherever its figure is shown.</summary>
    internal const string TopGunNote = TopGun.Note;

    /// <summary>The part's heading under the form, which is the form's own "Scope and mount" section worked out.</summary>
    internal const string PartHeading = "What the scope and mount allow";

    /// <summary>Scrolls the rifle page so its Scope and mount part is at the top, for its picture.</summary>
    internal void ShowScopeAndMount()
    {
        // Laid out first, so the scroller knows how tall the page is before it is asked to move.
        UpdateLayout();
        var heading = equipmentForm.GetLogicalDescendants().OfType<TextBlock>().LastOrDefault(t => t.Text == PartHeading);

        // A rectangle from the heading down, taller than the window, so bringing it into view puts the heading at the top.
        heading?.BringIntoView(new Rect(0, -Tokens.Space16, Math.Max(1, heading.Bounds.Width), 4000));
        Dispatcher.UIThread.RunJobs();
        UpdateLayout();
    }

    /// <summary>The Scope and mount part's text, for the headless tests.</summary>
    internal IEnumerable<string> ScopeMountText => equipmentForm.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}
