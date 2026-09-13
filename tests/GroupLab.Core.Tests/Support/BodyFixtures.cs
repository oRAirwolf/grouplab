using System.Text.Json;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Tests.Support;

/// <summary>The definitions tools/gltd/check.py encodes, dumped to Fixtures/gltd-check.json.</summary>
internal static class ReferenceFixtures
{
    private static readonly Lazy<JsonDocument> Document = new(() =>
        JsonDocument.Parse(File.ReadAllBytes(Repo.PathTo("tests", "GroupLab.Core.Tests", "Fixtures", "gltd-check.json"))));

    public static IEnumerable<JsonElement> Definitions => Document.Value.RootElement.GetProperty("definitions").EnumerateArray();

    public static JsonElement Named(string name) => Definitions.Single(d => d.GetProperty("name").GetString() == name);

    /// <summary>Builds the body model from check.py's own input shape, independently of the projection.</summary>
    public static BodyModel ToBody(JsonElement d)
    {
        var page = d.GetProperty("page");
        Assert.True(GltdNames.PageSize.TryParse(page.GetProperty("size").GetString()!, out var size));
        byte pageCode = WireCodes.PageCode(size);
        byte orientation = page.TryGetProperty("orientation", out var o) && o.GetString() == "landscape" ? (byte)1 : (byte)0;
        var bodyPage = new BodyPage(pageCode,
            pageCode == 0 ? U16(page, "width") : (ushort)0,
            pageCode == 0 || pageCode >= 7 ? U16(page, "height") : (ushort)0,
            orientation,
            d.TryGetProperty("quantum", out var q) ? q.GetByte() : (byte)0);

        var inks = new List<Rgb>();
        foreach (var ink in d.GetProperty("inks").EnumerateArray().Where(i => i.GetProperty("role").GetString() != "paper"))
        {
            string srgb = ink.GetProperty("srgb").GetString()!.ToUpperInvariant();
            var rgb = new Rgb(Convert.ToByte(srgb[1..3], 16), Convert.ToByte(srgb[3..5], 16), Convert.ToByte(srgb[5..7], 16));
            if (!inks.Contains(rgb))
            {
                inks.Add(rgb);
            }
        }

        var ringSets = d.GetProperty("ringSets").EnumerateArray()
            .Select(s => (IReadOnlyList<BodyDisc>)s.GetProperty("discs").EnumerateArray()
                .Select(disc => new BodyDisc(U16(disc, "diameter"), U8(disc, "inkIdx"))).ToList())
            .ToList();

        var g = d.GetProperty("grid");
        var grid = new BodyGrid(U8(g, "cols"), U8(g, "rows"), U16(g, "originX"), U16(g, "originY"),
            U16(g, "pitchX"), U16(g, "pitchY"), U8(g, "ringSetIdx"), U8(g, "order"));
        var sighters = d.GetProperty("sighters").EnumerateArray()
            .Select(s => new BodySighterRow(U8(s, "count"), U16(s, "originX"), U16(s, "originY"), U16(s, "pitchX"), U8(s, "ringSetIdx")))
            .ToList();

        var f = d.GetProperty("fiducials");
        Assert.True(GltdNames.FiducialFamily.TryParse(f.GetProperty("family").GetString()!, out var family));
        var fiducials = new BodyFiducials((byte)Array.IndexOf(WireCodes.Schemes, f.GetProperty("scheme").GetString()),
            WireCodes.FamilyCode(family), U16(f, "markerSize"), U8(f, "quietZone"), U8(f, "inkIdx"));

        var c = d.GetProperty("codes");
        Assert.True(GltdNames.EcLevel.TryParse(c.GetProperty("ecLevel").GetString()!, out var ecLevel));
        Assert.Equal("corners-1", c.GetProperty("placement").GetString());
        var codes = new BodyCodes(U8(c, "count"), (byte)ecLevel, U8(c, "moduleSize"), 0);

        BodyDataBlock? dataBlock = null;
        if (d.TryGetProperty("dataBlock", out var b))
        {
            Assert.True(GltdNames.DataBlockLayout.TryParse(b.GetProperty("layout").GetString()!, out var layout));
            Assert.True(GltdNames.FieldSet.TryParse(b.GetProperty("fieldSet").GetString()!, out var fieldSet));
            dataBlock = new BodyDataBlock(U16(b, "x"), U16(b, "y"), U16(b, "width"), U16(b, "height"),
                (byte)layout, (byte)fieldSet, U16(b, "reserve"), U8(b, "inkIdx"));
        }

        BodyTiling? tiling = d.TryGetProperty("tiling", out var t)
            ? new BodyTiling(U8(t, "cols"), U8(t, "rows"), U16(t, "sheetWidth"), U16(t, "sheetHeight"), U16(t, "overlap"))
            : null;

        List<BodyMeasurementGrid>? grids = null;
        if (d.TryGetProperty("grids", out var gs))
        {
            grids = [];
            foreach (var m in gs.EnumerateArray())
            {
                Assert.True(GltdNames.GridUnit.TryParse(m.GetProperty("unit").GetString()!, out var unit));
                grids.Add(new BodyMeasurementGrid(U16(m, "centreX"), U16(m, "centreY"), U16(m, "half"), U8(m, "divisions"),
                    U8(m, "majorEvery"), WireCodes.GridUnitCode(unit), U16(m, "distance"),
                    m.GetProperty("distanceUnit").GetString() == "m" ? (byte)1 : (byte)0,
                    U8(m, "inkPair"), U8(m, "style"), U8(m, "labelStep")));
            }
        }

        return new BodyModel(bodyPage, inks, ringSets, grid, sighters, null, fiducials, codes, dataBlock, tiling, grids);
    }

    private static byte U8(JsonElement e, string name) => e.GetProperty(name).GetByte();

    private static ushort U16(JsonElement e, string name) => e.GetProperty(name).GetUInt16();
}

/// <summary>
/// Random canonical bodies for conformance test 2. Every choice below keeps the body in the one form the
/// encoder itself would produce, and keeps its decode a valid GLTD-J document.
/// </summary>
internal static class RandomBodies
{
    public static BodyModel Next(Random rng)
    {
        bool isExplicit = rng.Next(4) == 0;
        byte quantum = (byte)(isExplicit && rng.Next(2) == 0 ? 1 : 0);
        int q = WireCodes.QuantumDmm[quantum];

        byte pageCode = (byte)rng.Next(10);
        ushort width = 0, height = 0;
        if (pageCode == 0)
        {
            width = Raw(rng, 1500, 6000, q);
            height = Raw(rng, 3000, 9000, q);
        }
        else if (pageCode >= 7)
        {
            height = Raw(rng, 3000, 9000, q);
        }

        int inkCount = rng.Next(1, 7);
        var inks = new List<Rgb>();
        while (inks.Count < inkCount)
        {
            var colour = new Rgb((byte)rng.Next(256), (byte)rng.Next(256), (byte)rng.Next(256));
            if (!inks.Contains(colour))
            {
                inks.Add(colour);
            }
        }

        byte Ink(bool allowPaper) => allowPaper && rng.Next(4) == 0 ? WireCodes.PaperInk : (byte)rng.Next(inkCount);

        int setCount = rng.Next(1, isExplicit ? 9 : 16);
        var sets = Enumerable.Range(0, setCount)
            .Select(_ => (IReadOnlyList<BodyDisc>)Enumerable.Range(0, rng.Next(1, 6))
                .Select(_ => new BodyDisc(Raw(rng, 1, 2000, q), Ink(allowPaper: true))).ToList())
            .ToList();

        BodyGrid? grid = null;
        var sighters = new List<BodySighterRow>();
        List<BodyBull>? bulls = null;
        if (!isExplicit)
        {
            // A pitch along an axis with one bull is unobservable, so the canonical body stores the other axis's
            // pitch there, and zero for a single bull (TARGET-SCHEMA.md section 6).
            byte cols = (byte)rng.Next(1, 9), rows = (byte)rng.Next(1, 9);
            ushort pitchX = (ushort)(2 * rng.Next(1, 1001));
            ushort pitchY = (ushort)(2 * rng.Next(1, 1001));
            if (cols == 1 && rows == 1)
            {
                pitchX = pitchY = 0;
            }
            else if (cols == 1)
            {
                pitchX = pitchY;
            }
            else if (rows == 1)
            {
                pitchY = pitchX;
            }
            byte order = (byte)(cols > 1 && rows > 1 ? rng.Next(3) : 0);
            grid = new BodyGrid(cols, rows, (ushort)((pitchX / 2) + rng.Next(3000)), (ushort)((pitchY / 2) + rng.Next(3000)),
                pitchX, pitchY, (byte)rng.Next(setCount), order);

            int lastY = -1;
            for (int i = rng.Next(4); i > 0; i--)
            {
                byte count = (byte)rng.Next(1, 7);
                int y;
                do
                {
                    y = rng.Next(30000);
                }
                while (y == lastY);

                lastY = y;
                sighters.Add(new BodySighterRow(count, (ushort)rng.Next(30000), (ushort)y,
                    count == 1 ? pitchX : (ushort)rng.Next(1, 2001), (byte)rng.Next(setCount)));
            }
        }
        else
        {
            bulls = [.. Enumerable.Range(0, rng.Next(5, 21)).Select(_ =>
                new BodyBull((ushort)rng.Next(4096), (ushort)rng.Next(4096), (byte)rng.Next(setCount), rng.Next(3) != 0))];
            if (quantum == 1)
            {
                // At least one coordinate past 4095 dmm, so 0.2 mm is the smallest quantum that fits.
                bulls[0] = bulls[0] with { X = (ushort)rng.Next(2048, 4096) };
            }
        }

        var fiducials = new BodyFiducials((byte)rng.Next(1, 4), (byte)rng.Next(9), Raw(rng, 20, 400, q),
            (byte)rng.Next(quantum == 0 ? 256 : 128), Ink(allowPaper: false));
        var codes = new BodyCodes((byte)(2 * rng.Next(3)), (byte)rng.Next(4), (byte)Raw(rng, 4, 20, q), 0);

        BodyDataBlock? dataBlock = rng.Next(2) == 0
            ? new BodyDataBlock(Raw(rng, 0, 3000, q), Raw(rng, 0, 3000, q), Raw(rng, 200, 3000, q), Raw(rng, 100, 500, q),
                (byte)rng.Next(2), (byte)rng.Next(2), Raw(rng, 0, 500, q), Ink(allowPaper: false))
            : null;

        BodyTiling? tiling = rng.Next(2) == 0
            ? new BodyTiling((byte)rng.Next(1, 17), (byte)rng.Next(1, 17), Raw(rng, 500, 3000, q), Raw(rng, 500, 3000, q), Raw(rng, 0, 100, q))
            : null;

        List<BodyMeasurementGrid>? grids = rng.Next(2) == 0
            ? [.. Enumerable.Range(0, rng.Next(1, 5)).Select(_ => new BodyMeasurementGrid(
                Raw(rng, 0, 5000, q), Raw(rng, 0, 5000, q), Raw(rng, 100, 2000, q), (byte)rng.Next(1, 101), (byte)rng.Next(1, 101),
                (byte)rng.Next(5), (ushort)rng.Next(1, 5001), (byte)rng.Next(2),
                (byte)(Ink(allowPaper: true) | (Ink(allowPaper: true) << 4)), WireCodes.StandardGridStyle, (byte)rng.Next(101)))]
            : null;

        return new BodyModel(new BodyPage(pageCode, width, height, (byte)rng.Next(2), quantum), inks, sets, grid, sighters, bulls,
            fiducials, codes, dataBlock, tiling, grids);
    }

    /// <summary>A value in quanta whose length in dmm lies within the given range.</summary>
    private static ushort Raw(Random rng, int minDmm, int maxDmm, int q) => (ushort)rng.Next((minDmm + q - 1) / q, (maxDmm / q) + 1);
}
