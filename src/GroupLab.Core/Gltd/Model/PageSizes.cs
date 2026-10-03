namespace GroupLab.Core.Gltd.Model;

/// <summary>Standard page dimensions of TARGET-SCHEMA.md section 3.2, in dmm.</summary>
public static class PageSizes
{
    public static (int Width, int Height)? Standard(PageSize size) => size switch
    {
        PageSize.Letter => (2159, 2794),
        PageSize.Legal => (2159, 3556),
        PageSize.Tabloid => (2794, 4318),
        PageSize.A5 => (1480, 2100),
        PageSize.A4 => (2100, 2970),
        PageSize.A3 => (2970, 4200),
        PageSize.A6 => (1050, 1480),
        PageSize.Label4x6 => (1016, 1524),
        PageSize.Label100x150 => (1000, 1500),
        _ => null,
    };

    /// <summary>Roll presets fix the width and leave the height to the design.</summary>
    public static int? RollWidth(PageSize size) => size switch
    {
        PageSize.Roll24 => 6096,
        PageSize.Roll36 => 9144,
        PageSize.Roll42 => 10668,
        _ => null,
    };

    /// <summary>
    /// The label sizes of NOTES-FROM-PLANNING.md entry 358 section 1, which a thermal label printer takes on a roll of die-cut labels. Letter
    /// and A4 are thermal sizes too, on the wider printers, but they are not labels.
    /// </summary>
    public static bool IsLabel(PageSize size) => size is PageSize.A6 or PageSize.Label4x6 or PageSize.Label100x150;
}
