using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering.Pdf;

namespace GroupLab.Core.Rendering;

/// <summary>
/// Where a bull's label is printed. TARGET-SCHEMA.md section 3.5 leaves the default position to a rendering
/// convention; this one is docs/SPEC-ERRATA.md C6: left of the outermost disc, vertically centred, 25 dmm Helvetica.
/// The size and side were measured against every built-in sheet: it is the only fixed placement that clears them all,
/// with 3.1 dmm to spare beside the top-left code of GL-LR300-T, whereas a larger label there, or any label below the
/// disc on GL-CF30-LTR, lands on a code. The validator reads the same boxes, because section 7 checks labels for
/// overlap like any other printed element.
/// </summary>
public static class LabelLayout
{
    /// <summary>25 dmm, which puts a capital about 18 dmm tall.</summary>
    public const long FontSize = 50;

    /// <summary>15 dmm between the label and the outermost disc.</summary>
    public const long Gap = 30;

    public static long CapHeight => FontSize * HelveticaMetrics.CapHeight / 1000;

    /// <summary>A sheet with one aiming mark, the zeroing sheets of TARGET-LIBRARY.md section 5, prints no label.</summary>
    public static bool Printed(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Bulls.Count > 1;
    }

    /// <summary>The label as drawn, or null when the bull has no label.</summary>
    public static TextRun? Run(Bull bull, int outerDiameter, Gltd.Binary.Rgb colour)
    {
        ArgumentNullException.ThrowIfNull(bull);
        if (bull.Label is null)
        {
            return null;
        }

        return bull.LabelOffset is { } offset
            ? new TextRun(SceneLayer.Labels, colour, 2L * (bull.X + offset.X), 2L * (bull.Y + offset.Y), FontSize, bull.Label, TextAnchor.Left)
            : new TextRun(SceneLayer.Labels, colour, (2L * bull.X) - outerDiameter - Gap, (2L * bull.Y) + (CapHeight / 2), FontSize, bull.Label, TextAnchor.Right);
    }

    /// <summary>The ink box of <paramref name="run"/>, from its baseline up to the cap height, in half-dmm.</summary>
    public static Box2 Box(TextRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        long width = HelveticaMetrics.TextWidth(run.Text, run.FontSize);
        long left = run.Anchor switch
        {
            TextAnchor.Right => run.X - width,
            TextAnchor.Centre => run.X - (width / 2),
            _ => run.X,
        };
        return new Box2(left, run.Baseline - (run.FontSize * HelveticaMetrics.CapHeight / 1000), left + width, run.Baseline);
    }
}
