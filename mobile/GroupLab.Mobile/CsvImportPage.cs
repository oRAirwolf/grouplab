using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// Shots from a CSV file on the phone, NOTES-FROM-PLANNING.md entry 278 section 2 (CSV B, as drawn on the phone parity canvas): once a file
/// is chosen GroupLab reads its headings and numbers and guesses which column is across, which is up and down, the unit, which way is up and
/// where the numbers are measured from; the page shows the group as it will be read and a card of those guesses, each line tapped to change,
/// and a line GroupLab could not guess says so and asks. "Import N shots" makes the result; "Show me the whole file" shows every row.
/// </summary>
internal sealed class CsvImportPage : UserControl
{
    private static readonly CoordinateUnit[] Units = [CoordinateUnit.Inch, CoordinateUnit.Millimetre, CoordinateUnit.Centimetre, CoordinateUnit.Moa, CoordinateUnit.Mil];

    private readonly CsvTable table;
    private readonly string name;
    private readonly Action back;
    private readonly Action<PhoneResult> imported;
    private readonly UnitSettings units = Phone.Settings.LoadUnits();
    private readonly TextBox distance = new() { MinHeight = Screens.Touch };
    private CsvGuess guess;

    public CsvImportPage(CsvTable table, string name, Action back, Action<PhoneResult> imported)
    {
        WorkInProgress.HoldWhileShown(this);
        this.table = table;
        this.name = name;
        this.back = back;
        this.imported = imported;
        guess = CsvGuess.For(table, name);
        distance.PlaceholderText = $"Distance in {UnitSettings.Symbol(units.Distance)}, for MOA or mil";
        distance.TextChanged += (_, _) => Show();
        Show();
    }

    private double? DistanceInches =>
        double.TryParse(distance.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double v) && v > 0 ? UnitSettings.DistanceToInches(v, units.Distance) : null;

    /// <summary>The shots as the current lines read them, or why they cannot be read yet.</summary>
    private (IReadOnlyList<PointD> Offsets, int Skipped, string? Why) Read()
    {
        if (guess.Across.Value is not { } x || guess.UpDown.Value is not { } y || x == y)
        {
            return ([], 0, "Choose the across and the up and down columns.");
        }

        if (guess.Unit.Value is not { } unit)
        {
            return ([], 0, "Choose the unit to see the group.");
        }

        if (unit is CoordinateUnit.Moa or CoordinateUnit.Mil && DistanceInches is null)
        {
            return ([], 0, "Numbers in MOA or mil need the distance they were shot at.");
        }

        var (offsets, skipped) = ShotCsv.Shots(table, x, y, unit, guess.UpIsPositive.Value ?? true, DistanceInches);
        return offsets.Count == 0 ? ([], skipped, "No row has a number in both of those columns.") : (offsets, skipped, null);
    }

    private void Show()
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Import shots"));
        column.Children.Add(Screens.Dim($"{name}: {table.Rows.Count.ToString(CultureInfo.CurrentCulture)} rows."));
        var (offsets, skipped, why) = Read();
        if (why is null)
        {
            var centre = new PointD(offsets.Average(o => o.X), offsets.Average(o => o.Y));
            column.Children.Add(new CompositePlot
            {
                Height = 280,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Shots = [.. offsets.Select((o, k) => new PlotShot(k, (k + 1).ToString(CultureInfo.InvariantCulture), null, o, false))],
                Centre = centre,
                Length = inches => units.Length(inches),
                ShowKey = false,
            });
            if (skipped > 0)
            {
                column.Children.Add(Screens.Dim($"{skipped.ToString(CultureInfo.CurrentCulture)} rows without two numbers, such as a total line, are left out."));
            }
        }
        else
        {
            column.Children.Add(Screens.Line(why));
        }

        var card = new StackPanel();
        card.Children.Add(Screens.Heading("GroupLab's guesses"));
        card.Children.Add(Screens.Row(guess.Across.Words, null, () => ChooseColumn(across: true)));
        card.Children.Add(Screens.Row(guess.UpDown.Words, null, () => ChooseColumn(across: false)));
        card.Children.Add(Screens.Row(guess.Unit.Words, null, ChooseUnit));
        card.Children.Add(Screens.Row(guess.UpIsPositive.Words, null, () => Change(guess with
        {
            UpIsPositive = guess.UpIsPositive.Value is false
                ? new GuessLine<bool>(true, "Up and down: a larger number is higher on the target, as you chose.")
                : new GuessLine<bool>(false, "Up and down: a larger number is lower on the target, as you chose."),
        })));
        card.Children.Add(Screens.Row(guess.FromGroupCentre.Words, null, ChooseOrigin));
        column.Children.Add(new Border { Child = card, Classes = { PhoneStyles.Card } });
        if (guess.Unit.Value is CoordinateUnit.Moa or CoordinateUnit.Mil)
        {
            column.Children.Add(distance);
        }

        if (why is null && guess.FromGroupCentre.Value is { } fromCentre)
        {
            column.Children.Add(Screens.Primary($"Import {offsets.Count.ToString(CultureInfo.CurrentCulture)} shots", () => Import(offsets, skipped, fromCentre)));
        }
        else if (why is null)
        {
            column.Children.Add(Screens.Line("Choose where the numbers are measured from, then import."));
        }

        column.Children.Add(Screens.Choice("Show me the whole file", WholeFile));
        column.Children.Add(Screens.Choice("Cancel", back));
        Content = Screens.Page(column);
    }

    private void Change(CsvGuess changed)
    {
        guess = changed;
        Show();
    }

    private void Choose(string heading, IEnumerable<(string Words, Action Chosen)> options)
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title(heading));
        var card = new StackPanel();
        foreach (var (words, chosen) in options)
        {
            card.Children.Add(Screens.Row(words, null, chosen));
        }

        column.Children.Add(new Border { Child = card, Classes = { PhoneStyles.Card } });
        column.Children.Add(Screens.Choice("Back", Show));
        Content = Screens.Page(column);
    }

    private void ChooseColumn(bool across)
    {
        string axis = across ? "Across" : "Up and down";
        Choose(across ? "Which column is across?" : "Which column is up and down?", table.Headers.Select((h, i) =>
        {
            string heading = string.IsNullOrWhiteSpace(h) ? $"column {(i + 1).ToString(CultureInfo.CurrentCulture)}" : h;
            var line = new GuessLine<int>(i, $"{axis}: the column called \"{heading}\", as you chose.");
            return (heading, (Action)(() => Change(across ? guess with { Across = line } : guess with { UpDown = line })));
        }));
    }

    private void ChooseUnit() => Choose("Which unit are the numbers in?", Units.Select(u =>
        (CsvGuess.Name(u), (Action)(() => Change(guess with { Unit = new GuessLine<CoordinateUnit>(u, $"Unit: {CsvGuess.Name(u)}, as you chose.") })))));

    private void ChooseOrigin() => Choose("Where are the numbers measured from?",
    [
        ("The point of aim", () => Change(guess with { FromGroupCentre = new GuessLine<bool>(false, "Measured from: the point of aim, as you chose.") })),
        ("The group's own center", () => Change(guess with { FromGroupCentre = new GuessLine<bool>(true, "Measured from: the group's own center, as you chose.") })),
    ]);

    private void WholeFile()
    {
        var column = new StackPanel { Spacing = 8 };
        column.Children.Add(Screens.Title(name));
        column.Children.Add(Screens.Dim(string.Join("  |  ", table.Headers)));
        foreach (var row in table.Rows.Take(500))
        {
            column.Children.Add(Screens.Line(string.Join("  |  ", row)));
        }

        if (table.Rows.Count > 500)
        {
            column.Children.Add(Screens.Dim($"And {(table.Rows.Count - 500).ToString(CultureInfo.CurrentCulture)} more rows."));
        }

        column.Children.Add(Screens.Choice("Back", Show));
        Content = Screens.Page(column);
    }

    private void Import(IReadOnlyList<PointD> offsets, int skipped, bool fromCentre)
    {
        var state = ShotCsv.Marking(offsets, DistanceInches, fromCentre);
        long? id = PhoneAnalysis.Save(state, null, units, null);
        DiagnosticLog.Info("file.import", ("kind", "csv"), ("shots", offsets.Count), ("skipped", skipped));
        imported(new PhoneResult(state, null, null, id));
    }
}
