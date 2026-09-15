using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Publication;

/// <summary>
/// A target sheet's overall size as the person who shot it stated it, NOTES-FROM-PLANNING.md entry 37 section 5: the two numbers in the order
/// written, the unit written, and the text they were read from. "Action Target PR-BE6 17.5x23”" is a scale reference that needs no grid and
/// no measuring, from the one person who knows which target it was, and it is used as stated rather than looked up in a table of
/// third-party target sizes.
/// </summary>
public sealed partial record StatedSheetSize(double Width, double Height, string Unit, string Text)
{
    /// <summary>Where the size is read from in a submission, and recorded as coming from in its provenance.</summary>
    public const string Source = "answers.notes";

    public double WidthInches => Inches(Width);

    public double HeightInches => Inches(Height);

    private double Inches(double value) => Unit switch
    {
        "mm" => value / 25.4,
        "cm" => value / 2.54,
        _ => value,
    };

    /// <summary>
    /// The one sheet size in a free-text note, or null. A size is two numbers joined by x or ×, followed by a unit: in, inch, inches, an
    /// inch mark (<c>"</c>, <c>″</c>, or the curly <c>”</c> a phone keyboard puts in its place, as both of the first notes carry), mm or
    /// cm. A note with no such size, or with two, gives nothing. So does a size with no unit, because a guessed unit is a scale error of
    /// 2.54 or 25.4 times.
    /// </summary>
    public static StatedSheetSize? Parse(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes) || Size().Matches(notes) is not { Count: 1 } matches)
        {
            return null;
        }

        var m = matches[0];
        double width = double.Parse(m.Groups["w"].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
        double height = double.Parse(m.Groups["h"].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
        string unit = m.Groups["unit"].Value.ToLowerInvariant() switch
        {
            "mm" => "mm",
            "cm" => "cm",
            _ => "in",
        };
        return width > 0 && height > 0 ? new StatedSheetSize(width, height, unit, m.Value) : null;
    }

    /// <summary>The size as a provenance record carries it, with where it came from.</summary>
    public JsonObject ToJson() => new()
    {
        ["source"] = Source,
        ["text"] = Text,
        ["width"] = Width,
        ["height"] = Height,
        ["unit"] = Unit,
    };

    /// <summary>
    /// The stated size for an image published with a provenance record beside it that lists the image by name, as <c>grouplab intake</c>
    /// writes it into <c>grouplab-testdata</c>; null for any other image, or a record that states none or cannot be read.
    /// </summary>
    public static StatedSheetSize? Beside(string imagePath)
    {
        ArgumentNullException.ThrowIfNull(imagePath);
        string record = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(imagePath)) ?? "", PublicationCheck.ProvenanceFile);
        try
        {
            if (!File.Exists(record) || JsonNode.Parse(File.ReadAllText(record)) is not JsonObject provenance)
            {
                return null;
            }

            string name = Path.GetFileName(imagePath);
            bool listed = (provenance["files"] as JsonArray)?.OfType<JsonObject>().Any(f => (string?)f["storedName"] == name) == true;
            if (!listed || provenance["statedSheetSize"] is not JsonObject size
                || size["width"]?.GetValueKind() != JsonValueKind.Number || size["height"]?.GetValueKind() != JsonValueKind.Number
                || (string?)size["unit"] is not ("in" or "mm" or "cm") || (string?)size["text"] is not { } text)
            {
                return null;
            }

            return new StatedSheetSize(size["width"]!.GetValue<double>(), size["height"]!.GetValue<double>(), (string)size["unit"]!, text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"(?<![\d.])(?<w>\d+(?:\.\d+)?)\s*[x×]\s*(?<h>\d+(?:\.\d+)?)\s*(?<unit>inches\b|inch\b|in\b|""|″|”|mm\b|cm\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Size();
}
