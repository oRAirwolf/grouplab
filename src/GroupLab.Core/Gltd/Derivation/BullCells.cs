using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

/// <summary>
/// Each bull's cell, page dmm: centred on the bull, the scoring grid's pitch across and down. Render-and-difference looks for holes only
/// inside these, which is the position prior of docs/DETECTION-PIPELINE.md stage S8, so they are the region the analyser reads. The renderer
/// reads the same cells to keep the printed name out of that region, NOTES-FROM-PLANNING.md entry 77 section 5, and the two cannot disagree
/// because there is one rule.
/// </summary>
public static class BullCells
{
    public static IReadOnlyList<(double X, double Y, double HalfWidth, double HalfHeight)> Of(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        static double Pitch(IEnumerable<int> values)
        {
            var distinct = values.Distinct().Order().ToList();
            var gaps = distinct.Zip(distinct.Skip(1), (a, b) => b - a).Where(g => g > 0).ToList();
            return gaps.Count == 0 ? 0 : gaps.Min();
        }

        var scoring = definition.Bulls.Where(b => b.Scoring).ToList();
        double pitchX = Pitch(scoring.Select(b => b.X)), pitchY = Pitch(scoring.Select(b => b.Y));
        double pitch = Math.Max(pitchX, pitchY);
        pitchX = pitchX > 0 ? pitchX : pitch;
        pitchY = pitchY > 0 ? pitchY : pitch;
        return [.. definition.Bulls.Select(b => ((double)b.X, (double)b.Y, pitchX / 2, pitchY / 2))];
    }
}
