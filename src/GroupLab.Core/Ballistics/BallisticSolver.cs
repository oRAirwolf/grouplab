namespace GroupLab.Core.Ballistics;

/// <summary>
/// What a trajectory is computed from: the load, the sight, the zero, the air and the shot. Lengths are in inches, ranges in yards, speeds in
/// ft/s, weights in grains, and the crosswind is signed, positive from the shooter's left, drifting the bullet to the right. The azimuth is the
/// direction of fire, 0 north and 90 east, which only the Coriolis vertical term needs.
/// </summary>
public sealed record BallisticInput(
    double BallisticCoefficient,
    DragModel Model,
    double MuzzleVelocityFps,
    double BulletWeightGrains,
    double SightHeightInches = 1.5,
    double ZeroRangeYards = 100,
    double TemperatureF = 59,
    double? PressureInHg = null,
    double AltitudeFt = 0,
    double HumidityPct = 50,
    double CrosswindMph = 0,
    double AngleDegrees = 0,
    ReferenceAtmosphere Reference = ReferenceAtmosphere.Icao,
    double? TwistInches = null,
    int TwistDirection = 1,
    double? BulletDiameterInches = null,
    double? BulletLengthInches = null,
    double? LatitudeDegrees = null,
    double AzimuthDegrees = 0)
{
    /// <summary>Station pressure: as stated, or from the altitude by the ICAO troposphere.</summary>
    public double StationPressureInHg => PressureInHg ?? Atmosphere.PressureFromAltitude(AltitudeFt);
}

/// <summary>
/// One row of a trajectory, at a range along the line of sight. Drop is the height of the bullet above the line of sight, negative below it,
/// and its angle is positive when the bullet is low, the elevation to dial. Wind is the crosswind's drift, positive to the right. Spin drift and
/// the two Coriolis terms are present only when their inputs are; the vertical term is positive up.
/// </summary>
public sealed record TrajectoryPoint(
    double RangeYards,
    double VelocityFps,
    double EnergyFtLb,
    double DropInches,
    double DropMoa,
    double DropMil,
    double TimeOfFlight,
    double WindInches,
    double WindMoa,
    double WindMil,
    double Mach,
    double? SpinDriftInches = null,
    double? CoriolisInches = null,
    double? CoriolisVerticalInches = null);

/// <summary>A trajectory: its rows, the stability factor where the bullet is described, the zero's launch angle, and what is not modelled.</summary>
public sealed record Trajectory(IReadOnlyList<TrajectoryPoint> Points, double? Stability, double ZeroAngleMoa, IReadOnlyList<string> NotModelled);

/// <summary>
/// GroupLab's ballistic solver, NOTES-FROM-PLANNING.md entry 110 section 2 and DESIGN.md section 16: a point-mass trajectory, ported from
/// reference/ballistics-js/ballistics.js. The drag deceleration is (ρ/ρ₀) Cd(M) V² 0.00020856 / BC, the constant being π × 0.0764742 / 1152
/// for a BC in lb/in² and ρ₀ as mass density in lb/ft³. The downrange and vertical motion are integrated by fourth-order Runge-Kutta in steps
/// of half a millisecond, and the crosswind's drift comes from the lag rule, wind × (t − x / V₀).
/// <para>
/// <b>Where the port differs from the JavaScript, on purpose,</b> entry 110 sections 2b to 2e:
/// </para>
/// <list type="bullet">
/// <item><b>Shooting angle.</b> The JavaScript tilted the launch but measured drop from the horizontal line through the sight, so any angle
/// reported the line of sight's own rise, about 300 MOA per 5 degrees. Here range is measured along the line of sight and drop perpendicular to
/// it, and the rifle is zeroed on the flat, as it is at a range.</item>
/// <item><b>Aerodynamic jump is not modelled.</b> The JavaScript's term was not a published formula and ran the wrong way with stability.</item>
/// <item><b>The Coriolis vertical term with its sign corrected</b>: fire toward the east strikes high, where the JavaScript had it low
/// (docs/QUESTIONS-FOR-PLANNING.md question 24, answered by entry 111 section 2).</item>
/// <item><b>The standard G1 table</b>, where the JavaScript's was not the standard function above Mach 0.85 (question 25, answered by entry 111
/// section 1), and the standard G7 table's full 84 points.</item>
/// <item><b>A signed crosswind</b>, where the JavaScript's extended solve added a wind from the right as drift to the right.</item>
/// <item><b>Exact ranges.</b> Each row is interpolated to its range, where the JavaScript recorded the first step at or past it, up to 1.5 ft
/// late.</item>
/// <item><b>The zero by RK4</b>, on the same trajectory it is applied to, where the JavaScript zeroed with first-order steps.</item>
/// <item><b>Full precision</b>, rounding only for display.</item>
/// <item><b>Miller's pressure correction</b> and <b>the BC's reference atmosphere</b> as a stated input.</item>
/// </list>
/// <para>
/// <see cref="Solve"/> with <c>javaScriptCompatible</c> reproduces the JavaScript's flat-fire sampling and zero, for the transcription check
/// of docs/BALLISTICS-VALIDATION.md section 1 only. No pseudoscience: the solver models physics only, as DESIGN.md section 16 says.
/// </para>
/// </summary>
public static class BallisticSolver
{
    public const double Gravity = 32.17405;
    public const double DragConstant = 0.00020856;
    public const double TimeStep = 0.0005;
    public const double MoaPerRadian = 180 / Math.PI * 60;
    public const double MilPerMoa = 0.290888;

    /// <summary>
    /// The sentence every display of the solver's output carries, entry 110 section 2c: aerodynamic jump stays out until its fit is checked
    /// against the page it is published on (entry 111 section 2).
    /// </summary>
    public static IReadOnlyList<string> NotModelled { get; } =
    [
        "Aerodynamic jump is not modelled.",
    ];

    /// <summary>The drag deceleration in ft/s² at a speed, Mach number and density ratio.</summary>
    public static double DragDeceleration(double velocity, double mach, double ballisticCoefficient, DragModel model, double densityRatio, bool javaScriptTables = false) =>
        densityRatio * DragTables.Cd(mach, model, javaScriptTables) * velocity * velocity * DragConstant / ballisticCoefficient;

    /// <summary>
    /// The trajectory to <paramref name="maxRangeYards"/>, a row every <paramref name="stepYards"/> from the muzzle, range 0, onward.
    /// </summary>
    public static Trajectory Solve(BallisticInput input, double maxRangeYards, double stepYards, bool javaScriptCompatible = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.BallisticCoefficient <= 0 || input.MuzzleVelocityFps <= 0 || input.BulletWeightGrains <= 0)
        {
            throw new ArgumentException("The ballistic coefficient, muzzle velocity and bullet weight must all be positive.", nameof(input));
        }

        if (stepYards <= 0 || maxRangeYards < 0)
        {
            throw new ArgumentException("The step must be positive and the range not negative.", nameof(stepYards));
        }

        var air = new Air(input, javaScriptCompatible);
        var points = javaScriptCompatible ? Legacy(input, air, maxRangeYards, stepYards, out double zero) : Exact(input, air, maxRangeYards, stepYards, out zero);
        double? stability = input is { TwistInches: { } twist, BulletDiameterInches: { } diameter, BulletLengthInches: { } length }
            ? Stability.MillerStability(twist, diameter, length, input.BulletWeightGrains, input.MuzzleVelocityFps, input.TemperatureF, javaScriptCompatible ? 29.92 : input.StationPressureInHg)
            : null;
        if (stability is not null || input.LatitudeDegrees is not null)
        {
            points = [.. points.Select(p => p with
            {
                SpinDriftInches = stability is { } sg ? Stability.SpinDriftInches(sg, p.TimeOfFlight, input.TwistDirection) : null,
                CoriolisInches = input.LatitudeDegrees is { } latitude ? Stability.CoriolisHorizontalInches(latitude, p.RangeYards * 3, p.TimeOfFlight) : null,
                CoriolisVerticalInches = input.LatitudeDegrees is { } lat && !javaScriptCompatible
                    ? Stability.CoriolisVerticalInches(lat, input.AzimuthDegrees, p.TimeOfFlight > 0 ? p.RangeYards * 3 / p.TimeOfFlight : input.MuzzleVelocityFps, p.TimeOfFlight)
                    : null,
            })];
        }

        return new Trajectory(points, stability, zero * MoaPerRadian, NotModelled);
    }

    /// <summary>The air the bullet flies through, its density against the coefficient's reference and its speed of sound, and which tables it is flown on.</summary>
    private sealed class Air(BallisticInput input, bool javaScriptTables)
    {
        public bool JavaScriptTables { get; } = javaScriptTables;

        public double DensityRatio { get; } = Atmosphere.DensityRatio(input.TemperatureF, input.StationPressureInHg, input.HumidityPct, input.Reference);

        public double SpeedOfSound { get; } = Atmosphere.SpeedOfSound(input.TemperatureF, input.StationPressureInHg, input.HumidityPct);
    }

    /// <summary>The state's rate of change: position by velocity, velocity by drag along the velocity and gravity.</summary>
    private static (double, double, double, double) Derivatives(double vx, double vy, BallisticInput input, Air air)
    {
        double v = Math.Sqrt((vx * vx) + (vy * vy));
        if (v < 1)
        {
            return (0, 0, 0, -Gravity);
        }

        double drag = DragDeceleration(v, v / air.SpeedOfSound, input.BallisticCoefficient, input.Model, air.DensityRatio, air.JavaScriptTables);
        return (vx, vy, -drag * (vx / v), (-drag * (vy / v)) - Gravity);
    }

    /// <summary>One fourth-order Runge-Kutta step of the state.</summary>
    private static (double X, double Y, double Vx, double Vy) Step((double X, double Y, double Vx, double Vy) s, BallisticInput input, Air air)
    {
        const double dt = TimeStep;
        var k1 = Derivatives(s.Vx, s.Vy, input, air);
        var k2 = Derivatives(s.Vx + (0.5 * dt * k1.Item3), s.Vy + (0.5 * dt * k1.Item4), input, air);
        var k3 = Derivatives(s.Vx + (0.5 * dt * k2.Item3), s.Vy + (0.5 * dt * k2.Item4), input, air);
        var k4 = Derivatives(s.Vx + (dt * k3.Item3), s.Vy + (dt * k3.Item4), input, air);
        return (
            s.X + (dt / 6 * (k1.Item1 + (2 * k2.Item1) + (2 * k3.Item1) + k4.Item1)),
            s.Y + (dt / 6 * (k1.Item2 + (2 * k2.Item2) + (2 * k3.Item2) + k4.Item2)),
            s.Vx + (dt / 6 * (k1.Item3 + (2 * k2.Item3) + (2 * k3.Item3) + k4.Item3)),
            s.Vy + (dt / 6 * (k1.Item4 + (2 * k2.Item4) + (2 * k3.Item4) + k4.Item4)));
    }

    /// <summary>
    /// The height above the line of sight where the trajectory, launched at <paramref name="launch"/> above a level line of sight, reaches
    /// <paramref name="rangeFt"/>, by RK4 and interpolated between the two steps either side (entry 110 section 2e).
    /// </summary>
    private static double HeightAtRange(double launch, double rangeFt, BallisticInput input, Air air)
    {
        var s = (X: 0.0, Y: -input.SightHeightInches / 12, Vx: input.MuzzleVelocityFps * Math.Cos(launch), Vy: input.MuzzleVelocityFps * Math.Sin(launch));
        for (double t = 0; t < 10; t += TimeStep)
        {
            var next = Step(s, input, air);
            if (next.X >= rangeFt)
            {
                return s.Y + ((next.Y - s.Y) * (rangeFt - s.X) / (next.X - s.X));
            }

            s = next;
        }

        return s.Y;
    }

    /// <summary>The launch angle above the line of sight that crosses it again at the zero range, by bisection, on the flat.</summary>
    private static double Zero(BallisticInput input, Air air)
    {
        double rangeFt = input.ZeroRangeYards * 3;
        if (rangeFt <= 0)
        {
            return 0;
        }

        double low = 0, high = 0.1;
        for (int i = 0; i < 60; i++)
        {
            double middle = (low + high) / 2;
            if (HeightAtRange(middle, rangeFt, input, air) < 0)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return (low + high) / 2;
    }

    /// <summary>
    /// The port's trajectory. The rifle is zeroed on the flat; the line of sight is then tilted by the shooting angle, with the muzzle the sight
    /// height below it and the bore at the zero's angle above it. Each row is where the bullet crosses the perpendicular to the line of sight
    /// at the row's range, interpolated between the steps either side, with drop measured perpendicular to the line of sight.
    /// </summary>
    private static List<TrajectoryPoint> Exact(BallisticInput input, Air air, double maxRangeYards, double stepYards, out double zero)
    {
        zero = Zero(input, air);
        double angle = input.AngleDegrees * Math.PI / 180, cos = Math.Cos(angle), sin = Math.Sin(angle);
        double sight = input.SightHeightInches / 12, v0 = input.MuzzleVelocityFps, windFps = input.CrosswindMph * 5280 / 3600;
        var s = (X: sight * sin, Y: -sight * cos, Vx: v0 * Math.Cos(zero + angle), Vy: v0 * Math.Sin(zero + angle));
        double Along((double X, double Y, double Vx, double Vy) p) => (p.X * cos) + (p.Y * sin);
        double Above((double X, double Y, double Vx, double Vy) p) => (-p.X * sin) + (p.Y * cos);

        var points = new List<TrajectoryPoint>();
        void Record(double rangeYards, double t, (double X, double Y, double Vx, double Vy) p)
        {
            double v = Math.Sqrt((p.Vx * p.Vx) + (p.Vy * p.Vy)), dropInches = Above(p) * 12, rangeInches = rangeYards * 36;
            double lag = rangeYards > 0 ? t - (rangeYards * 3 / v0) : 0, windInches = windFps * lag * 12;
            double dropMoa = rangeYards > 0 ? -dropInches / rangeInches * MoaPerRadian : 0, windMoa = rangeYards > 0 ? windInches / rangeInches * MoaPerRadian : 0;
            points.Add(new TrajectoryPoint(rangeYards, v, input.BulletWeightGrains * v * v / 450240, dropInches, dropMoa, dropMoa * MilPerMoa,
                t, windInches, windMoa, windMoa * MilPerMoa, v / air.SpeedOfSound));
        }

        // Row 0 is the muzzle, the sight height below the line of sight.
        Record(0, 0, s);
        int row = 1;
        double time = 0;
        while (row * stepYards <= maxRangeYards + 1e-9 && time < 15)
        {
            var next = Step(s, input, air);
            double from = Along(s), to = Along(next);
            while (row * stepYards <= maxRangeYards + 1e-9 && to >= row * stepYards * 3)
            {
                double f = (row * stepYards * 3 - from) / (to - from);
                var at = (s.X + (f * (next.X - s.X)), s.Y + (f * (next.Y - s.Y)), s.Vx + (f * (next.Vx - s.Vx)), s.Vy + (f * (next.Vy - s.Vy)));
                Record(row * stepYards, time + (f * TimeStep), at);
                row++;
            }

            s = next;
            time += TimeStep;
            if ((s.Vx * cos) + (s.Vy * sin) < 10)
            {
                break;
            }
        }

        return points;
    }

    /// <summary>
    /// The JavaScript's flat-fire trajectory as it computes it, for the transcription check alone: the zero found with first-order steps and
    /// no interpolation, and each row recorded at the first RK4 step at or past its range, with its nominal range used in the angles.
    /// </summary>
    private static List<TrajectoryPoint> Legacy(BallisticInput input, Air air, double maxRangeYards, double stepYards, out double zero)
    {
        double sight = input.SightHeightInches / 12, v0 = input.MuzzleVelocityFps, windFps = input.CrosswindMph * 5280 / 3600, angle = input.AngleDegrees * Math.PI / 180;
        double RunToRange(double launch, double rangeYards)
        {
            double target = rangeYards * 3, x = 0, y = -sight, vx = v0 * Math.Cos(launch + angle), vy = v0 * Math.Sin(launch + angle), t = 0;
            while (x < target && t < 10)
            {
                double v = Math.Sqrt((vx * vx) + (vy * vy));
                double drag = DragDeceleration(v, v / air.SpeedOfSound, input.BallisticCoefficient, input.Model, air.DensityRatio, air.JavaScriptTables);
                vx += -drag * (vx / v) * TimeStep;
                vy += ((-drag * (vy / v)) - Gravity) * TimeStep;
                x += vx * TimeStep;
                y += vy * TimeStep;
                t += TimeStep;
            }

            return y;
        }

        zero = 0;
        if (input.ZeroRangeYards > 0 && Math.Abs(RunToRange(0, input.ZeroRangeYards)) > 0.0001)
        {
            double low = 0, high = 0.1;
            for (int i = 0; i < 50; i++)
            {
                double middle = (low + high) / 2;
                if (RunToRange(middle, input.ZeroRangeYards) < 0)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            zero = (low + high) / 2;
        }

        var s = (X: 0.0, Y: -sight, Vx: v0 * Math.Cos(zero + angle), Vy: v0 * Math.Sin(zero + angle));
        double time = 0, next = 0;
        var points = new List<TrajectoryPoint>();
        while (s.X <= (maxRangeYards * 3) + 3 && time < 15)
        {
            if (s.X / 3 >= next - 0.01)
            {
                double v = Math.Sqrt((s.Vx * s.Vx) + (s.Vy * s.Vy)), dropInches = s.Y * 12, rangeInches = next * 36;
                double dropMoa = next > 0 ? -(dropInches / rangeInches) * MoaPerRadian : 0;
                double lag = next > 0 ? time - (next * 3 / v0) : 0, windInches = windFps * lag * 12;
                double windMoa = next > 0 ? windInches / rangeInches * MoaPerRadian : 0;
                points.Add(new TrajectoryPoint(next, v, input.BulletWeightGrains * v * v / 450240, dropInches, dropMoa, dropMoa * MilPerMoa,
                    time, windInches, windMoa, windMoa * MilPerMoa, v / air.SpeedOfSound));
                next += stepYards;
                if (next > maxRangeYards)
                {
                    break;
                }
            }

            s = Step(s, input, air);
            time += TimeStep;
            if (s.Vx < 10)
            {
                break;
            }
        }

        return points;
    }
}
