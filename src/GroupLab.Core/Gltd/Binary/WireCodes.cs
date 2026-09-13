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
    public static readonly string[] Schemes = ["explicit", "grid-boundary-1", "grid-boundary-half-1", "field-ring-1"];

    /// <summary>Quantum byte values 0 to 3 in dmm, section 5.4.</summary>
    public static readonly int[] QuantumDmm = [1, 2, 5, 10];

    public const byte StandardGridStyle = 1;

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
        _ => null,
    };

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
