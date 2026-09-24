using System.Buffers.Binary;
using GroupLab.Core.Gltd.Derivation;

namespace GroupLab.Core.Gltd.Binary;

/// <summary>A parsed body, or the reason it was rejected.</summary>
public sealed record BodyReadResult(BodyModel? Body, string? Error);

/// <summary>
/// Writes and reads the GLTD-B body of TARGET-SCHEMA.md sections 5.2 and 5.5. Reading checks every
/// length against the bytes actually present before it reads or allocates, and rejects reserved bits,
/// unknown enumeration values and trailing bytes.
/// </summary>
public static class BodyCodec
{
    public static (byte[] Body, ushort Flags) Write(BodyModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var w = new List<byte>(96);
        ushort flags = 0;

        var page = model.Page;
        w.Add(page.PageCode);
        if (page.PageCode == 0)
        {
            U16(w, page.Width);
            U16(w, page.Height);
        }
        else if (page.PageCode >= 7)
        {
            U16(w, page.Height);
        }

        w.Add(page.Orientation);
        w.Add(page.Quantum);

        w.Add((byte)model.Inks.Count);
        foreach (var ink in model.Inks)
        {
            w.Add(ink.R);
            w.Add(ink.G);
            w.Add(ink.B);
        }

        w.Add((byte)model.RingSets.Count);
        foreach (var set in model.RingSets)
        {
            w.Add((byte)set.Count);
            foreach (var disc in set)
            {
                U16(w, disc.Diameter);
                w.Add(disc.InkIndex);
            }
        }

        if (model.ExplicitBulls is { } bulls)
        {
            flags |= WireCodes.ExplicitLayoutFlag;
            U16(w, (ushort)bulls.Count);
            foreach (var bull in bulls)
            {
                if (bull.X > 0xFFF || bull.Y > 0xFFF || bull.RingSetIndex > 7)
                {
                    throw new ArgumentException("Explicit bulls pack 12-bit coordinates and a 3-bit ring set index.", nameof(model));
                }

                int packed = bull.X | (bull.Y << 12);
                w.Add((byte)packed);
                w.Add((byte)(packed >> 8));
                w.Add((byte)(packed >> 16));
            }

            // Four bits per bull, the first bull of each byte in the low nibble.
            for (int i = 0; i < bulls.Count; i += 2)
            {
                int low = Attributes(bulls[i]);
                int high = i + 1 < bulls.Count ? Attributes(bulls[i + 1]) : 0;
                w.Add((byte)(low | (high << 4)));
            }
        }
        else
        {
            var g = model.Grid ?? throw new ArgumentException("A parametric body needs a grid block.", nameof(model));
            w.Add(g.Cols);
            w.Add(g.Rows);
            U16(w, g.OriginX);
            U16(w, g.OriginY);
            U16(w, g.PitchX);
            U16(w, g.PitchY);
            w.Add(g.RingSetIndex);
            w.Add(g.Order);

            w.Add((byte)model.Sighters.Count);
            foreach (var row in model.Sighters)
            {
                w.Add(row.Count);
                U16(w, row.OriginX);
                U16(w, row.OriginY);
                U16(w, row.PitchX);
                w.Add(row.RingSetIndex);
            }
        }

        var f = model.Fiducials;
        w.Add(f.Scheme);
        w.Add(f.Family);
        U16(w, f.MarkerSize);
        w.Add(f.QuietZone);
        w.Add(f.InkIndex);

        var c = model.Codes;
        w.Add(c.Count);
        w.Add(c.EcLevel);
        w.Add(c.ModuleSize);
        w.Add(c.Placement);

        if (model.DataBlock is { } d)
        {
            flags |= WireCodes.DataBlockFlag;
            U16(w, d.X);
            U16(w, d.Y);
            U16(w, d.Width);
            U16(w, d.Height);
            w.Add(d.Layout);
            w.Add(d.FieldSet);
            U16(w, d.Reserve);
            w.Add(d.InkIndex);
        }

        if (model.Tiling is { } t)
        {
            flags |= WireCodes.TilingFlag;
            w.Add(t.Cols);
            w.Add(t.Rows);
            U16(w, t.SheetWidth);
            U16(w, t.SheetHeight);
            U16(w, t.Overlap);
            w.Add(0);
        }

        if (model.Grids is { } grids)
        {
            flags |= WireCodes.GridsFlag;
            w.Add((byte)grids.Count);
            foreach (var m in grids)
            {
                U16(w, m.CentreX);
                U16(w, m.CentreY);
                U16(w, m.Half);
                w.Add(m.Divisions);
                w.Add(m.MajorEvery);
                w.Add(m.Unit);
                U16(w, m.Distance);
                w.Add(m.DistanceUnit);
                w.Add(m.InkPair);
                w.Add(m.Style);
                w.Add(m.LabelStep);
            }
        }

        return ([.. w], flags);
    }

    public static BodyReadResult Read(ReadOnlySpan<byte> body, ushort flags)
    {
        var reader = new Reader(body);
        try
        {
            var model = ReadModel(ref reader, flags);
            return reader.Remaining == 0
                ? new BodyReadResult(model, null)
                : new BodyReadResult(null, $"{reader.Remaining} bytes follow the last block of the body.");
        }
        catch (MalformedBodyException ex)
        {
            return new BodyReadResult(null, ex.Message);
        }
    }

    private static BodyModel ReadModel(ref Reader r, ushort flags)
    {
        byte pageCode = r.U8("the page code");
        if (WireCodes.PageSizeOf(pageCode) is null)
        {
            throw new MalformedBodyException($"Unknown page code {pageCode}.");
        }

        ushort width = 0, height = 0;
        if (pageCode == 0)
        {
            width = r.U16("the page width");
            height = r.U16("the page height");
        }
        else if (pageCode >= 7)
        {
            height = r.U16("the roll page height");
        }

        byte orientation = r.U8("the orientation");
        byte quantum = r.U8("the quantum");
        Require(orientation <= 1, $"Unknown orientation {orientation}.");
        Require(quantum <= 3, $"Unknown quantum {quantum}.");

        byte inkCount = r.U8("the ink count");
        Require(inkCount <= 15, $"{inkCount} inks exceeds the 15 an ink index can address alongside paper.");
        var inks = new Rgb[inkCount];
        for (int i = 0; i < inkCount; i++)
        {
            inks[i] = new Rgb(r.U8("an ink color"), r.U8("an ink color"), r.U8("an ink color"));
        }

        Require(inks.Distinct().Count() == inks.Length,
            "The ink table repeats a color; a canonical body stores each distinct color once (TARGET-SCHEMA.md section 6).");

        byte setCount = r.U8("the ring set count");
        Require(setCount is >= 1 and <= 15, $"{setCount} ring sets is outside 1 to 15.");
        var sets = new IReadOnlyList<BodyDisc>[setCount];
        for (int s = 0; s < setCount; s++)
        {
            byte discCount = r.U8("a disc count");
            Require(discCount is >= 1 and <= 15, $"Ring set {s} has {discCount} discs, outside 1 to 15.");
            var discs = new BodyDisc[discCount];
            for (int i = 0; i < discCount; i++)
            {
                ushort diameter = r.U16("a disc diameter");
                byte ink = r.U8("a disc ink");
                Require((ink & 0xF0) == 0, $"Ring set {s} disc {i} sets the reserved ink bits 4 to 7.");
                RequireInk(ink, inkCount, allowPaper: true, $"ring set {s} disc {i}");
                discs[i] = new BodyDisc(diameter, ink);
            }

            sets[s] = discs;
        }

        BodyGrid? grid = null;
        var sighters = new List<BodySighterRow>();
        BodyBull[]? bulls = null;
        if ((flags & WireCodes.ExplicitLayoutFlag) != 0)
        {
            ushort count = r.U16("the bull count");
            Require(count >= 1, "The bull block declares no bulls.");
            int needed = (count * 3) + ((count + 1) / 2);
            Require(needed <= r.Remaining,
                $"The bull block declares {count} bulls, needing {needed} bytes, but only {r.Remaining} remain.");

            var coordinates = new int[count];
            for (int i = 0; i < count; i++)
            {
                coordinates[i] = r.U8("a bull") | (r.U8("a bull") << 8) | (r.U8("a bull") << 16);
            }

            bulls = new BodyBull[count];
            for (int i = 0; i < count; i += 2)
            {
                byte attributes = r.U8("the bull attributes");
                bulls[i] = ExplicitBull(coordinates[i], attributes & 0xF, setCount, i);
                if (i + 1 < count)
                {
                    bulls[i + 1] = ExplicitBull(coordinates[i + 1], attributes >> 4, setCount, i + 1);
                }
                else
                {
                    Require(attributes >> 4 == 0, "The padding nibble after the last bull is not zero.");
                }
            }
        }
        else
        {
            grid = new BodyGrid(
                r.U8("the grid columns"), r.U8("the grid rows"),
                r.U16("the grid origin"), r.U16("the grid origin"),
                r.U16("the grid pitch"), r.U16("the grid pitch"),
                r.U8("the grid ring set"), r.U8("the grid order"));
            Require(grid.Cols >= 1 && grid.Rows >= 1, "The grid has no columns or no rows.");
            Require(grid.RingSetIndex < setCount, $"The grid names ring set {grid.RingSetIndex} of {setCount}.");
            Require(grid.Order <= 2, $"Unknown grid order {grid.Order}.");

            byte rows = r.U8("the sighter row count");
            Require(rows <= 4, $"{rows} sighter rows exceeds 4.");
            for (int i = 0; i < rows; i++)
            {
                var row = new BodySighterRow(
                    r.U8("a sighter count"), r.U16("a sighter origin"), r.U16("a sighter origin"),
                    r.U16("a sighter pitch"), r.U8("a sighter ring set"));
                Require(row.Count >= 1, $"Sighter row {i} is empty.");
                Require(row.RingSetIndex < setCount, $"Sighter row {i} names ring set {row.RingSetIndex} of {setCount}.");
                sighters.Add(row);
            }
        }

        var fiducials = new BodyFiducials(
            r.U8("the fiducial scheme"), r.U8("the fiducial family"), r.U16("the marker size"),
            r.U8("the marker quiet zone"), r.U8("the fiducial ink"));
        Require(fiducials.Scheme < WireCodes.Schemes.Length, $"Unknown fiducial scheme {fiducials.Scheme}.");
        Require(fiducials.Scheme != 0,
            "Explicit fiducial placement has no byte layout: the body cannot carry the marker list.");
        Require(fiducials.Family <= 8, $"Unknown marker family {fiducials.Family}.");
        RequireInk(fiducials.InkIndex, inkCount, allowPaper: false, "the fiducials");

        var codes = new BodyCodes(r.U8("the code count"), r.U8("the error correction level"),
            r.U8("the module size"), r.U8("the code placement"));
        Require(codes.EcLevel <= 3, $"Unknown error correction level {codes.EcLevel}.");
        Require(codes.Placement != 1,
            "Explicit code placement has no byte layout (TARGET-SCHEMA.md section 11, question 13).");
        Require(codes.Placement == 0, $"Unknown code placement {codes.Placement}.");
        Require(Corners1.Supports(codes.Count), $"corners-1 places 0, 2 or 4 codes, not {codes.Count}.");

        BodyDataBlock? dataBlock = null;
        if ((flags & WireCodes.DataBlockFlag) != 0)
        {
            dataBlock = new BodyDataBlock(
                r.U16("the data block"), r.U16("the data block"), r.U16("the data block"), r.U16("the data block"),
                r.U8("the data block layout"), r.U8("the data block field set"),
                r.U16("the data block reserve"), r.U8("the data block ink"));
            Require(dataBlock.Layout != 255 && dataBlock.FieldSet != 255,
                "Explicit data block layouts and field sets have no complete byte layout.");
            Require(dataBlock.Layout <= 1, $"Unknown data block layout {dataBlock.Layout}.");
            Require(dataBlock.FieldSet <= 1, $"Unknown data block field set {dataBlock.FieldSet}.");
            RequireInk(dataBlock.InkIndex, inkCount, allowPaper: false, "the data block");
        }

        BodyTiling? tiling = null;
        if ((flags & WireCodes.TilingFlag) != 0)
        {
            tiling = new BodyTiling(
                r.U8("the tiling columns"), r.U8("the tiling rows"),
                r.U16("the sheet width"), r.U16("the sheet height"), r.U16("the tile overlap"));
            Require(r.U8("the tiling flags") == 0, "The reserved tiling flags byte is not zero.");
        }

        List<BodyMeasurementGrid>? grids = null;
        if ((flags & WireCodes.GridsFlag) != 0)
        {
            byte gridCount = r.U8("the measurement grid count");
            Require(gridCount <= 4, $"{gridCount} measurement grids exceeds 4.");
            grids = new List<BodyMeasurementGrid>(gridCount);
            for (int i = 0; i < gridCount; i++)
            {
                var m = new BodyMeasurementGrid(
                    r.U16("a grid center"), r.U16("a grid center"), r.U16("a grid half-extent"),
                    r.U8("the grid divisions"), r.U8("the grid major step"), r.U8("the grid unit"),
                    r.U16("the grid distance"), r.U8("the grid distance unit"), r.U8("the grid inks"),
                    r.U8("the grid style"), r.U8("the grid label step"));
                Require(WireCodes.GridUnitOf(m.Unit) is not null, $"Measurement grid {i} has unknown unit {m.Unit}.");
                Require(m.DistanceUnit <= 1, $"Measurement grid {i} has unknown distance unit {m.DistanceUnit}.");
                RequireInk((byte)(m.InkPair & 0xF), inkCount, allowPaper: true, $"measurement grid {i} minor lines");
                RequireInk((byte)(m.InkPair >> 4), inkCount, allowPaper: true, $"measurement grid {i} major lines");
                Require(m.Style == WireCodes.StandardGridStyle,
                    $"Measurement grid {i} has style {m.Style}; only style 1 is defined (TARGET-SCHEMA.md section 11, question 11).");
                grids.Add(m);
            }
        }

        return new BodyModel(new BodyPage(pageCode, width, height, orientation, quantum), inks, sets,
            grid, sighters, bulls, fiducials, codes, dataBlock, tiling, grids);
    }

    private static BodyBull ExplicitBull(int packed, int attributes, int setCount, int index)
    {
        int ringSet = attributes & 0x7;
        Require(ringSet < setCount, $"Bull {index} names ring set {ringSet} of {setCount}.");
        return new BodyBull((ushort)(packed & 0xFFF), (ushort)(packed >> 12), (byte)ringSet, (attributes & 0x8) != 0);
    }

    private static int Attributes(BodyBull bull) => bull.RingSetIndex | (bull.Scoring ? 0x8 : 0);

    private static void RequireInk(byte index, int inkCount, bool allowPaper, string owner)
    {
        if (index == WireCodes.PaperInk)
        {
            Require(allowPaper, $"The paper knockout cannot be the ink of {owner}.");
        }
        else
        {
            Require(index < inkCount, $"The ink of {owner} is index {index}, but the table holds {inkCount}.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new MalformedBodyException(message);
        }
    }

    private static void U16(List<byte> w, ushort value)
    {
        w.Add((byte)value);
        w.Add((byte)(value >> 8));
    }

    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _position;

        public Reader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _position = 0;
        }

        public readonly int Remaining => _data.Length - _position;

        public byte U8(string field)
        {
            if (Remaining < 1)
            {
                throw Truncated(field);
            }

            return _data[_position++];
        }

        public ushort U16(string field)
        {
            if (Remaining < 2)
            {
                throw Truncated(field);
            }

            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_data[_position..]);
            _position += 2;
            return value;
        }

        private readonly MalformedBodyException Truncated(string field) =>
            new($"The body ends at byte {_position} before {field}.");
    }

    private sealed class MalformedBodyException(string message) : Exception(message);
}
