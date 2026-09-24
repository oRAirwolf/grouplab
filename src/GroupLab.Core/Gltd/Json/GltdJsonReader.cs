using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Json;

/// <summary><see cref="Definition"/> is null whenever <see cref="Diagnostics"/> holds an error.</summary>
public sealed record GltdReadResult(TargetDefinition? Definition, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// Reads GLTD-J and enforces the JSON Schema of TARGET-SCHEMA.md section 9. The cross-field
/// rules listed after that schema belong to the validator, not here.
/// </summary>
public static partial class GltdJsonReader
{
    public const int SupportedMajor = 1;

    private const int DmmMax = 65535;

    private static readonly string[] TopLevelKeys =
    [
        "gltd", "revision", "id", "name", "description", "author", "licence", "created", "units",  // British on purpose: a key in a file GroupLab reads and writes
        "page", "inks", "ringSets", "bulls", "cells", "fiducials", "codes", "print",
        "dataBlock", "instance", "tiling", "grids",
    ];

    private static readonly string[] PageKeys = ["size", "width", "height", "orientation"];
    private static readonly string[] InkKeys = ["key", "srgb", "role"];
    private static readonly string[] RingSetKeys = ["key", "discs"];
    private static readonly string[] DiscKeys = ["diameter", "ink"];
    private static readonly string[] BullKeys = ["x", "y", "ringSet", "label", "scoring", "labelOffset"];
    private static readonly string[] PointKeys = ["x", "y"];
    private static readonly string[] CellsKeys = ["mode", "drawn", "sighterGap", "ink", "stroke", "grid", "polygons"];
    private static readonly string[] CellGridKeys = ["originX", "originY", "pitchX", "pitchY", "cols", "rows"];
    private static readonly string[] FiducialsKeys = ["scheme", "family", "markerSize", "quietZone", "ink", "markers"];
    private static readonly string[] MarkerKeys = ["id", "x", "y"];
    private static readonly string[] CodesKeys =
        ["count", "version", "ecLevel", "moduleSize", "quietZone", "placement", "humanReadableId", "positions"];
    private static readonly string[] PrintKeys = ["scaling", "minimumDpi", "colourMode", "duplex", "generator", "notes"];
    private static readonly string[] DataBlockKeys =
        ["x", "y", "width", "height", "layout", "fieldSet", "reserve", "ink", "labelInk", "border", "fields"];
    private static readonly string[] DataFieldKeys = ["key", "label", "x", "y", "width", "height"];
    private static readonly string[] InstanceKeys = ["serial", "printed", "values"];
    private static readonly string[] TilingKeys = ["cols", "rows", "sheetWidth", "sheetHeight", "overlap"];
    private static readonly string[] GridKeys =
    [
        "key", "centreX", "centreY", "half", "divisions", "majorEvery", "unit", "distance", "distanceUnit",
        "minorInk", "majorInk", "axisInk", "minorStroke", "majorStroke", "axisStroke", "labelStep", "labelInk",
    ];

    public static GltdReadResult ReadFile(string path) => Read(File.ReadAllBytes(path));

    public static GltdReadResult Read(ReadOnlySpan<byte> utf8)
    {
        // Canonical files carry no BOM (section 6), but a hand-edited file on Windows may.
        ReadOnlySpan<byte> bom = [0xEF, 0xBB, 0xBF];
        if (utf8.StartsWith(bom))
        {
            utf8 = utf8[bom.Length..];
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (JsonException ex)
        {
            return new GltdReadResult(null, [Diagnostic.Error("json.syntax", "", ex.Message)]);
        }

        using (document)
        {
            var parser = new Parser();
            var definition = parser.ReadDocument(document.RootElement);
            return new GltdReadResult(parser.HasErrors ? null : definition, parser.Diagnostics);
        }
    }

    // JSON Schema patterns are anchored at the true end of input; .NET's $ also matches before a
    // trailing newline, so \z stands in for it.
    [GeneratedRegex(@"^[a-zA-Z0-9_-]{1,16}\z")]
    private static partial Regex InkKeyPattern();

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}\z")]
    private static partial Regex SrgbPattern();

    [GeneratedRegex(@"^GL-[0-9A-HJKMNP-TV-Z]{4}(-[0-9A-HJKMNP-TV-Z]{4}){3}\z")]
    private static partial Regex IdPattern();

    [GeneratedRegex(@"^(explicit|[a-z0-9-]+-[0-9]+)\z")]
    private static partial Regex SchemePattern();

    [GeneratedRegex(@"^[0-9A-HJKMNP-TV-Z]{4}\z")]
    private static partial Regex SerialPattern();

    private static string Child(string path, string name) => $"{path}/{name.Replace("~", "~0").Replace("/", "~1")}";

    private static string Child(string path, int index) => $"{path}/{index}";

    private sealed class Parser
    {
        public List<Diagnostic> Diagnostics { get; } = [];

        public bool HasErrors => Diagnostics.Exists(d => d.Severity == Severity.Error);

        public void Error(string code, string path, string message) =>
            Diagnostics.Add(Diagnostic.Error(code, path, message));

        public TargetDefinition? ReadDocument(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                Error("schema.type", "", "A GLTD-J document is a JSON object.");
                return null;
            }

            var o = new Obj(this, root, "", TopLevelKeys, allowUnknown: true);

            // Rule R4: an unknown major is refused outright, before anything else is interpreted.
            if (o.TryGetRaw("gltd", out var gltdElement)
                && gltdElement.ValueKind == JsonValueKind.Number
                && gltdElement.TryGetInt64(out long major)
                && major != SupportedMajor)
            {
                Diagnostics.Clear();
                Error("gltd.unsupportedMajor", "/gltd",
                    $"GLTD major version {major} is not supported. This reader supports version {SupportedMajor} " +
                    "and refuses other majors rather than guessing (TARGET-SCHEMA.md rule R4).");
                return null;
            }

            int gltd = o.Int("gltd", true, SupportedMajor, SupportedMajor) ?? 0;
            int revision = o.Int("revision", true, 0, int.MaxValue) ?? 0;
            string? id = o.Str("id", false, pattern: IdPattern());
            string name = o.Str("name", true, minLength: 1, maxLength: 120) ?? "";
            string? description = o.Str("description", false, maxLength: 2000);
            string? author = o.Str("author", false, maxLength: 120);
            string? licence = o.Str("licence", false, maxLength: 64);  // British on purpose: a key in a file GroupLab reads and writes
            string? created = o.Date("created", false);
            string units = o.Str("units", true) ?? "";
            if (o.Has("units") && units.Length > 0 && units != "dmm")
            {
                Error("schema.const", "/units", $"\"{units}\" is not supported; version 1 requires \"dmm\".");
            }

            var page = o.Object("page", true, PageKeys, ReadPage) ?? new Page(default, 0, 0, null);
            var inks = o.Array("inks", true, 1, 16, (e, p) => ObjectItem(e, p, InkKeys, ReadInk)) ?? [];
            var ringSets = o.Array("ringSets", true, 1, 15, (e, p) => ObjectItem(e, p, RingSetKeys, ReadRingSet)) ?? [];
            var bulls = o.Array("bulls", true, 1, 4095, (e, p) => ObjectItem(e, p, BullKeys, ReadBull)) ?? [];
            var cells = o.Object("cells", false, CellsKeys, ReadCells);
            var fiducials = o.Object("fiducials", false, FiducialsKeys, ReadFiducials);
            var codes = o.Object("codes", false, CodesKeys, ReadCodes);
            var print = o.Object("print", false, PrintKeys, ReadPrint);
            var dataBlock = o.Object("dataBlock", false, DataBlockKeys, ReadDataBlock);
            var instance = o.Object("instance", false, InstanceKeys, ReadInstance);
            var tiling = o.Object("tiling", false, TilingKeys, ReadTiling);
            var grids = o.Array("grids", false, 0, 4, (e, p) => ObjectItem(e, p, GridKeys, ReadGrid));

            return new TargetDefinition(
                gltd, revision, id, name, description, author, licence, created, units,
                page, inks, ringSets, bulls, cells, fiducials, codes, print, dataBlock, instance, tiling, grids,
                o.Unknown);
        }

        public T? ObjectItem<T>(JsonElement e, string path, string[] keys, Func<Obj, T> read)
            where T : class
        {
            if (e.ValueKind != JsonValueKind.Object)
            {
                Error("schema.type", path, "Expected an object.");
                return null;
            }

            return read(new Obj(this, e, path, keys, allowUnknown: false));
        }

        public int? ReadInt(JsonElement e, string path, long min, long max)
        {
            if (e.ValueKind != JsonValueKind.Number)
            {
                Error("schema.type", path, "Expected an integer.");
                return null;
            }

            string raw = e.GetRawText();
            if (raw.AsSpan().IndexOfAny('.', 'e', 'E') >= 0)
            {
                Error("schema.type", path,
                    $"Expected an integer, got {raw}. GLTD geometry is integer dmm (TARGET-SCHEMA.md rule R1).");
                return null;
            }

            if (!e.TryGetInt64(out long value) || value < min || value > max)
            {
                Error("schema.range", path, $"{raw} is outside the range {min} to {max}.");
                return null;
            }

            return (int)value;
        }

        private static Page ReadPage(Obj o) => new(
            o.Choice("size", true, GltdNames.PageSize) ?? default,
            o.Int("width", true, 500, DmmMax) ?? 0,
            o.Int("height", true, 500, DmmMax) ?? 0,
            o.Choice("orientation", false, GltdNames.Orientation));

        private static Ink ReadInk(Obj o) => new(
            o.Str("key", true, pattern: InkKeyPattern()) ?? "",
            o.Str("srgb", true, pattern: SrgbPattern())?.ToUpperInvariant() ?? "",
            o.Choice("role", true, GltdNames.InkRole) ?? default);

        private RingSet ReadRingSet(Obj o) => new(
            o.Str("key", true, pattern: InkKeyPattern()) ?? "",
            o.Array("discs", true, 1, 15, (e, p) => ObjectItem(e, p, DiscKeys, ReadDisc)) ?? []);

        private static Disc ReadDisc(Obj o) => new(
            o.Int("diameter", true, 1, DmmMax) ?? 0,
            o.Str("ink", true, pattern: InkKeyPattern()) ?? "");

        private static Bull ReadBull(Obj o) => new(
            o.Int("x", true, 0, DmmMax) ?? 0,
            o.Int("y", true, 0, DmmMax) ?? 0,
            o.Str("ringSet", true, pattern: InkKeyPattern()) ?? "",
            o.Str("label", false, maxLength: 8),
            o.Bool("scoring", true) ?? false,
            o.Object("labelOffset", false, PointKeys, p => new Offset(
                p.Int("x", true, int.MinValue, int.MaxValue) ?? 0,
                p.Int("y", true, int.MinValue, int.MaxValue) ?? 0)));

        private static PointDmm ReadPoint(Obj o) => new(
            o.Int("x", true, 0, DmmMax) ?? 0,
            o.Int("y", true, 0, DmmMax) ?? 0);

        private Cells ReadCells(Obj o) => new(
            o.Choice("mode", true, GltdNames.CellsMode) ?? default,
            o.Bool("drawn", false),
            o.Int("sighterGap", false, 0, DmmMax),
            o.Str("ink", false, pattern: InkKeyPattern()),
            o.Int("stroke", false, 0, 255),
            o.Object("grid", false, CellGridKeys, g => new CellGrid(
                g.Int("originX", true, 0, DmmMax) ?? 0,
                g.Int("originY", true, 0, DmmMax) ?? 0,
                g.Int("pitchX", true, 1, DmmMax) ?? 0,
                g.Int("pitchY", true, 1, DmmMax) ?? 0,
                g.Int("cols", true, 1, 255) ?? 0,
                g.Int("rows", true, 1, 255) ?? 0)),
            o.Array("polygons", false, 0, int.MaxValue, ReadPolygon));

        private IReadOnlyList<PointDmm>? ReadPolygon(JsonElement e, string path)
        {
            if (e.ValueKind != JsonValueKind.Array)
            {
                Error("schema.type", path, "Expected an array of points.");
                return null;
            }

            int count = e.GetArrayLength();
            if (count < 3)
            {
                Error("schema.items", path, $"A polygon needs at least 3 points, got {count}.");
            }

            var points = new List<PointDmm>(count);
            int i = 0;
            foreach (var item in e.EnumerateArray())
            {
                var point = ObjectItem(item, Child(path, i++), PointKeys, ReadPoint);
                if (point is not null)
                {
                    points.Add(point);
                }
            }

            return points;
        }

        private Fiducials ReadFiducials(Obj o) => new(
            o.Str("scheme", true, pattern: SchemePattern()) ?? "",
            o.Choice("family", true, GltdNames.FiducialFamily) ?? default,
            o.Int("markerSize", true, 20, DmmMax) ?? 0,
            o.Int("quietZone", true, 0, 255) ?? 0,
            o.Str("ink", true, pattern: InkKeyPattern()) ?? "",
            o.Array("markers", false, 0, 1024, (e, p) => ObjectItem(e, p, MarkerKeys, m => new Marker(
                m.Int("id", true, 0, 1023) ?? 0,
                m.Int("x", true, 0, DmmMax) ?? 0,
                m.Int("y", true, 0, DmmMax) ?? 0))));

        private Codes ReadCodes(Obj o) => new(
            o.Int("count", true, 0, 8) ?? 0,
            o.Int("version", false, 1, 40),
            o.Choice("ecLevel", true, GltdNames.EcLevel) ?? default,
            o.Int("moduleSize", true, 3, 255) ?? 0,
            o.Int("quietZone", false, 0, 255),
            o.Choice("placement", true, GltdNames.CodePlacement) ?? default,
            o.Bool("humanReadableId", false),
            o.Array("positions", true, 0, 8, (e, p) => ObjectItem(e, p, PointKeys, ReadPoint)) ?? []);

        private static PrintSettings ReadPrint(Obj o) => new(
            o.Choice("scaling", false, GltdNames.PrintScaling),
            o.Int("minimumDpi", false, 72, 4800),
            o.Choice("colourMode", false, GltdNames.ColourMode),
            o.Bool("duplex", false),
            o.Str("generator", false, maxLength: 64),
            o.Str("notes", false, maxLength: 1000));

        private DataBlock ReadDataBlock(Obj o) => new(
            o.Int("x", true, 0, DmmMax) ?? 0,
            o.Int("y", true, 0, DmmMax) ?? 0,
            o.Int("width", true, 200, DmmMax) ?? 0,
            o.Int("height", true, 100, DmmMax) ?? 0,
            o.Choice("layout", true, GltdNames.DataBlockLayout) ?? default,
            o.Choice("fieldSet", true, GltdNames.FieldSet) ?? default,
            o.Int("reserve", true, 0, DmmMax) ?? 0,
            o.Str("ink", true, pattern: InkKeyPattern()) ?? "",
            o.Str("labelInk", false, pattern: InkKeyPattern()),
            o.Int("border", false, 0, 255),
            o.Array("fields", false, 0, 32, (e, p) => ObjectItem(e, p, DataFieldKeys, f => new DataField(
                f.Str("key", true, pattern: InkKeyPattern()) ?? "",
                f.Str("label", true, maxLength: 32) ?? "",
                f.Int("x", false, 0, DmmMax),
                f.Int("y", false, 0, DmmMax),
                f.Int("width", false, 0, DmmMax),
                f.Int("height", false, 0, DmmMax)))));

        private Instance ReadInstance(Obj o) => new(
            o.Str("serial", false, pattern: SerialPattern()),
            o.Date("printed", false),
            o.TryGetRaw("values", out var values) ? ReadInstanceValues(values, o.PathOf("values")) : null);

        private List<KeyValuePair<string, string>>? ReadInstanceValues(JsonElement e, string path)
        {
            if (e.ValueKind != JsonValueKind.Object)
            {
                Error("schema.type", path, "Expected an object of strings.");
                return null;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<KeyValuePair<string, string>>();
            foreach (var member in e.EnumerateObject())
            {
                string memberPath = Child(path, member.Name);
                if (!seen.Add(member.Name))
                {
                    Error("json.duplicateKey", memberPath, $"Key \"{member.Name}\" appears more than once.");
                    continue;
                }

                if (member.Value.ValueKind != JsonValueKind.String)
                {
                    Error("schema.type", memberPath, "Expected a string.");
                    continue;
                }

                string value = member.Value.GetString()!;
                if (value.EnumerateRunes().Count() > 64)
                {
                    Error("schema.length", memberPath, "Instance values are at most 64 characters.");
                    continue;
                }

                result.Add(new(member.Name, value));
            }

            return result;
        }

        private static Tiling ReadTiling(Obj o) => new(
            o.Int("cols", true, 1, 16) ?? 0,
            o.Int("rows", true, 1, 16) ?? 0,
            o.Int("sheetWidth", true, 500, DmmMax) ?? 0,
            o.Int("sheetHeight", true, 500, DmmMax) ?? 0,
            o.Int("overlap", true, 0, DmmMax) ?? 0);

        private static MeasurementGrid ReadGrid(Obj o) => new(
            o.Str("key", true, pattern: InkKeyPattern()) ?? "",
            o.Int("centreX", true, 0, DmmMax) ?? 0,
            o.Int("centreY", true, 0, DmmMax) ?? 0,
            o.Int("half", true, 100, DmmMax) ?? 0,
            o.Int("divisions", true, 1, 100) ?? 0,
            o.Int("majorEvery", true, 1, 100) ?? 0,
            o.Choice("unit", true, GltdNames.GridUnit) ?? default,
            o.Int("distance", true, 1, 5000) ?? 0,
            o.Choice("distanceUnit", true, GltdNames.DistanceUnit) ?? default,
            o.Str("minorInk", false, pattern: InkKeyPattern()),
            o.Str("majorInk", false, pattern: InkKeyPattern()),
            o.Str("axisInk", false, pattern: InkKeyPattern()),
            o.Int("minorStroke", false, 1, 255),
            o.Int("majorStroke", false, 1, 255),
            o.Int("axisStroke", false, 1, 255),
            o.Int("labelStep", false, 0, 100),
            o.Str("labelInk", false, pattern: InkKeyPattern()));
    }

    /// <summary>One JSON object being read: tracks its members, duplicates and unknown keys.</summary>
    private sealed class Obj
    {
        private readonly Parser _parser;
        private readonly string _path;
        private readonly Dictionary<string, JsonElement> _members = new(StringComparer.Ordinal);

        public Obj(Parser parser, JsonElement element, string path, string[] knownKeys, bool allowUnknown)
        {
            _parser = parser;
            _path = path;
            foreach (var member in element.EnumerateObject())
            {
                string memberPath = Child(path, member.Name);
                if (!_members.TryAdd(member.Name, member.Value))
                {
                    parser.Error("json.duplicateKey", memberPath, $"Key \"{member.Name}\" appears more than once.");
                    continue;
                }

                if (System.Array.IndexOf(knownKeys, member.Name) >= 0)
                {
                    continue;
                }

                if (allowUnknown)
                {
                    Unknown.Add(new(member.Name, member.Value.Clone()));
                }
                else
                {
                    parser.Error("schema.additionalProperty", memberPath,
                        $"\"{member.Name}\" is not a property of this object.");
                }
            }
        }

        public List<KeyValuePair<string, JsonElement>> Unknown { get; } = [];

        public string PathOf(string name) => Child(_path, name);

        public bool Has(string name) => _members.ContainsKey(name);

        public bool TryGetRaw(string name, out JsonElement element) => _members.TryGetValue(name, out element);

        public int? Int(string name, bool required, long min, long max) =>
            Get(name, required, out var e) ? _parser.ReadInt(e, PathOf(name), min, max) : null;

        public bool? Bool(string name, bool required)
        {
            if (!Get(name, required, out var e))
            {
                return null;
            }

            if (e.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return e.GetBoolean();
            }

            _parser.Error("schema.type", PathOf(name), "Expected true or false.");
            return null;
        }

        public string? Str(string name, bool required, int minLength = 0, int maxLength = int.MaxValue, Regex? pattern = null)
        {
            if (!Get(name, required, out var e))
            {
                return null;
            }

            string path = PathOf(name);
            if (e.ValueKind != JsonValueKind.String)
            {
                _parser.Error("schema.type", path, "Expected a string.");
                return null;
            }

            string value = e.GetString()!;
            int length = value.EnumerateRunes().Count();
            if (length < minLength || length > maxLength)
            {
                _parser.Error("schema.length", path,
                    $"Length {length} is outside {minLength} to {(maxLength == int.MaxValue ? "unbounded" : maxLength)}.");
                return null;
            }

            if (pattern is not null && !pattern.IsMatch(value))
            {
                _parser.Error("schema.pattern", path, $"\"{value}\" does not match {pattern}.");
                return null;
            }

            return value;
        }

        public string? Date(string name, bool required)
        {
            string? value = Str(name, required);
            if (value is not null
                && !DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                _parser.Error("schema.format", PathOf(name), $"\"{value}\" is not an ISO 8601 date.");
                return null;
            }

            return value;
        }

        public T? Choice<T>(string name, bool required, NameTable<T> table)
            where T : struct, System.Enum
        {
            if (!Get(name, required, out var e))
            {
                return null;
            }

            if (e.ValueKind == JsonValueKind.String && table.TryParse(e.GetString()!, out var value))
            {
                return value;
            }

            _parser.Error("schema.enum", PathOf(name), $"{e.GetRawText()} is not one of {string.Join(", ", table.Names)}.");
            return null;
        }

        public T? Object<T>(string name, bool required, string[] keys, Func<Obj, T> read)
            where T : class =>
            Get(name, required, out var e) ? _parser.ObjectItem(e, PathOf(name), keys, read) : null;

        public List<T>? Array<T>(string name, bool required, int minItems, int maxItems, Func<JsonElement, string, T?> readItem)
            where T : class
        {
            if (!Get(name, required, out var e))
            {
                return null;
            }

            string path = PathOf(name);
            if (e.ValueKind != JsonValueKind.Array)
            {
                _parser.Error("schema.type", path, "Expected an array.");
                return null;
            }

            int count = e.GetArrayLength();
            if (count < minItems || count > maxItems)
            {
                _parser.Error("schema.items", path, $"{count} items is outside {minItems} to {maxItems}.");
                return null;
            }

            var items = new List<T>(count);
            int i = 0;
            foreach (var item in e.EnumerateArray())
            {
                var value = readItem(item, Child(path, i++));
                if (value is not null)
                {
                    items.Add(value);
                }
            }

            return items;
        }

        private bool Get(string name, bool required, out JsonElement element)
        {
            if (_members.TryGetValue(name, out element))
            {
                return true;
            }

            if (required)
            {
                _parser.Error("schema.required", _path, $"Required property \"{name}\" is missing.");
            }

            return false;
        }
    }
}
