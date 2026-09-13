using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering.Pdf;

namespace GroupLab.Core.Rendering;

/// <summary>A rendered PDF with the scenes it was written from, or the diagnostics that stopped it.</summary>
public sealed record RenderResult(byte[]? Pdf, IReadOnlyList<Scene> Pages, string? DefinitionId, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// The PDF renderer of PHASE0-BRIEF.md deliverable 4: TARGET-SCHEMA.md section 3 drawn under the print constraints of
/// section 7. A definition with validation errors is refused unless <see cref="RenderOptions.AllowInvalid"/> is set for
/// inspection, because a sheet that prints but cannot be analysed is worse than no sheet.
/// </summary>
public static class TargetRenderer
{
    public static RenderResult Render(TargetDefinition definition, RenderOptions? options = null)
    {
        options ??= new RenderOptions();
        var scenes = SceneBuilder.Build(definition, options);
        if (scenes.Pages.Count == 0)
        {
            return new RenderResult(null, [], scenes.DefinitionId, scenes.Diagnostics);
        }

        return new RenderResult(PdfWriter.Write(scenes.Pages, options.Scale), scenes.Pages, scenes.DefinitionId, scenes.Diagnostics);
    }
}
