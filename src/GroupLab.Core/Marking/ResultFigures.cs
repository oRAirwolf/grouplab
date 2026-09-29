using System.Globalization;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>
/// One figure on the phone's result: its name, its value in the chosen unit, its range where it has one, the plain explanation, and what
/// the shots it came from can say about it (the amber box of entry 259's explanation sheet).
/// </summary>
public sealed record ResultFigure(string Key, string Label, string Value, string? Range = null, string? Explanation = null, string? ShotsCanSay = null,
    bool Headline = false, string? Beneath = null);

/// <summary>A section of the phone's result, a card that opens: its heading, whether it starts open, and its figures.</summary>
public sealed record ResultSection(string Key, string Heading, bool Open, IReadOnlyList<ResultFigure> Figures, string? Note = null);

/// <summary>
/// NOTES-FROM-PLANNING.md entries 258 and 259 screen 1, "full figures, A": the phone's result as the desktop's analysis, from the same
/// shared computation (<see cref="GroupAnalysis"/>, <see cref="AnalysisPanel"/>, <see cref="FigureExplanations"/>), so the phone and the
/// desktop cannot give different numbers. Four tiles at the top (mean radius the headline with its size on paper beneath, extreme spread,
/// CEP 50, center from aim), then the sections: "All figures" open, "Advanced", "Bull by bull", and "Full CEP table and the fitted
/// ellipse". Sizes are in the chosen unit, inches or an angle; an angle needs the distance.
/// </summary>
public static class ResultFigures
{
    /// <summary>The figures of a marking, sizes in <paramref name="angle"/> where the distance allows and it is given, in the person's length otherwise.</summary>
    public static (IReadOnlyList<ResultFigure> Tiles, IReadOnlyList<ResultSection> Sections) Build(MarkingState state, UnitSettings units, AngularUnit? angle = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(units);
        var report = GroupAnalysis.Analyse(state);
        var all = report.Counted; // entry 278 section 5c: a shot left out is out of every figure
        double? distance = state.ShotDistanceInches;
        var shown = angle is { } a && distance is not null ? units with { Angular = a } : null;
        string Size(double inches) => shown?.Angle(inches, distance) is { } v
            ? v.ToString("0.00", CultureInfo.CurrentCulture) + " " + UnitSettings.Symbol(shown.Angular)
            : units.Length(inches);
        string Paper(double inches) => units.Length(inches) + " on the paper";

        var tiles = new List<ResultFigure>();
        var sections = new List<ResultSection>();
        if (all is null || all.Shots == 0)
        {
            return (tiles, sections);
        }

        int n = all.Shots;
        ResultFigure Figure(string key, string label, ReportedEstimate? e, string? unavailable, bool headline = false, string? beneath = null) =>
            new(key, label, e is null ? unavailable ?? "not available" : Size(e.Value), Range(e, Size), Explain(key), ShotsCanSay(e, n, Size), headline, beneath);

        tiles.Add(Figure("meanRadius", "Mean radius", all.MeanRadius, all.MeanRadiusUnavailable, headline: true,
            beneath: all.MeanRadius is { } mr && shown is not null ? Paper(mr.Value) : null));
        tiles.Add(Figure("extremeSpread", "Extreme spread", all.ExtremeSpread, all.ExtremeSpreadUnavailable, beneath: "center to center"));
        tiles.Add(Figure("cep50", "CEP 50", all.Cep50, all.MeanRadiusUnavailable));
        if (all.CentreFromAim is { } c)
        {
            double off = Math.Sqrt((c.X * c.X) + (c.Y * c.Y));
            tiles.Add(new ResultFigure("centreFromAim", "Center from aim", Size(off), Explanation: Explain("centreFromAim"), Beneath: "from the bulls' centers"));
        }
        else
        {
            tiles.Add(new ResultFigure("centreFromAim", "Center from aim", all.CentreFromAimUnavailable ?? "not available", Explanation: Explain("centreFromAim")));
        }

        var figures = new List<ResultFigure>();
        if (all.Width is { } w && all.Height is { } h)
        {
            figures.Add(new ResultFigure("groupSize", "Group width × height", $"{Size(w)} × {Size(h)}", Explanation: Explain("groupSize") ?? "How wide and how tall the group is, center to center of the outermost holes each way."));
        }

        figures.Add(Figure("cep90", "CEP 90", all.Cep90, all.MeanRadiusUnavailable));
        figures.Add(Figure("cep95", "CEP 95", all.Cep95, all.MeanRadiusUnavailable));
        figures.Add(Figure("cep99", "CEP 99", all.Cep99, all.MeanRadiusUnavailable));
        if (all.MeanRadius is { } radius)
        {
            figures.Add(new ResultFigure("meanRadiusRange", "Mean radius range", Range(radius, Size) ?? "not available", Explanation: Explain("meanRadius"), ShotsCanSay: ShotsCanSay(radius, n, Size)));
        }

        var panel = AnalysisPanel.Build(state, units);
        foreach (var zero in panel.Blocks.Where(b => b.Key == "zero").SelectMany(b => b.Figures).Where(f => !f.Label.EndsWith("other units", StringComparison.Ordinal)))
        {
            figures.Add(new ResultFigure("zero", "Zero, " + zero.Label.ToLowerInvariant(), zero.Value, Explanation: zero.Explanation, Beneath: zero.Interval));
        }

        sections.Add(new ResultSection("all", "All figures", true, figures, report.Problem ?? all.DispersionWithheld));

        var advanced = new List<ResultFigure> { Figure("sigma", "Sigma", all.Sigma, all.SigmaUnavailable) };
        if (all.SdX is { } sx && all.SdY is { } sy)
        {
            advanced.Add(new ResultFigure("sdAcross", "Spread across", Size(sx), Explanation: "How far the shots spread from side to side, one standard deviation."));
            advanced.Add(new ResultFigure("sdUpDown", "Spread up and down", Size(sy), Explanation: "How far the shots spread up and down, one standard deviation."));
        }

        sections.Add(new ResultSection("advanced", "Advanced", false, advanced));

        var bulls = new List<ResultFigure>();
        foreach (var bull in state.Shots.Where(s => s.IsShot && s.Exclusion is null && s.Bull is not null).GroupBy(s => s.Bull!.Value).OrderBy(g => g.Key))
        {
            string name = state.Bulls.FirstOrDefault(b => b.Index == bull.Key)?.Label ?? (bull.Key + 1).ToString(CultureInfo.InvariantCulture);
            bulls.Add(new ResultFigure("bull", "Bull " + name, bull.Count() == 1 ? "1 shot" : $"{bull.Count()} shots"));
        }

        sections.Add(new ResultSection("bulls", "Bull by bull", false, bulls));

        var table = new List<ResultFigure>
        {
            Figure("cep50", "CEP 50", all.Cep50, all.MeanRadiusUnavailable),
            Figure("cep90", "CEP 90", all.Cep90, all.MeanRadiusUnavailable),
            Figure("cep95", "CEP 95", all.Cep95, all.MeanRadiusUnavailable),
            Figure("cep99", "CEP 99", all.Cep99, all.MeanRadiusUnavailable),
        };
        if (all.AspectRatio is { } ratio && all.AngleDegrees is { } degrees)
        {
            table.Add(new ResultFigure("ellipse", "Fitted ellipse", string.Create(CultureInfo.CurrentCulture, $"{ratio:0.00} to 1, turned {degrees:0} degrees"),
                Explanation: Explain("aspectRatio") ?? "How much longer the group is one way than the other, and which way it leans."));
        }

        sections.Add(new ResultSection("table", "Full CEP table and the fitted ellipse", false, table));
        return (tiles, sections);
    }

    /// <summary>A figure's 95 percent range, in the unit of the figure.</summary>
    public static string? Range(ReportedEstimate? estimate, Func<double, string> size)
    {
        ArgumentNullException.ThrowIfNull(size);
        return estimate is { Lower: { } low, Upper: { } high } ? $"{size(low)} to {size(high)}" : null;
    }

    /// <summary>
    /// The amber box: the range these shots put on the figure, and what half as many would have done to it, from the sampling distribution
    /// of the spread (<see cref="SampleSize.SigmaIntervalMultiples"/>), the same the desktop's reasoning uses.
    /// </summary>
    public static string? ShotsCanSay(ReportedEstimate? estimate, int shots, Func<double, string> size)
    {
        ArgumentNullException.ThrowIfNull(size);
        if (estimate is not { Lower: { } low, Upper: { } high } || shots < 3)
        {
            return null;
        }

        string said = $"From {shots} shots the true value lies between {size(low)} and {size(high)}, 95 times in 100.";
        int half = Math.Max(3, shots / 2);
        if (half < shots)
        {
            var (l1, u1) = SampleSize.SigmaIntervalMultiples(shots);
            var (l2, u2) = SampleSize.SigmaIntervalMultiples(half);
            double wider = (u2 - l2) / (u1 - l1);
            said += string.Create(CultureInfo.CurrentCulture, $" With {half} shots the range would be {wider:0.0} times as wide.");
        }

        return said;
    }

    /// <summary>A figure's plain explanation: every CEP shares the glossary's "cep", and the fitted ellipse its "aspect".</summary>
    public static string? Explain(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return FigureExplanations.For(key.StartsWith("cep", StringComparison.Ordinal) ? "cep" : key == "aspectRatio" ? "aspect" : key)?.Plain;
    }
}
