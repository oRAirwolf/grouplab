namespace GroupLab.Core.Gltd.Model;

/// <summary>
/// The shape a disc of a ring set is drawn in, TARGET-SCHEMA.md section 3.4 and NOTES-FROM-PLANNING.md entry 243 section 4: a circle, as every
/// sheet had until then, or a square, which turned 45 degrees is the C bull's diamond.
/// </summary>
public enum DiscShape
{
    Circle,
    Square,
}

/// <summary>
/// A disc's outline about the bull's centre, in dmm. <see cref="Radius"/> is half the declared diameter, which for a square is half its
/// diagonal, the radius of the circle through its corners, so every check that reasons about a disc's reach by its diameter stays
/// conservative. <see cref="Rotation"/> turns a square by that many degrees, from +x toward +y: at 0 its sides are square to the page and at
/// 45 it stands on a point, its four points on the vertical and horizontal lines through the aim point.
/// </summary>
public readonly record struct Outline(double Radius, DiscShape Shape = DiscShape.Circle, int Rotation = 0)
{
    public static Outline Of(Disc disc)
    {
        ArgumentNullException.ThrowIfNull(disc);
        return new Outline(disc.Diameter / 2.0, disc.Shape, disc.Rotation);
    }

    public bool IsCircle => Shape == DiscShape.Circle;

    /// <summary>The radius of the largest circle inside: the radius itself for a circle, half the side for a square.</summary>
    public double Inscribed => IsCircle ? Radius : Radius / Math.Sqrt(2);

    private double Turn => Rotation * Math.PI / 180;

    /// <summary>How far the outline is from the centre along the ray at <paramref name="angle"/> radians, from +x toward +y.</summary>
    public double RadiusAt(double angle)
    {
        if (IsCircle)
        {
            return Radius;
        }

        double a = angle - Turn;
        return Inscribed / Math.Max(Math.Abs(Math.Cos(a)), Math.Abs(Math.Sin(a)));
    }

    /// <summary>
    /// The signed distance of the point (<paramref name="dx"/>, <paramref name="dy"/>) from the centre to the outline, negative inside, with
    /// the outline's outward normal there. For a square it is exact across each side and measured to the nearer side's line near a corner,
    /// which is what an edge fit wants: the direction ink spreads is the side's normal.
    /// </summary>
    public double Distance(double dx, double dy, out double nx, out double ny)
    {
        if (IsCircle)
        {
            double d = Math.Sqrt((dx * dx) + (dy * dy));
            (nx, ny) = d > 0 ? (dx / d, dy / d) : (1.0, 0.0);
            return d - Radius;
        }

        double c = Math.Cos(Turn), s = Math.Sin(Turn);
        double u = (dx * c) + (dy * s), v = (-dx * s) + (dy * c);
        double nu, nv, distance;
        if (Math.Abs(u) >= Math.Abs(v))
        {
            (nu, nv, distance) = (Math.Sign(u) == 0 ? 1 : Math.Sign(u), 0.0, Math.Abs(u) - Inscribed);
        }
        else
        {
            (nu, nv, distance) = (0.0, Math.Sign(v), Math.Abs(v) - Inscribed);
        }

        nx = (nu * c) - (nv * s);
        ny = (nu * s) + (nv * c);
        return distance;
    }

    /// <summary>A square's four corners relative to the centre, in order round it; none for a circle.</summary>
    public IReadOnlyList<(double X, double Y)> Corners()
    {
        if (IsCircle)
        {
            return [];
        }

        var corners = new (double X, double Y)[4];
        for (int k = 0; k < 4; k++)
        {
            double a = Turn + (Math.PI / 4) + (k * Math.PI / 2);
            corners[k] = (Radius * Math.Cos(a), Radius * Math.Sin(a));
        }

        return corners;
    }

    /// <summary>Whether this outline lies strictly inside <paramref name="outer"/>, both about the same centre.</summary>
    public bool Inside(Outline outer)
    {
        if (IsCircle)
        {
            return Radius < outer.Inscribed;
        }

        foreach (var (x, y) in Corners())
        {
            if (outer.Distance(x, y, out _, out _) >= 0)
            {
                return false;
            }
        }

        return true;
    }
}
