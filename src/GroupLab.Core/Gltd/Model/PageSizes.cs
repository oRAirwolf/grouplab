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
}
