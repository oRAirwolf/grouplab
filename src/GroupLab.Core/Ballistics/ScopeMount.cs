using System.Globalization;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Ballistics;

/// <summary>What a rifle's scope travel is: the whole of it, or the most it goes up from the rifle's zero.</summary>
public enum ScopeTravelKind
{
    Total,
    MostUpFromZero,
}

/// <summary>The scope and mount figures for a rifle and load, in MOA, or why there are none.</summary>
/// <param name="ZeroUsesMoa">The elevation the zero uses: the bore's angle above the line of sight at the rifle's zero, from the solver.</param>
/// <param name="HalfTravelMoa">Half the scope's elevation travel, the most it can go either way from its middle.</param>
/// <param name="MostRailCantMoa">The most rail cant that still lets the rifle be zeroed.</param>
/// <param name="LeftAboveZeroMoa">The elevation left above the zero with the rail's actual cant, or null where no cant is entered.</param>
/// <param name="FarthestYards">The farthest range that elevation reaches with this load, or null where it reaches past what is looked at.</param>
/// <param name="CannotZero">True where the entered rail cant is more than the most that still zeroes.</param>
public sealed record MountFigures(double ZeroUsesMoa, double HalfTravelMoa, double MostRailCantMoa, double? LeftAboveZeroMoa, double? FarthestYards, bool CannotZero);

/// <summary>
/// Rail cant, NOTES-FROM-PLANNING.md entry 456 section 1, from Alan's pissinhot.com scope cant calculator: the elevation the zero uses (the
/// zero compensation), half the scope's travel, the most rail cant that still zeroes (their sum) and, with the rail's actual cant, the
/// elevation left above the zero (half travel plus cant less the zero's), and the farthest range that elevation reaches.
/// <para>
/// <b>The one deliberate difference from his page:</b> the zero compensation is GroupLab's own zero launch angle, from the solver with drag
/// (<see cref="Trajectory.ZeroAngleMoa"/>), where his page adds the sight-height angle to a drop angle from a time of flight, 0.5 g t
/// squared. The solver's angle includes the same two parts and the slowing of the bullet the drop angle leaves out.
/// </para>
/// </summary>
public static class ScopeMount
{
    /// <summary>A value in a scope's unit as MOA.</summary>
    public static double ToMoa(double value, AngularUnit unit) => unit == AngularUnit.Mrad ? value / BallisticSolver.MilPerMoa : value;

    /// <summary>A value in MOA in a scope's unit.</summary>
    public static double FromMoa(double moa, AngularUnit unit) => unit == AngularUnit.Mrad ? moa * BallisticSolver.MilPerMoa : moa;

    /// <summary>The figures, from the zero's angle and the rifle's travel and cant (in its scope unit), or null where no travel is entered.</summary>
    public static MountFigures? Figures(Rifle rifle, double zeroAngleMoa, Trajectory? reach = null)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        if (rifle.ScopeTravel is not { } travel || travel <= 0)
        {
            return null;
        }

        double travelMoa = ToMoa(travel, rifle.ClickUnit);
        double half = rifle.ScopeTravelKind == ScopeTravelKind.MostUpFromZero ? travelMoa + zeroAngleMoa : travelMoa / 2;
        double most = zeroAngleMoa + half;
        double? left = rifle.RailCant is { } cant ? half + ToMoa(cant, rifle.ClickUnit) - zeroAngleMoa : null;
        bool cannot = rifle.RailCant is { } c && ToMoa(c, rifle.ClickUnit) > most;
        double? farthest = left is { } up && reach is not null ? Farthest(reach, up) : null;
        return new MountFigures(zeroAngleMoa, half, most, left, farthest, cannot);
    }

    /// <summary>The last range of a trajectory whose elevation to dial is within <paramref name="leftMoa"/>, or null where every row is.</summary>
    public static double? Farthest(Trajectory trajectory, double leftMoa)
    {
        ArgumentNullException.ThrowIfNull(trajectory);
        double? last = null;
        foreach (var point in trajectory.Points.Where(p => p.RangeYards > 0))
        {
            // The solver's DropMoa is positive when the bullet is low, the elevation to dial up.
            if (point.DropMoa > leftMoa)
            {
                return last ?? 0;
            }

            last = point.RangeYards;
        }

        return null;
    }

    /// <summary>
    /// The figures in words for the rifle page, on the computer and the phone alike, in the scope's unit; the load names whose flight the
    /// zero and the reach are worked out for.
    /// </summary>
    public static IReadOnlyList<string> Lines(Rifle rifle, string load, MountFigures figures, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        ArgumentNullException.ThrowIfNull(figures);
        ArgumentNullException.ThrowIfNull(units);
        string unit = rifle.ClickUnit == AngularUnit.Mrad ? "mil" : "MOA";
        string A(double moa) => string.Create(CultureInfo.InvariantCulture, $"{FromMoa(moa, rifle.ClickUnit):0.0} {unit}");
        var lines = new List<string>
        {
            $"The zero uses {A(figures.ZeroUsesMoa)} of elevation with {load}.",
            rifle.ScopeTravelKind == ScopeTravelKind.MostUpFromZero
                ? $"The scope goes {A(figures.HalfTravelMoa - figures.ZeroUsesMoa)} up from the zero, so {A(figures.HalfTravelMoa)} either way from its middle."
                : $"Half the travel: {A(figures.HalfTravelMoa)} either way from the middle.",
            $"The most rail cant that still zeroes: {A(figures.MostRailCantMoa)}.",
        };
        if (figures.CannotZero)
        {
            lines.Add($"The {A(ToMoa(rifle.RailCant!.Value, rifle.ClickUnit))} rail is more than the {A(figures.MostRailCantMoa)} that still zeroes: this rifle cannot be zeroed with it.");
        }
        else if (figures.LeftAboveZeroMoa is { } left)
        {
            lines.Add($"With the {A(ToMoa(rifle.RailCant!.Value, rifle.ClickUnit))} rail, {A(left)} is left above the zero, "
                + (figures.FarthestYards is { } far ? $"which reaches {units.DistanceText(far * 36)} with {load}." : $"more than {load} needs to {units.DistanceText(TravelLooked * 36)}."));
        }
        else
        {
            lines.Add("Enter the rail's cant to see how far the elevation left reaches.");
        }

        return lines;
    }

    /// <summary>How far the reach is looked for.</summary>
    public const double TravelLooked = 2500;

    /// <summary>
    /// What a row past the dialable range says, entry 456 section 1: "out of elevation, hold 1.4 mil in the reticle", the part of the
    /// elevation the turret cannot give, in the scope's unit.
    /// </summary>
    public static string? OutOfElevation(Rifle rifle, MountFigures? figures, double elevationMoa)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        if (figures?.LeftAboveZeroMoa is not { } left || elevationMoa <= left)
        {
            return null;
        }

        double hold = FromMoa(elevationMoa - left, rifle.ClickUnit);
        return string.Create(CultureInfo.InvariantCulture, $"out of elevation, hold {hold:0.0} {(rifle.ClickUnit == AngularUnit.Mrad ? "mil" : "MOA")} in the reticle");
    }
}

/// <summary>A tracking test worked out: the sight scale factor, the error, and what that error means in words.</summary>
public sealed record TrackingResult(double ScaleFactor, double ErrorPercent, string Verdict);

/// <summary>
/// The sight scale factor, entry 456 section 2, from Alan's pissinhot.com SSF calculator: a tall-target tracking test (CATS style) dials
/// from one line to another a known angle apart; the factor is the expected angle over the turret's reading, and the error is the reading's
/// difference from the expected as a percentage, positive when the scope under-tracks (each click moves less than marked). Every click
/// GroupLab gives is then the true angle over the factor.
/// </summary>
public static class ScopeTracking
{
    /// <summary>The test's usual distance between the target's lines: 20.0 mil, or 66.0 MOA.</summary>
    public const double ExpectedMil = 20.0, ExpectedMoa = 66.0;

    /// <summary>The test, in plain words.</summary>
    public const string Steps = "Hang a tall target with two lines a known angle apart (20 mil or 66 MOA at the distance you shoot it) and level the reticle "
        + "with the target. Measure the distance exactly. Aim at the top line, dial down until the reticle sits on the bottom line, and read how far the "
        + "turret turned. Type that reading here.";

    public static TrackingResult Work(double expected, double turretReading)
    {
        if (!(expected > 0) || !(turretReading > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(turretReading), "Both the expected angle and the turret's reading must be above zero.");
        }

        double factor = expected / turretReading;
        double error = (turretReading - expected) / expected * 100;
        double size = Math.Abs(error);
        string verdict = size < 0.5 ? "Essentially perfect: under half a percent."
            : size < 2 ? "Normal for a quality scope: under 2 percent."
            : "A significant tracking error: 2 percent or more.";
        string way = error > 0 ? " Each click moves less than marked." : error < 0 ? " Each click moves more than marked." : "";
        return new TrackingResult(factor, error, verdict + (size < 0.5 ? "" : way));
    }

    /// <summary>"SSF 0.980 applied", said once beside clicks the factor has corrected, or null where it is 1.</summary>
    public static string? Applied(double? factor) => factor is { } f && Math.Abs(f - 1) > 5e-4
        ? string.Create(CultureInfo.InvariantCulture, $"SSF {f:0.000} applied")
        : null;
}

/// <summary>
/// Top Gun, entry 456 section 3, from Alan's pissinhot.com calculator: Bryan Litz's trend (Modern Advancements in Long Range Shooting,
/// Volume III, chapter 3, 2022) of the expected average group of a conventional bolt rifle with quality ammunition from a stable rest,
/// group (MOA) = (muzzle energy, ft lb / rifle weight as fired, lb) / 200, with muzzle energy = bullet weight, gr, times velocity squared
/// over 450240. A trend with real rifles on both sides of it: never a pass or a fail.
/// <para>
/// <b>What it measures is not confirmed</b> (request 103): how many shots a group is and whether it is extreme spread. Until it is, the figure is
/// shown as Litz states it and never converted or set beside GroupLab's own figures; <see cref="MetricConfirmed"/> says so in one place.
/// </para>
/// </summary>
public static class TopGun
{
    /// <summary>False until Alan confirms the shot count and the measure (request 103); the comparison waits on it.</summary>
    public const bool MetricConfirmed = false;

    public static double MuzzleEnergyFtLb(double bulletGrains, double velocityFps) => bulletGrains * velocityFps * velocityFps / 450240;

    public static double GroupMoa(double bulletGrains, double velocityFps, double rifleWeightLb) =>
        rifleWeightLb > 0 ? MuzzleEnergyFtLb(bulletGrains, velocityFps) / rifleWeightLb / 200 : double.NaN;

    /// <summary>The figure for a rifle and load, or null where either lacks what it needs.</summary>
    public static double? For(Rifle? rifle, Load? load) =>
        rifle?.WeightAsFiredLb is > 0 and var weight && load?.BulletWeightGrains is > 0 and var grains && load.MuzzleVelocityFps is > 0 and var fps
            ? GroupMoa(grains, fps, weight)
            : null;

    /// <summary>"Top Gun expects about 0.82 MOA average group for this rifle and load (Bryan Litz's trend, not a limit)."</summary>
    public static string Says(double moa, string what = "this rifle and load") =>
        string.Create(CultureInfo.InvariantCulture, $"Top Gun expects about {moa:0.00} MOA average group for {what} (Bryan Litz's trend, not a limit).");

    /// <summary>
    /// Compare loads' Top Gun lines, entry 456 section 3: one per load the compared sessions name, with the rifle each names, where both have
    /// what it needs; the same load in two rifles is two lines.
    /// </summary>
    public static IReadOnlyList<string> Lines(IEnumerable<(string? Rifle, string? Load)> named, RecordBook book)
    {
        ArgumentNullException.ThrowIfNull(named);
        ArgumentNullException.ThrowIfNull(book);
        var lines = new List<string>();
        foreach (var (rifle, load) in named.Distinct())
        {
            if (For(book.FindRifle(rifle), book.FindLoad(load)) is { } moa)
            {
                lines.Add(Says(moa, $"{load} in {rifle}"));
            }
        }

        return lines;
    }

    /// <summary>What Top Gun is, said once wherever its figure is shown, on the computer and the phone.</summary>
    public const string Note = "A trend line from many rifles, with real rifles on both sides of it: a starting point, not a verdict on yours. "
        + "Its group's shot count and measure are being checked, so it is not set beside GroupLab's own figures yet.";

    /// <summary>
    /// What is usually checked when a rifle groups clearly worse than the trend: entry 456's list, with no pseudoscience. Shown only once the
    /// metric is confirmed and the group's own interval is clear of the trend.
    /// </summary>
    public const string WorseThanTrend = "Clearly wider than the trend: what is usually checked is the action's bedding and its return to battery, the crown, "
        + "the scope's mount and rings, the barrel, how evenly the bullet is engraved, and the technique and the rest.";
}
