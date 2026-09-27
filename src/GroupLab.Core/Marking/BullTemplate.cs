using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 228 section 1.4: where the bulls are on a commercial target, kept so the next sheet of the same target
/// needs only the bulls nudged into place. The bulls are stored in inches from the first, as the scale of the sheet it was made from put
/// them; on a new photograph the person taps the first two bulls and the rest are placed by the one turn, scale and shift that carries the
/// template's first two onto them.
/// </summary>
public sealed record BullTemplate(string Name, IReadOnlyList<PointD> BullsInches)
{
    /// <summary>The bulls of a marking with a scale, as a template; null without a scale or with fewer than two bulls.</summary>
    public static BullTemplate? From(string name, MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Scale is not { } scale || state.Bulls.Count < 2)
        {
            return null;
        }

        var first = scale.ToTarget(state.Bulls[0].Image);
        return new BullTemplate(name, [.. state.Bulls.Select(b => scale.ToTarget(b.Image)).Select(p => new PointD(p.X - first.X, p.Y - first.Y))]);
    }

    /// <summary>Every bull's place in a new image, from where the person tapped the first two.</summary>
    public IReadOnlyList<PointD> Place(PointD first, PointD second)
    {
        if (BullsInches.Count < 2)
        {
            return [first];
        }

        // The similarity z -> a z + b, in complex numbers, that takes template bull 0 to first and bull 1 to second.
        var t0 = BullsInches[0];
        var t1 = BullsInches[1];
        double tx = t1.X - t0.X, ty = t1.Y - t0.Y, ix = second.X - first.X, iy = second.Y - first.Y;
        double norm = (tx * tx) + (ty * ty);
        if (norm <= 0)
        {
            return [first, second];
        }

        double ar = ((ix * tx) + (iy * ty)) / norm, ai = ((iy * tx) - (ix * ty)) / norm;
        return [.. BullsInches.Select(p =>
        {
            double dx = p.X - t0.X, dy = p.Y - t0.Y;
            return new PointD(first.X + (ar * dx) - (ai * dy), first.Y + (ai * dx) + (ar * dy));
        })];
    }
}
