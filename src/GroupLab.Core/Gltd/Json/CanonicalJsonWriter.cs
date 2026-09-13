using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Json;

/// <summary>
/// Writes the canonical GLTD-J form of TARGET-SCHEMA.md section 6: two-space indentation, LF,
/// UTF-8 without BOM, no trailing whitespace, one final newline. Inner objects follow the property
/// order of the section 9 schema; top-level blocks that section 3.1 does not place follow
/// docs/SPEC-ERRATA.md C6, and unknown top-level members come last in the order they were read.
/// </summary>
public static class CanonicalJsonWriter
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        IndentCharacter = ' ',
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static byte[] Write(TargetDefinition d)
    {
        ArgumentNullException.ThrowIfNull(d);
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer, Options))
        {
            w.WriteStartObject();
            w.WriteNumber("gltd", d.Gltd);
            w.WriteNumber("revision", d.Revision);
            Opt(w, "id", d.Id);
            w.WriteString("name", d.Name);
            Opt(w, "description", d.Description);
            Opt(w, "author", d.Author);
            Opt(w, "licence", d.Licence);
            Opt(w, "created", d.Created);
            w.WriteString("units", d.Units);
            WritePage(w, d.Page);
            WriteArray(w, "inks", d.Inks, WriteInk);
            WriteArray(w, "ringSets", d.RingSets, WriteRingSet);
            WriteArray(w, "bulls", d.Bulls, WriteBull);
            if (d.Cells is not null)
            {
                WriteCells(w, d.Cells);
            }

            if (d.Fiducials is not null)
            {
                WriteFiducials(w, d.Fiducials);
            }

            if (d.Codes is not null)
            {
                WriteCodes(w, d.Codes);
            }

            if (d.Print is not null)
            {
                WritePrint(w, d.Print);
            }

            if (d.DataBlock is not null)
            {
                WriteDataBlock(w, d.DataBlock);
            }

            if (d.Instance is not null)
            {
                WriteInstance(w, d.Instance);
            }

            if (d.Tiling is not null)
            {
                WriteTiling(w, d.Tiling);
            }

            if (d.Grids is not null)
            {
                WriteArray(w, "grids", d.Grids, WriteGrid);
            }

            foreach (var (name, value) in d.UnknownFields)
            {
                w.WritePropertyName(name);
                value.WriteTo(w);
            }

            w.WriteEndObject();
        }

        buffer.Write("\n"u8);
        return buffer.WrittenSpan.ToArray();
    }

    private static void WritePage(Utf8JsonWriter w, Page p)
    {
        w.WriteStartObject("page");
        w.WriteString("size", GltdNames.PageSize.NameOf(p.Size));
        w.WriteNumber("width", p.Width);
        w.WriteNumber("height", p.Height);
        if (p.Orientation is { } orientation)
        {
            w.WriteString("orientation", GltdNames.Orientation.NameOf(orientation));
        }

        w.WriteEndObject();
    }

    private static void WriteInk(Utf8JsonWriter w, Ink ink)
    {
        w.WriteStartObject();
        w.WriteString("key", ink.Key);
        w.WriteString("srgb", ink.Srgb);
        w.WriteString("role", GltdNames.InkRole.NameOf(ink.Role));
        w.WriteEndObject();
    }

    private static void WriteRingSet(Utf8JsonWriter w, RingSet set)
    {
        w.WriteStartObject();
        w.WriteString("key", set.Key);
        WriteArray(w, "discs", set.Discs, (dw, disc) =>
        {
            dw.WriteStartObject();
            dw.WriteNumber("diameter", disc.Diameter);
            dw.WriteString("ink", disc.Ink);
            dw.WriteEndObject();
        });
        w.WriteEndObject();
    }

    private static void WriteBull(Utf8JsonWriter w, Bull bull)
    {
        w.WriteStartObject();
        w.WriteNumber("x", bull.X);
        w.WriteNumber("y", bull.Y);
        w.WriteString("ringSet", bull.RingSet);
        Opt(w, "label", bull.Label);
        w.WriteBoolean("scoring", bull.Scoring);
        if (bull.LabelOffset is { } offset)
        {
            w.WriteStartObject("labelOffset");
            w.WriteNumber("x", offset.X);
            w.WriteNumber("y", offset.Y);
            w.WriteEndObject();
        }

        w.WriteEndObject();
    }

    private static void WritePoint(Utf8JsonWriter w, PointDmm p)
    {
        w.WriteStartObject();
        w.WriteNumber("x", p.X);
        w.WriteNumber("y", p.Y);
        w.WriteEndObject();
    }

    private static void WriteCells(Utf8JsonWriter w, Cells c)
    {
        w.WriteStartObject("cells");
        w.WriteString("mode", GltdNames.CellsMode.NameOf(c.Mode));
        Opt(w, "drawn", c.Drawn);
        Opt(w, "sighterGap", c.SighterGap);
        Opt(w, "ink", c.Ink);
        Opt(w, "stroke", c.Stroke);
        if (c.Grid is { } g)
        {
            w.WriteStartObject("grid");
            w.WriteNumber("originX", g.OriginX);
            w.WriteNumber("originY", g.OriginY);
            w.WriteNumber("pitchX", g.PitchX);
            w.WriteNumber("pitchY", g.PitchY);
            w.WriteNumber("cols", g.Cols);
            w.WriteNumber("rows", g.Rows);
            w.WriteEndObject();
        }

        if (c.Polygons is not null)
        {
            WriteArray(w, "polygons", c.Polygons, (pw, polygon) =>
            {
                pw.WriteStartArray();
                foreach (var point in polygon)
                {
                    WritePoint(pw, point);
                }

                pw.WriteEndArray();
            });
        }

        w.WriteEndObject();
    }

    private static void WriteFiducials(Utf8JsonWriter w, Fiducials f)
    {
        w.WriteStartObject("fiducials");
        w.WriteString("scheme", f.Scheme);
        w.WriteString("family", GltdNames.FiducialFamily.NameOf(f.Family));
        w.WriteNumber("markerSize", f.MarkerSize);
        w.WriteNumber("quietZone", f.QuietZone);
        w.WriteString("ink", f.Ink);
        if (f.Markers is not null)
        {
            WriteArray(w, "markers", f.Markers, (mw, m) =>
            {
                mw.WriteStartObject();
                mw.WriteNumber("id", m.Id);
                mw.WriteNumber("x", m.X);
                mw.WriteNumber("y", m.Y);
                mw.WriteEndObject();
            });
        }

        w.WriteEndObject();
    }

    private static void WriteCodes(Utf8JsonWriter w, Codes c)
    {
        w.WriteStartObject("codes");
        w.WriteNumber("count", c.Count);
        Opt(w, "version", c.Version);
        w.WriteString("ecLevel", GltdNames.EcLevel.NameOf(c.EcLevel));
        w.WriteNumber("moduleSize", c.ModuleSize);
        Opt(w, "quietZone", c.QuietZone);
        w.WriteString("placement", GltdNames.CodePlacement.NameOf(c.Placement));
        Opt(w, "humanReadableId", c.HumanReadableId);
        WriteArray(w, "positions", c.Positions, WritePoint);
        w.WriteEndObject();
    }

    private static void WritePrint(Utf8JsonWriter w, PrintSettings p)
    {
        w.WriteStartObject("print");
        if (p.Scaling is { } scaling)
        {
            w.WriteString("scaling", GltdNames.PrintScaling.NameOf(scaling));
        }

        Opt(w, "minimumDpi", p.MinimumDpi);
        if (p.ColourMode is { } colourMode)
        {
            w.WriteString("colourMode", GltdNames.ColourMode.NameOf(colourMode));
        }

        Opt(w, "duplex", p.Duplex);
        Opt(w, "generator", p.Generator);
        Opt(w, "notes", p.Notes);
        w.WriteEndObject();
    }

    private static void WriteDataBlock(Utf8JsonWriter w, DataBlock b)
    {
        w.WriteStartObject("dataBlock");
        w.WriteNumber("x", b.X);
        w.WriteNumber("y", b.Y);
        w.WriteNumber("width", b.Width);
        w.WriteNumber("height", b.Height);
        w.WriteString("layout", GltdNames.DataBlockLayout.NameOf(b.Layout));
        w.WriteString("fieldSet", GltdNames.FieldSet.NameOf(b.FieldSet));
        w.WriteNumber("reserve", b.Reserve);
        w.WriteString("ink", b.Ink);
        Opt(w, "labelInk", b.LabelInk);
        Opt(w, "border", b.Border);
        if (b.Fields is not null)
        {
            WriteArray(w, "fields", b.Fields, (fw, f) =>
            {
                fw.WriteStartObject();
                fw.WriteString("key", f.Key);
                fw.WriteString("label", f.Label);
                Opt(fw, "x", f.X);
                Opt(fw, "y", f.Y);
                Opt(fw, "width", f.Width);
                Opt(fw, "height", f.Height);
                fw.WriteEndObject();
            });
        }

        w.WriteEndObject();
    }

    private static void WriteInstance(Utf8JsonWriter w, Instance i)
    {
        w.WriteStartObject("instance");
        Opt(w, "serial", i.Serial);
        Opt(w, "printed", i.Printed);
        if (i.Values is not null)
        {
            w.WriteStartObject("values");
            foreach (var (key, value) in i.Values)
            {
                w.WriteString(key, value);
            }

            w.WriteEndObject();
        }

        w.WriteEndObject();
    }

    private static void WriteTiling(Utf8JsonWriter w, Tiling t)
    {
        w.WriteStartObject("tiling");
        w.WriteNumber("cols", t.Cols);
        w.WriteNumber("rows", t.Rows);
        w.WriteNumber("sheetWidth", t.SheetWidth);
        w.WriteNumber("sheetHeight", t.SheetHeight);
        w.WriteNumber("overlap", t.Overlap);
        w.WriteEndObject();
    }

    private static void WriteGrid(Utf8JsonWriter w, MeasurementGrid g)
    {
        w.WriteStartObject();
        w.WriteString("key", g.Key);
        w.WriteNumber("centreX", g.CentreX);
        w.WriteNumber("centreY", g.CentreY);
        w.WriteNumber("half", g.Half);
        w.WriteNumber("divisions", g.Divisions);
        w.WriteNumber("majorEvery", g.MajorEvery);
        w.WriteString("unit", GltdNames.GridUnit.NameOf(g.Unit));
        w.WriteNumber("distance", g.Distance);
        w.WriteString("distanceUnit", GltdNames.DistanceUnit.NameOf(g.DistanceUnit));
        Opt(w, "minorInk", g.MinorInk);
        Opt(w, "majorInk", g.MajorInk);
        Opt(w, "axisInk", g.AxisInk);
        Opt(w, "minorStroke", g.MinorStroke);
        Opt(w, "majorStroke", g.MajorStroke);
        Opt(w, "axisStroke", g.AxisStroke);
        Opt(w, "labelStep", g.LabelStep);
        Opt(w, "labelInk", g.LabelInk);
        w.WriteEndObject();
    }

    private static void WriteArray<T>(Utf8JsonWriter w, string name, IReadOnlyList<T> items, Action<Utf8JsonWriter, T> writeItem)
    {
        w.WriteStartArray(name);
        foreach (var item in items)
        {
            writeItem(w, item);
        }

        w.WriteEndArray();
    }

    private static void Opt(Utf8JsonWriter w, string name, string? value)
    {
        if (value is not null)
        {
            w.WriteString(name, value);
        }
    }

    private static void Opt(Utf8JsonWriter w, string name, int? value)
    {
        if (value is { } v)
        {
            w.WriteNumber(name, v);
        }
    }

    private static void Opt(Utf8JsonWriter w, string name, bool? value)
    {
        if (value is { } v)
        {
            w.WriteBoolean(name, v);
        }
    }
}
