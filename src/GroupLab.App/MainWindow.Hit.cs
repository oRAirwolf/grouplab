using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// Hit probability on the Ballistics screen, NOTES-FROM-PLANNING.md entry 156, from the shooter's own measured dispersion. Everything
/// GroupLab has measured fills itself in: the precision from the group open in the analysis or from a load's sessions pooled, the velocity's
/// spread from the load, and the zero's error from the uncertainty in the group's center. What nobody can measure is set by a confidence
/// preset, and each figure can be edited under Advanced, where the true value can be set apart from the believed one. The answer sits with
/// the dope for the distance, first and second round side by side, each with its interval, with what costs the most beneath it. The model
/// is written down in <see cref="HitProbability"/>.
/// </summary>
public sealed partial class MainWindow
{
    private static readonly string[] HitFromChoices = ["the group open in the analysis", "the chosen load's sessions, pooled", "typed here"];

    private static readonly HitSource[] HitStringSources =
        [HitSource.Wind, HitSource.Range, HitSource.Drag, HitSource.Temperature, HitSource.Pressure, HitSource.Humidity, HitSource.Inclination, HitSource.Azimuth, HitSource.Latitude];

    private readonly ComboBox hitFrom = new() { ItemsSource = HitFromChoices, SelectedIndex = 0, MinWidth = 260 };
    private readonly TextBox hitPrecision = Field();
    private readonly TextBlock hitFromWords = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly TextBox hitDistance = Field("600");
    private readonly ComboBox hitShape = new() { ItemsSource = new[] { "circle, its diameter", "rectangle, width by height", "IPSC outline, width by height" }, SelectedIndex = 0, MinWidth = 200 };
    private readonly TextBox hitWidth = Field("12");
    private readonly TextBox hitHeight = Field("12");
    private readonly ComboBox hitSizeUnit = new() { MinWidth = 90 };
    private readonly TextBox hitWind = Field("10");
    private readonly ComboBox hitPreset = new() { ItemsSource = HitPresets.All.Select(p => p.Name).Append(HitPresets.Custom).ToList(), SelectedIndex = 1, MinWidth = 260 };
    private readonly TextBlock hitPresetWords = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly TextBox hitShots = Field("1");
    private readonly TextBox hitVelocitySd = Field();
    private readonly TextBox hitVelocityBias = Field("0");
    private readonly TextBox hitZero = Field();
    private readonly TextBox hitAngle = Field("0");
    private readonly TextBox hitLatitude = Field();
    private readonly TextBox hitAzimuth = Field("0");
    private readonly TextBox hitTrials = Field(HitProbability.DefaultTrials.ToString(CultureInfo.InvariantCulture));
    private readonly TextBox hitSeed = Field(HitProbability.DefaultSeed.ToString(CultureInfo.InvariantCulture));
    private readonly Dictionary<HitSource, (TextBox Sd, TextBox Bias)> hitErrors = [];
    private readonly Expander hitAdvanced = new() { Header = "Advanced", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly StackPanel hitLines = new() { Spacing = Tokens.Space4 };
    private readonly HitScatter hitScatter = new() { HorizontalAlignment = HorizontalAlignment.Left };
    private readonly HitCurve hitCurve = new();
    private bool fillingHit;

    /// <summary>The analysis's shot distance the distance box was last set from, so a distance somebody typed is not written over.</summary>
    private double? hitDistanceFrom;

    /// <summary>What the section is called and what it means, the first thing under its heading.</summary>
    internal const string HitIntroduction = "The chance of a hit on a target at a distance, from your own measured precision and the load's measured velocity spread, with the errors nobody can measure set by a confidence preset or by hand. It is worked out by simulating thousands of strings, and every figure comes with its interval.";

    /// <summary>The assumptions, entry 156 section 4 item 2, in one short paragraph on the screen.</summary>
    internal const string HitAssumptions = "This assumes the rifle's dispersion is the same from shot to shot, that the error sources are independent of each other, and that the target is engaged from a stable position like the one the group was shot from. The second round assumes the first impact was seen and the whole of its miss was dialed off.";

    private void BuildHit(StackPanel column)
    {
        column.Children.Add(Heading("Hit probability"));
        column.Children.Add(Line(HitIntroduction));
        column.Children.Add(Row(FieldLabel("Precision from"), hitFrom, Angled("Rifle precision, per axis SD"), hitPrecision));
        column.Children.Add(hitFromWords);
        column.Children.Add(Row(Distanced("At"), hitDistance, FieldLabel("Target"), hitShape, FieldLabel("Size"), hitWidth, hitHeight, hitSizeUnit));
        column.Children.Add(Row(Measured("Crosswind, full value", BallisticMeasure.WindSpeed), hitWind, FieldLabel("Confidence preset"), hitPreset, FieldLabel("Shots in the string"), hitShots));
        column.Children.Add(hitPresetWords);

        // Entry 156 section 1: the figures nobody can measure, behind a disclosure so the screen is usable before somebody knows what a wind
        // call uncertainty is. Section 7 item 1: each has a bias beside its standard deviation, the true value less the believed one.
        var advanced = new StackPanel { Spacing = Tokens.Space4 };
        advanced.Children.Add(Line("Each is one standard deviation of how far the truth may lie from what you believe, and a bias, the true value less the believed one, for something you know is off, such as a chronograph reading fast. A confidence preset sets the standard deviations; editing any of them makes it custom."));
        advanced.Children.Add(Row(HitLabel(HitSource.Velocity, "Velocity SD, per shot"), hitVelocitySd, HitLabel(HitSource.Velocity, "bias"), hitVelocityBias));
        foreach (var source in HitStringSources)
        {
            var sd = Field();
            var bias = Field("0");
            hitErrors[source] = (sd, bias);
            foreach (var box in new[] { sd, bias })
            {
                // Avalonia raises this after the text is set, so a preset's own writes are told apart by whether the figures still match it.
                box.TextChanged += (_, _) =>
                {
                    if (!fillingHit && hitPreset.SelectedIndex < HitPresets.All.Count && !MatchesPreset(hitPreset.SelectedIndex))
                    {
                        fillingHit = true;
                        hitPreset.SelectedIndex = HitPresets.All.Count;
                        fillingHit = false;
                        ShowPresetWords();
                    }
                };
            }

            advanced.Children.Add(Row(HitLabel(source, HitSourceLabel(source)), sd, HitLabel(source, "bias"), bias));
        }

        advanced.Children.Add(Row(HitLabel(HitSource.Zero, "Zero error, per axis SD"), hitZero));
        advanced.Children.Add(Row(FieldLabel("Shot angle, degrees"), hitAngle, FieldLabel("Latitude, degrees"), hitLatitude, FieldLabel("Direction of fire, degrees from north"), hitAzimuth));
        advanced.Children.Add(Line("Leave the latitude empty to leave the Earth's rotation out."));
        advanced.Children.Add(Row(FieldLabel("Trials"), hitTrials, FieldLabel("Seed"), hitSeed));
        advanced.Children.Add(Line("The same seed gives the same answer, so a result can be checked and a screenshot made again."));
        hitAdvanced.Content = advanced;
        column.Children.Add(hitAdvanced);
        column.Children.Add(Row(Button("Work out the chance", FillHit)));
        column.Children.Add(hitLines);
        column.Children.Add(hitScatter);
        column.Children.Add(hitCurve);

        hitFrom.SelectionChanged += (_, _) =>
        {
            if (!fillingHit)
            {
                ShowHitPrecision();
            }
        };
        hitShape.SelectionChanged += (_, _) =>
        {
            hitHeight.IsVisible = hitShape.SelectedIndex > 0;
            if (hitShape.SelectedIndex == 2)
            {
                // GroupLab's IPSC outline at the size its body and head are drawn for.
                hitSizeUnit.SelectedIndex = 0;
                hitWidth.Text = Text(UnitSettings.FromInches(18, units.Linear));
                hitHeight.Text = Text(UnitSettings.FromInches(30, units.Linear));
            }
        };
        hitHeight.IsVisible = false;
        ShowSizeUnits();
        hitPreset.SelectionChanged += (_, _) =>
        {
            if (!fillingHit)
            {
                ApplyPreset();
            }
        };
        ApplyPreset();
    }

    /// <summary>
    /// The target's size is a length in the person's units, or an angle, entry 156 section 1: "in inches, centimetres, MOA or mil". An
    /// angle is turned into a length at the distance the answer is for.
    /// </summary>
    private void ShowSizeUnits()
    {
        int chosen = Math.Max(0, hitSizeUnit.SelectedIndex);
        hitSizeUnit.ItemsSource = new[] { UnitSettings.Symbol(units.Linear), "MOA", "mil" };
        hitSizeUnit.SelectedIndex = chosen;
    }

    /// <summary>A size box's value in inches at the distance, or null where it holds nothing usable.</summary>
    private double? HitSizeInches(TextBox box, double yards) => Number(box) is not { } value ? null : hitSizeUnit.SelectedIndex switch
    {
        1 => value / UnitSettings.AngleIn(1, yards * 36, AngularUnit.Moa)!.Value,
        2 => value / UnitSettings.AngleIn(1, yards * 36, AngularUnit.Mrad)!.Value,
        _ => UnitSettings.ToInches(value, units.Linear),
    };

    /// <summary>Sets the distance from the analysis's shot distance when that has changed, so the answer starts at the distance the group was shot.</summary>
    private void ShowHitDistance()
    {
        if (session.State.ShotDistanceInches is { } shot && shot != hitDistanceFrom)
        {
            hitDistanceFrom = shot;
            hitDistance.Text = Text(UnitSettings.DistanceFromInches(shot, units.Distance));
        }
    }

    /// <summary>A label that names its unit and is rewritten when the toggle moves.</summary>
    private TextBlock HitLabel(HitSource source, string name)
    {
        var label = FieldLabel("");
        unitLabels.Add((label, () => name + ", " + HitUnit(source)));
        return label;
    }

    private TextBlock Angled(string name)
    {
        var label = FieldLabel("");
        unitLabels.Add((label, () => name + ", " + UnitSettings.Symbol(units.Angular)));
        return label;
    }

    private static string HitSourceLabel(HitSource source) => source switch
    {
        HitSource.Wind => "Wind call uncertainty",
        HitSource.Range => "Range estimation error",
        HitSource.Drag => "Drag uncertainty",
        HitSource.Temperature => "Temperature uncertainty",
        HitSource.Pressure => "Pressure uncertainty",
        HitSource.Humidity => "Humidity uncertainty",
        HitSource.Inclination => "Shot angle uncertainty",
        HitSource.Azimuth => "Direction of fire uncertainty",
        _ => "Latitude uncertainty",
    };

    /// <summary>What each source is called where its cost is listed.</summary>
    internal static string HitSourceName(HitSource source) => source switch
    {
        HitSource.Dispersion => "The rifle's own dispersion",
        HitSource.Velocity => "The muzzle velocity's spread",
        HitSource.Wind => "The wind call",
        HitSource.Range => "The range estimate",
        HitSource.Zero => "The zero error",
        HitSource.Drag => "The drag",
        HitSource.Temperature => "The temperature",
        HitSource.Pressure => "The station pressure",
        HitSource.Humidity => "The humidity",
        HitSource.Inclination => "The shot angle",
        HitSource.Azimuth => "The direction of fire",
        _ => "The latitude",
    };

    private string HitUnit(HitSource source) => source switch
    {
        HitSource.Velocity => BallisticMeasures.Symbol(BallisticMeasure.Speed, units),
        HitSource.Wind => BallisticMeasures.Symbol(BallisticMeasure.WindSpeed, units),
        HitSource.Temperature => BallisticMeasures.Symbol(BallisticMeasure.Temperature, units),
        HitSource.Pressure => BallisticMeasures.Symbol(BallisticMeasure.Pressure, units),
        HitSource.Range => UnitSettings.Symbol(units.Distance),
        HitSource.Zero => UnitSettings.Symbol(units.Angular),
        HitSource.Drag or HitSource.Humidity => "%",
        _ => "degrees",
    };

    /// <summary>One mrad in the person's angular unit.</summary>
    private double ShownPerMrad => UnitSettings.AngleIn(36, 36000, units.Angular) ?? 1;

    /// <summary>
    /// A difference as the person typed it, turned into the imperial one the engine works in: a temperature difference scales and never
    /// shifts, so it is the conversion of the value less the conversion of zero. An angle is in mrad.
    /// </summary>
    private double HitToImperial(HitSource source, double shown) => source switch
    {
        HitSource.Velocity => BallisticMeasures.ToImperial(shown, BallisticMeasure.Speed, units),
        HitSource.Wind => BallisticMeasures.ToImperial(shown, BallisticMeasure.WindSpeed, units),
        HitSource.Temperature => BallisticMeasures.ToImperial(shown, BallisticMeasure.Temperature, units) - BallisticMeasures.ToImperial(0, BallisticMeasure.Temperature, units),
        HitSource.Pressure => BallisticMeasures.ToImperial(shown, BallisticMeasure.Pressure, units),
        HitSource.Range => UnitSettings.DistanceToInches(shown, units.Distance) / 36,
        HitSource.Zero => shown / ShownPerMrad,
        _ => shown,
    };

    private double HitFromImperial(HitSource source, double imperial) => source switch
    {
        HitSource.Velocity => BallisticMeasures.FromImperial(imperial, BallisticMeasure.Speed, units),
        HitSource.Wind => BallisticMeasures.FromImperial(imperial, BallisticMeasure.WindSpeed, units),
        HitSource.Temperature => BallisticMeasures.FromImperial(imperial, BallisticMeasure.Temperature, units) - BallisticMeasures.FromImperial(0, BallisticMeasure.Temperature, units),
        HitSource.Pressure => BallisticMeasures.FromImperial(imperial, BallisticMeasure.Pressure, units),
        HitSource.Range => UnitSettings.DistanceFromInches(imperial * 36, units.Distance),
        HitSource.Zero => imperial * ShownPerMrad,
        _ => imperial,
    };

    private static string HitText(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double? Signed(TextBox box) =>
        double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value) ? value : null;

    /// <summary>The distance the answer is for, in yards, or null where the box holds nothing usable.</summary>
    private double? HitYards => Number(hitDistance) is { } d ? UnitSettings.DistanceToInches(d, units.Distance) / 36 : null;

    /// <summary>Fills every figure nobody can measure from the chosen preset, for the distance in the box.</summary>
    private void ApplyPreset()
    {
        if (hitPreset.SelectedIndex >= 0 && hitPreset.SelectedIndex < HitPresets.All.Count)
        {
            var errors = HitPresets.All[hitPreset.SelectedIndex].Errors(HitYards ?? 600);
            fillingHit = true;
            foreach (var (source, (sd, _)) in hitErrors)
            {
                sd.Text = HitText(HitFromImperial(source, errors.TryGetValue(source, out var u) ? u.Sd : 0));
            }

            fillingHit = false;
        }

        ShowPresetWords();
    }

    /// <summary>True while every figure under Advanced is still the preset's, within what the boxes' rounding and the unit toggle leave.</summary>
    private bool MatchesPreset(int index)
    {
        var errors = HitPresets.All[index].Errors(HitYards ?? 600);
        return hitErrors.All(e =>
        {
            double want = errors.TryGetValue(e.Key, out var u) ? u.Sd : 0;
            return Signed(e.Value.Sd) is { } sd && Math.Abs(HitToImperial(e.Key, sd) - want) <= 0.002 * Math.Max(1, Math.Abs(want)) && (Signed(e.Value.Bias) ?? 0) == 0;
        });
    }

    private void ShowPresetWords() => hitPresetWords.Text = hitPreset.SelectedIndex >= 0 && hitPreset.SelectedIndex < HitPresets.All.Count
        ? HitPresets.All[hitPreset.SelectedIndex].Situation + " Its figures are under Advanced."
        : "Custom: one or more of the figures under Advanced has been edited.";

    /// <summary>Every box on the section that holds a difference, with the kind of quantity it is, so the unit toggle can rewrite it.</summary>
    private IEnumerable<(TextBox Box, HitSource Source)> HitBoxes()
    {
        foreach (var (source, (sd, bias)) in hitErrors)
        {
            yield return (sd, source);
            yield return (bias, source);
        }

        yield return (hitVelocitySd, HitSource.Velocity);
        yield return (hitVelocityBias, HitSource.Velocity);
        yield return (hitZero, HitSource.Zero);
        yield return (hitPrecision, HitSource.Zero);
        yield return (hitWind, HitSource.Wind);
    }

    /// <summary>What is in the section's boxes, kept in imperial before the unit toggle moves, and written back in the new units after.</summary>
    private Action KeepHitBoxes()
    {
        var kept = HitBoxes().Select(b => (b.Box, b.Source, Value: Signed(b.Box) is { } v ? HitToImperial(b.Source, v) : (double?)null)).ToList();
        return () =>
        {
            fillingHit = true;
            foreach (var (box, source, value) in kept)
            {
                if (value is { } v)
                {
                    box.Text = HitText(HitFromImperial(source, v));
                }
            }

            fillingHit = false;
        };
    }

    /// <summary>
    /// Where the precision comes from, entry 156 section 1: its sigma as an angle in mrad, the degrees of freedom it rests on and the distance
    /// it was measured at, or null with the reason. The zero's error starts at the uncertainty of the group's center, sigma over the root of
    /// the shots, because that is how well the group can say where the rifle points.
    /// </summary>
    private (HitPrecision? Precision, string Words, double? ZeroMrad) HitPrecisionFrom()
    {
        string Angle(double mrad) => HitText(Math.Round(mrad * ShownPerMrad, 3)) + " " + UnitSettings.Symbol(units.Angular);
        switch (hitFrom.SelectedIndex)
        {
            case 1:
                if (ChosenLoad is not { } load)
                {
                    return (null, "Choose a load above to pool its sessions.", null);
                }

                if (sessions is null)
                {
                    return (null, "There are no saved sessions to pool.", null);
                }

                var targets = new List<IReadOnlyList<PointD>>();
                var distances = new List<double>();
                foreach (var summary in sessions.List(load: load.Name))
                {
                    if (sessions.Get(summary.Id) is { DistanceInches: { } d and > 0 } record)
                    {
                        var (offsets, _) = KeptOffsets(MarkingFile.Read(record.MarkingJson).State);
                        if (offsets.Count >= 2)
                        {
                            targets.Add([.. offsets.Select(o => new PointD(o.X * 1000 / d, o.Y * 1000 / d))]);
                            distances.Add(d);
                        }
                    }
                }

                if (targets.Count == 0)
                {
                    return (null, $"{load.Name} has no saved sessions with a distance and two or more shots to pool.", null);
                }

                var pooled = Pooling.Recentred(targets);
                int shots = targets.Sum(t => t.Count);
                double nearest = distances.Min();
                return (new HitPrecision(pooled.Sigma.Value, pooled.DegreesOfFreedom, nearest / 36),
                    $"From {targets.Count} saved session{(targets.Count == 1 ? "" : "s")} of {load.Name}, {shots} shots, each centered on its own group and pooled: sigma {Angle(pooled.Sigma.Value)} per axis, {Angle(pooled.Sigma.Lower)} to {Angle(pooled.Sigma.Upper)}. The velocity's share of it is taken out at the nearest of their distances, {units.DistanceText(nearest)}.",
                    pooled.Sigma.Value / Math.Sqrt((double)shots / targets.Count));
            case 2:
                if (Number(hitPrecision) is not { } typed)
                {
                    return (null, "Type the rifle's precision, the per axis standard deviation of its shots as an angle.", null);
                }

                return (new HitPrecision(typed / ShownPerMrad, null, null), "Typed, so its own uncertainty is not known and is left out of the interval. It is the per axis standard deviation, sigma, and never a group size or a mean radius.", null);
            default:
                var state = session.State;
                var figures = GroupAnalysis.Analyse(state);
                var group = figures.Excluded > 0 ? figures.WithoutExclusions : figures.AllShots;
                if (state.ShotDistanceInches is not { } distance)
                {
                    return (null, "The group open in the analysis has no shot distance; set it in the marking.", null);
                }

                if (group?.Sigma is not { } sigma)
                {
                    return (null, "The analysis has no group with a sigma; mark at least " + GroupAnalysis.MinimumShotsForDispersion.ToString(CultureInfo.InvariantCulture) + " shots and accept them.", null);
                }

                double yards = distance / 36, mrad = HitPrecision.MradFromInches(sigma.Value, yards);
                string interval = sigma.Lower is { } lo && sigma.Upper is { } hi ? $", {Angle(HitPrecision.MradFromInches(lo, yards))} to {Angle(HitPrecision.MradFromInches(hi, yards))}" : "";
                return (new HitPrecision(mrad, 2.0 * (group.Shots - 1), yards),
                    $"From the group open in the analysis: sigma {Angle(mrad)} per axis{interval}, {group.Shots} shots at {units.DistanceText(distance)}.",
                    mrad / Math.Sqrt(group.Shots));
        }
    }

    /// <summary>Fills the precision, the velocity's spread and the zero's error from what GroupLab measured.</summary>
    private void ShowHitPrecision()
    {
        var (precision, words, zero) = HitPrecisionFrom();
        fillingHit = true;
        hitPrecision.IsReadOnly = hitFrom.SelectedIndex != 2;
        if (hitFrom.SelectedIndex != 2)
        {
            hitPrecision.Text = precision is null ? "" : HitText(Math.Round(precision.SigmaMrad * ShownPerMrad, 4));
        }

        hitFromWords.Text = words;
        hitZero.Text = zero is { } z ? HitText(Math.Round(z * ShownPerMrad, 4)) : hitFrom.SelectedIndex == 2 ? hitZero.Text : "";
        hitVelocitySd.Text = BallisticMeasures.Text(ChosenLoad?.MuzzleVelocitySdFps, BallisticMeasure.Speed, units);
        fillingHit = false;
    }

    /// <summary>
    /// The chance of a hit, as the section's fields say, entry 156 sections 3 and 4: the dope at the distance with the first and second round
    /// beside it, what dominates the interval, the string's figures, what each source costs, the split up and down and across, the
    /// assumptions, the scatter and the curve against distance. Every probability has its interval, and none more digits than it supports.
    /// </summary>
    internal void FillHit()
    {
        hitLines.Children.Clear();
        hitScatter.First = [];
        hitScatter.Second = [];
        hitCurve.Points = [];
        hitScatter.InvalidateVisual();
        hitCurve.InvalidateVisual();
        var rifle = ChosenRifle;
        var load = ChosenLoad;
        var missing = SolverUse.Missing(rifle, load);
        if (missing.Count > 0)
        {
            hitLines.Children.Add(Line("The solver needs " + Joined(missing) + ", for the rifle and load chosen above."));
            return;
        }

        if (hitFrom.SelectedIndex != 2)
        {
            ShowHitPrecision();
        }

        var (precision, words, _) = HitPrecisionFrom();
        int shots = 0, trials = 0;
        ulong seed = 0;
        string? refusal = precision is null ? words
            : HitYards is null ? "Enter the distance to work the chance out at."
            : Number(hitWidth) is null || (hitShape.SelectedIndex > 0 && Number(hitHeight) is null) ? "Enter the target's size."
            : !int.TryParse(hitShots.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out shots) || shots is < 1 or > 50 ? "Enter the shots in the string, from 1 to 50."
            : !int.TryParse(hitTrials.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out trials) || trials is < 100 or > 1_000_000 ? "Enter the trials, from 100 to a million."
            : !ulong.TryParse(hitSeed.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed) ? "Enter the seed as a whole number."
            : Signed(hitWind) is null ? "Enter the crosswind, 0 for none."
            : null;
        if (refusal is not null)
        {
            hitLines.Children.Add(Line(refusal));
            return;
        }

        double yards = HitYards!.Value;
        if (hitPreset.SelectedIndex < HitPresets.All.Count)
        {
            ApplyPreset();
        }

        var errors = new Dictionary<HitSource, HitUncertainty>();
        foreach (var (source, (sd, bias)) in hitErrors)
        {
            errors[source] = new HitUncertainty(Math.Abs(HitToImperial(source, Signed(sd) ?? 0)), HitToImperial(source, Signed(bias) ?? 0));
        }

        errors[HitSource.Velocity] = new HitUncertainty(Math.Abs(HitToImperial(HitSource.Velocity, Signed(hitVelocitySd) ?? 0)), HitToImperial(HitSource.Velocity, Signed(hitVelocityBias) ?? 0));
        errors[HitSource.Zero] = new HitUncertainty(Math.Abs(HitToImperial(HitSource.Zero, Signed(hitZero) ?? 0)));
        var air = Air();
        var believed = SolverUse.Input(rifle, load, air)! with
        {
            CrosswindMph = BallisticMeasures.ToImperial(Signed(hitWind)!.Value, BallisticMeasure.WindSpeed, units),
            AngleDegrees = Signed(hitAngle) ?? 0,
            LatitudeDegrees = Signed(hitLatitude),
            AzimuthDegrees = Signed(hitAzimuth) ?? 0,
        };
        double width = HitSizeInches(hitWidth, yards)!.Value;
        double height = hitShape.SelectedIndex > 0 ? HitSizeInches(hitHeight, yards)!.Value : width;
        var target = new HitTarget(hitShape.SelectedIndex switch { 1 => HitShape.Rectangle, 2 => HitShape.Ipsc, _ => HitShape.Circle }, width, height);
        var setup = new HitSetup(believed, yards, target, precision!, errors, shots, trials, seed);
        var answer = HitProbability.Work(setup);
        DiagnosticLog.Info("ballistics.hit", ("from", hitFrom.SelectedIndex), ("shots", shots), ("trials", trials), ("refused", answer.Refusal is not null));
        ShowHit(setup, answer);
    }

    private void ShowHit(HitSetup setup, HitAnswer answer)
    {
        string at = units.DistanceText(setup.DistanceYards * 36);
        string target = setup.Target.Shape switch
        {
            HitShape.Rectangle => $"a {units.Number(setup.Target.WidthInches)} by {units.Length(setup.Target.HeightInches)} rectangle",
            HitShape.Ipsc => $"an IPSC outline {units.Number(setup.Target.WidthInches)} by {units.Length(setup.Target.HeightInches)}",
            _ => $"a {units.Length(setup.Target.WidthInches)} circle",
        };
        if (answer.Refusal is not null)
        {
            hitLines.Children.Add(new TextBlock { Text = answer.Refusal, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
            foreach (string sentence in answer.NotIncluded)
            {
                hitLines.Children.Add(Note(sentence));
            }

            return;
        }

        // Entry 156 section 8 item 1: the probability with the dope, where the shooter reads the elevation and the wind.
        var point = BallisticSolver.Solve(setup.Solver, setup.DistanceYards, setup.DistanceYards).Points[^1];
        double range = setup.DistanceYards * 36;
        string elevation = (units.AngleText(Math.Abs(point.DropInches), range) ?? units.Length(Math.Abs(point.DropInches))) + (point.DropInches < 0 ? " up" : " down");
        string wind = Math.Abs(point.WindInches) < 5e-4 ? "no wind hold"
            : (units.AngleText(Math.Abs(point.WindInches), range) ?? units.Length(Math.Abs(point.WindInches))) + (point.WindInches > 0 ? " left" : " right") + " for the crosswind";
        hitLines.Children.Add(Line($"At {at}: elevation {elevation}, wind {wind}."));

        string Chance(HitChance c)
        {
            var (value, lower, upper) = HitProbability.Percents(c);
            return $"{value} percent ({lower} to {upper})";
        }

        foreach (string text in new[]
        {
            $"First round on {target} at {at}: {Chance(answer.FirstRound)}.",
            $"Second round, corrected from where the first landed: {Chance(answer.SecondRound)}.",
        })
        {
            hitLines.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        }

        string Points(double half) => HitText(Math.Round(100 * half, half >= 0.1 ? 0 : 1));
        var first = answer.FirstRound;
        string trials = answer.Trials.ToString("N0", CultureInfo.InvariantCulture);
        hitLines.Children.Add(Line(first.SigmaDominates
            ? $"Most of that interval is the uncertainty in the precision itself, {Points(first.SigmaHalfWidth)} points either way from its sigma's interval, against {Points(first.MonteCarloHalfWidth)} from the simulation's {trials} strings. More shots behind the precision would narrow it."
            : setup.Precision.DegreesOfFreedom is null
                ? $"The interval is the simulation's own, {Points(first.MonteCarloHalfWidth)} points either way at {trials} strings; the precision was typed, so its own uncertainty is not in it."
                : $"Most of that interval is the simulation's own, {Points(first.MonteCarloHalfWidth)} points either way at {trials} strings, against {Points(first.SigmaHalfWidth)} from the uncertainty in the precision."));
        if (answer.ShotsInString > 1)
        {
            var (e, lo, hi) = answer.ExpectedHits;
            hitLines.Children.Add(Line($"In a string of {answer.ShotsInString} shots fired on one reading: at least one hit {Chance(answer.AtLeastOne)}, and about {e.ToString("0.#", CultureInfo.InvariantCulture)} hits expected ({lo.ToString("0.#", CultureInfo.InvariantCulture)} to {hi.ToString("0.#", CultureInfo.InvariantCulture)})."));
        }

        // Section 3 item 5: the output a shooter can act on, which input is costing the most.
        hitLines.Children.Add(FieldLabel("What costs the most"));
        foreach (var cost in answer.Costs)
        {
            string costs = cost.Cost < first.MonteCarloHalfWidth ? "costs less than the simulation can tell apart" : $"costs {HitProbability.Percent(cost.Cost, first.MonteCarloHalfWidth)} points";
            hitLines.Children.Add(Line($"{HitSourceName(cost.Source)}, drawn {(cost.PerShot ? "per shot" : "per string")}, {costs}; it spreads the first round {units.Length(cost.UpDownInches)} up and down and {units.Length(cost.AcrossInches)} across."));
        }

        hitLines.Children.Add(Line($"All together the first round spreads {units.Length(answer.UpDownInches)} up and down and {units.Length(answer.AcrossInches)} across, as standard deviations."));
        hitLines.Children.Add(Note(HitAssumptions));
        foreach (string sentence in answer.NotIncluded)
        {
            hitLines.Children.Add(Note(sentence));
        }

        hitLines.Children.Add(Note($"{trials} strings, seed {answer.Seed.ToString(CultureInfo.InvariantCulture)}: the same seed gives the same answer. " + string.Join(" ", BallisticSolver.NotModelled)));
        hitScatter.First = answer.FirstScatter;
        hitScatter.Second = answer.SecondScatter;
        hitScatter.Target = setup.Target;
        hitScatter.Length = units.Length;
        hitScatter.InvalidateVisual();
        hitCurve.Points = HitProbability.Curve(setup, [.. Enumerable.Range(0, 11).Select(i => setup.DistanceYards * (0.25 + (0.125 * i)))]);
        hitCurve.MarkYards = setup.DistanceYards;
        hitCurve.Distance = yards => units.DistanceText(yards * 36);
        hitCurve.InvalidateVisual();
    }

    /// <summary>What the section shows, for the headless tests.</summary>
    internal IEnumerable<string> HitShown => hitLines.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>What the scatter and the curve say they show, for the headless tests.</summary>
    internal (string Scatter, string Curve) HitDrawings => (hitScatter.Description, hitCurve.Description);

    /// <summary>The distance box as it shows, for the headless tests.</summary>
    internal string HitDistanceShown => hitDistance.Text ?? "";

    /// <summary>The words under the precision's source, for the headless tests.</summary>
    internal string HitFromWords => hitFromWords.Text ?? "";

    /// <summary>Sets the section's fields as a person would, and works the chance out, for the headless tests.</summary>
    internal void WorkOutHit(string distance, int shape, string width, string height, int from = 0, string? precision = null, int? preset = null, string shots = "1", int sizeUnit = 0)
    {
        hitSizeUnit.SelectedIndex = sizeUnit;
        hitFrom.SelectedIndex = from;
        if (precision is not null)
        {
            hitPrecision.Text = precision;
        }

        if (preset is { } p)
        {
            hitPreset.SelectedIndex = p;
        }

        hitDistance.Text = distance;
        hitShape.SelectedIndex = shape;
        hitWidth.Text = width;
        hitHeight.Text = height;
        hitShots.Text = shots;
        FillHit();
    }

    /// <summary>The preset chosen and the wind call's standard deviation as its box shows it, for the headless tests.</summary>
    internal (string Preset, string WindSd) HitPresetShown => (hitPreset.SelectedItem as string ?? "", hitErrors[HitSource.Wind].Sd.Text ?? "");

    /// <summary>Types into the wind call's standard deviation, as a person editing it would, for the headless tests.</summary>
    internal void EditWindCall(string text) => hitErrors[HitSource.Wind].Sd.Text = text;
}
