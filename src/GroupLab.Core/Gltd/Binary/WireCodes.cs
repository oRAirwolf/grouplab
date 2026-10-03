using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Binary;

/// <summary>The byte values TARGET-SCHEMA.md sections 5.1, 5.2 and 5.5 assign to each enumeration.</summary>
internal static class WireCodes
{
    public const ushort ExplicitLayoutFlag = 1 << 0;
    public const ushort DeflateFlag = 1 << 1;
    public const ushort DataBlockFlag = 1 << 6;
    public const ushort TilingFlag = 1 << 7;
    public const ushort GridsFlag = 1 << 8;

    /// <summary>Bits 2 to 5: cell, label, print and extension blocks, which have no byte layout yet.</summary>
    public const ushort UnspecifiedBlockFlags = 0b0011_1100;

    public const ushort ReservedFlags = 0xFE00;

    public const byte PaperInk = 15;

    /// <summary>Scheme byte values 0 to 3, section 5.2.</summary>
    public static readonly string[] Schemes = ["explicit", "grid-boundary-1", "grid-boundary-half-1", "field-ring-1", "grid-boundary-edge-1"];

    /// <summary>Quantum byte values 0 to 3 in dmm, section 5.4.</summary>
    public static readonly int[] QuantumDmm = [1, 2, 5, 10];

    public const byte StandardGridStyle = 1;

    /// <summary>Grid style 2, question 59: five more bytes, the drawn field's two half-extents and the whole-unit step.</summary>
    public const byte ZeroingGridStyle = 2;

    /// <summary>Grid style 3, the C3 zeroing grid of entry 251: the same five more bytes as style 2, drawn to <c>GridStyle3</c>.</summary>
    public const byte ScopeGridStyle = 3;

    /// <summary>Grid style 4, the printer check page of entries 272 and 273: no field on the wire, everything drawn to <c>GridStyle4</c>.</summary>
    public const byte CheckPageStyle = 4;

    /// <summary>The wire code of a grid's style.</summary>
    public static byte GridStyleCode(int style) => style switch { 4 => CheckPageStyle, 3 => ScopeGridStyle, 2 => ZeroingGridStyle, _ => StandardGridStyle };

    /// <summary>True for a style whose grid carries a field and a whole-unit step on the wire.</summary>
    public static bool CarriesField(byte style) => style is ZeroingGridStyle or ScopeGridStyle;

    public static byte PageCode(PageSize size) => size switch
    {
        PageSize.Custom => 0,
        PageSize.Letter => 1,
        PageSize.Legal => 2,
        PageSize.Tabloid => 3,
        PageSize.A3 => 4,
        PageSize.A4 => 5,
        PageSize.A5 => 6,
        PageSize.Roll24 => 7,
        PageSize.Roll36 => 8,
        PageSize.Roll42 => 9,
        PageSize.A6 => 10,
        PageSize.Label4x6 => 11,
        PageSize.Label100x150 => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public static PageSize? PageSizeOf(byte code) => code switch
    {
        0 => PageSize.Custom,
        1 => PageSize.Letter,
        2 => PageSize.Legal,
        3 => PageSize.Tabloid,
        4 => PageSize.A3,
        5 => PageSize.A4,
        6 => PageSize.A5,
        7 => PageSize.Roll24,
        8 => PageSize.Roll36,
        9 => PageSize.Roll42,
        10 => PageSize.A6,
        11 => PageSize.Label4x6,
        12 => PageSize.Label100x150,
        _ => null,
    };

    /// <summary>
    /// The roll presets, whose page block carries the height (TARGET-SCHEMA.md section 5.2). Entry 358 added standard sizes after them, so a
    /// code above 9 is a named size again and carries nothing.
    /// </summary>
    public static bool IsRoll(byte pageCode) => pageCode is >= 7 and <= 9;

    /// <summary>The family table of section 5.5 is in the same order as <see cref="FiducialFamily"/>.</summary>
    public static byte FamilyCode(FiducialFamily family) => (byte)family;

    public static byte GridUnitCode(GridUnit unit) => unit switch
    {
        GridUnit.Custom => 0,
        GridUnit.Moa => 1,
        GridUnit.Mil => 2,
        GridUnit.Inch => 3,
        GridUnit.Cm => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };

    public static GridUnit? GridUnitOf(byte code) => code switch
    {
        0 => GridUnit.Custom,
        1 => GridUnit.Moa,
        2 => GridUnit.Mil,
        3 => GridUnit.Inch,
        4 => GridUnit.Cm,
        _ => null,
    };
}
