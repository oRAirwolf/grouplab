using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Gltd.Validation;

/// <summary>
/// Cross-field validation of a structurally valid definition: TARGET-SCHEMA.md section 10 tests 12 to 26f, 34
/// and 37, and the clearance warnings of <c>tools/layout</c> that docs/SPEC-ERRATA.md C5 carries over. Label
/// boxes join the overlap test with the renderer, which is what fixes where a label sits.
/// </summary>
public static class GltdValidator
{
    public const int QrModuleFloor = 3;
    public const int QrModuleWarning = 4;
    public const int SafeMargin = 120;
    public const int TightEdge = 60;
    public const int MajorClearance = 30;
    public const int MarkerClearance = 20;
    public const int SideBand = 150;

    public static IReadOnlyList<Diagnostic> Validate(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var run = new Run(definition);
        run.Execute();
        return run.Diagnostics;
    }

    private enum Kind
    {
        Bull,
        Marker,
        Code,
        DataBlock,
        GridField,
        Label,
    }

    private sealed record Element(Kind Kind, string Name, string Path, Box2 Box);

    private sealed class Run(TargetDefinition d)
    {
        private readonly Dictionary<string, Ink> _inks = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RingSet> _ringSets = new(StringComparer.Ordinal);

        public List<Diagnostic> Diagnostics { get; } = [];

        public void Execute()
        {
            CheckInks();
            CheckRingSets();
            CheckReferences();
            CheckPage();
            CheckCodeModule();
            var layout = BullLayout.Recognise(d);
            CheckPitch(layout);
            CheckSighterGap(layout);
            CheckCells(layout);
            var markers = CheckMarkers();
            CheckBracketing(markers);
            var codes = CheckCodePositions();
            CheckDataBlock();
            CheckGrids(markers, codes);
            CheckGeometry(markers, codes);
        }

        private void Error(string code, string path, string message, string? test) =>
            Diagnostics.Add(Diagnostic.Error(code, path, message, test));

        private void Warn(string code, string path, string message, string? test) =>
            Diagnostics.Add(Diagnostic.Warning(code, path, message, test));

        private void CheckInks()
        {
            int paper = 0;
            for (int i = 0; i < d.Inks.Count; i++)
            {
                var ink = d.Inks[i];
                if (!_inks.TryAdd(ink.Key, ink))
                {
                    Error("validate.duplicateKey", $"/inks/{i}/key", $"Ink key \"{ink.Key}\" is used twice.", "12");
                }

                if (ink.Role == InkRole.Paper && ++paper == 2)
                {
                    Error("validate.paperInks", $"/inks/{i}", "More than one ink carries the paper role (section 3.3).", "21");
                }
            }
        }

        private void CheckRingSets()
        {
            for (int i = 0; i < d.RingSets.Count; i++)
            {
                var set = d.RingSets[i];
                if (!_ringSets.TryAdd(set.Key, set))
                {
                    Error("validate.duplicateKey", $"/ringSets/{i}/key", $"Ring set key \"{set.Key}\" is used twice.", "12");
                }

                for (int k = 0; k < set.Discs.Count; k++)
                {
                    RequireInk(set.Discs[k].Ink, $"/ringSets/{i}/discs/{k}/ink");
                    if (k > 0 && set.Discs[k].Diameter >= set.Discs[k - 1].Diameter)
                    {
                        Error("validate.discOrder", $"/ringSets/{i}/discs/{k}/diameter",
                            $"Diameter {set.Discs[k].Diameter} does not decrease from {set.Discs[k - 1].Diameter}; discs are outermost first (section 3.4).", "20");
                    }
                }
            }
        }

        private void CheckReferences()
        {
            for (int i = 0; i < d.Bulls.Count; i++)
            {
                if (!_ringSets.ContainsKey(d.Bulls[i].RingSet))
                {
                    Error("validate.unresolvedRingSet", $"/bulls/{i}/ringSet", $"No ring set has key \"{d.Bulls[i].RingSet}\".", "12");
                }
            }

            if (d.Fiducials is { } f && RequireInk(f.Ink, "/fiducials/ink") is { } fiducialInk && fiducialInk.Role != InkRole.Fiducial)
            {
                Error("validate.inkRole", "/fiducials/ink", $"The fiducial ink \"{f.Ink}\" has role {GltdNames.InkRole.NameOf(fiducialInk.Role)}; section 3.7 requires fiducial.", "12");
            }

            if (d.Cells?.Ink is { } cellInk)
            {
                RequireInk(cellInk, "/cells/ink");
            }

            if (d.DataBlock is { } b)
            {
                RequireInk(b.Ink, "/dataBlock/ink");
                if (b.LabelInk is not null)
                {
                    RequireInk(b.LabelInk, "/dataBlock/labelInk");
                }
            }

            for (int i = 0; i < (d.Grids?.Count ?? 0); i++)
            {
                var g = d.Grids![i];
                foreach (var (key, name) in new[] { (g.MinorInk, "minorInk"), (g.MajorInk, "majorInk"), (g.AxisInk, "axisInk"), (g.LabelInk, "labelInk") })
                {
                    if (key is not null)
                    {
                        RequireInk(key, $"/grids/{i}/{name}");
                    }
                }
            }
        }

        private Ink? RequireInk(string key, string path)
        {
            if (_inks.TryGetValue(key, out var ink))
            {
                return ink;
            }

            Error("validate.unresolvedInk", path, $"No ink has key \"{key}\".", "12");
            return null;
        }

        private void CheckPage()
        {
            var p = d.Page;
            if (PageSizes.Standard(p.Size) is { } standard && (standard.Width != p.Width || standard.Height != p.Height))
            {
                Warn("validate.pageSize", "/page",
                    $"A {GltdNames.PageSize.NameOf(p.Size)} page is {standard.Width} by {standard.Height} dmm, not {p.Width} by {p.Height}.", "17");
            }

            if (PageSizes.RollWidth(p.Size) is { } rollWidth && rollWidth != p.Width)
            {
                Warn("validate.pageSize", "/page/width", $"A {GltdNames.PageSize.NameOf(p.Size)} roll is {rollWidth} dmm wide, not {p.Width}.", "17");
            }
        }

        private void CheckCodeModule()
        {
            if (d.Codes is not { } c)
            {
                return;
            }

            if (c.ModuleSize < QrModuleFloor)
            {
                Error("validate.moduleSize", "/codes/moduleSize", $"A {c.ModuleSize} dmm module is below the {QrModuleFloor} dmm floor (section 7).", "18");
            }
            else if (c.ModuleSize < QrModuleWarning)
            {
                Warn("validate.moduleSize", "/codes/moduleSize", $"A {c.ModuleSize} dmm module is below the {QrModuleWarning} dmm a consumer inkjet prints reliably (section 7).", "18");
            }
        }

        private void CheckPitch(ParametricLayout? layout)
        {
            if (d.Fiducials?.Scheme is not ("grid-boundary-1" or "grid-boundary-half-1") || (d.Cells?.Grid is null && layout is null))
            {
                return;
            }

            int multiple = d.Fiducials.Scheme == "grid-boundary-half-1" ? 4 : 2;
            int pitchX = d.Cells?.Grid?.PitchX ?? layout!.Grid.PitchX;
            int pitchY = d.Cells?.Grid?.PitchY ?? layout!.Grid.PitchY;
            if (pitchX % multiple != 0 || pitchY % multiple != 0)
            {
                Error("validate.pitch", d.Cells?.Grid is null ? "/bulls" : "/cells/grid",
                    $"{d.Fiducials.Scheme} needs a pitch divisible by {multiple} dmm so its lattice lands on integers, not {pitchX} by {pitchY}.", "19");
            }
        }

        private void CheckSighterGap(ParametricLayout? layout)
        {
            if (layout is not { Sighters.Count: > 0 } || d.Cells?.SighterGap is not null)
            {
                return;
            }

            var g = layout.Grid;
            int gap = layout.Sighters[0].OriginY - (g.OriginY + ((g.Rows - 1) * g.PitchY));
            if (Math.Abs((5L * gap) - (6L * g.PitchY)) > 5 && !ShortenedToBracket(g, gap))
            {
                Warn("validate.sighterGap", "/cells/sighterGap",
                    $"The sighter row is {gap} dmm below the last scoring row, not 1.2 times the {g.PitchY} dmm pitch, and no cells.sighterGap declares the departure (section 7).", "23");
            }
        }

        /// <summary>
        /// Test 23's exception: a gap shorter than the convention needs no declaration when the shortening is what brings
        /// the sighter row inside the fiducial lattice, because that is the one reason a generator shortens it (section 7).
        /// Both halves are checked, so a gap shortened where the lattice already brackets still warns.
        /// </summary>
        private bool ShortenedToBracket(GridLayout g, int gap)
        {
            int convention = (int)Math.Round(6.0 * g.PitchY / 5, MidpointRounding.AwayFromZero);
            var sighters = d.Bulls.Where(b => !b.Scoring).ToList();
            if (gap >= convention || sighters.Count == 0 || Lattice(d) is not { } now || !sighters.All(b => Inside(now, b.X, b.Y)))
            {
                return false;
            }

            int shift = convention - gap;
            var atConvention = d with { Bulls = [.. d.Bulls.Select(b => b.Scoring ? b : b with { Y = b.Y + shift })] };
            return Lattice(atConvention) is not { } before || !sighters.All(b => Inside(before, b.X, b.Y + shift));
        }

        /// <summary>
        /// Section 7: every bull centre, sighters included, must lie on or inside the rectangle bounded by the outermost
        /// marker centres (test 26f), inclusively, so a centre on the lattice edge conforms. An error since the geometry
        /// change that fixed the four built-in sheets which broke it (docs/NOTES-FROM-PLANNING.md entries 9 and 13).
        /// </summary>
        private void CheckBracketing(List<PointDmm> markers)
        {
            if (markers.Count == 0)
            {
                return;
            }

            var lattice = Bounds(markers);
            for (int i = 0; i < d.Bulls.Count; i++)
            {
                var b = d.Bulls[i];
                if (!Inside(lattice, b.X, b.Y))
                {
                    Error("validate.bracket", $"/bulls/{i}",
                        $"Bull {b.Label ?? i.ToString(System.Globalization.CultureInfo.InvariantCulture)} at ({b.X}, {b.Y}) lies outside the fiducial lattice, ({lattice.X0}, {lattice.Y0}) to ({lattice.X1}, {lattice.Y1}), and is extrapolated on any sheet that is not flat (section 7).", "26f");
                }
            }
        }

        /// <summary>The marker centres a definition's fiducials place on a sheet: stored for an explicit scheme, derived otherwise.</summary>
        private static (int X0, int Y0, int X1, int Y1)? Lattice(TargetDefinition definition)
        {
            IReadOnlyList<PointDmm>? markers = definition.Fiducials switch
            {
                null => null,
                { Scheme: "explicit" } f => f.Markers?.Select(m => new PointDmm(m.X, m.Y)).ToList(),
                _ => FiducialDerivation.Derive(definition).Markers?.Positions,
            };
            return markers is { Count: > 0 } ? Bounds(markers) : null;
        }

        private static (int X0, int Y0, int X1, int Y1) Bounds(IReadOnlyList<PointDmm> markers) =>
            (markers.Min(p => p.X), markers.Min(p => p.Y), markers.Max(p => p.X), markers.Max(p => p.Y));

        private static bool Inside((int X0, int Y0, int X1, int Y1) lattice, int x, int y) =>
            x >= lattice.X0 && x <= lattice.X1 && y >= lattice.Y0 && y <= lattice.Y1;

        /// <summary>
        /// Section 3.6: on a parametric layout the cell lattice is derived, half a pitch off each bull centre, and
        /// a stored <c>cells.grid</c> must equal it (test 24a). An undrawn lattice is clipped by the sheet, so only
        /// drawn boundaries that cross a tile boundary are an error (test 24).
        /// </summary>
        private void CheckCells(ParametricLayout? layout)
        {
            if (d.Cells is not { } cells)
            {
                return;
            }

            if (cells.Grid is { } cg)
            {
                if (layout is null)
                {
                    Error("validate.cellGrid", "/cells/grid", "cells.grid derives from a parametric bull grid, and these bulls do not form one (section 3.6).", "24a");
                }
                else
                {
                    var g = layout.Grid;
                    var (originX, originY) = BullLayout.CellOrigin(g);
                    if (cg.Cols != g.Cols || cg.Rows != g.Rows || cg.PitchX != g.PitchX || cg.PitchY != g.PitchY
                        || cg.OriginX != originX || cg.OriginY != originY)
                    {
                        Error("validate.cellGrid", "/cells/grid",
                            $"cells.grid differs from the derived lattice: {g.Cols} by {g.Rows} at pitch {g.PitchX} by {g.PitchY} " +
                            $"from origin ({originX}, {originY}); a mismatch is an error, not a repair (section 3.6).", "24a");
                    }
                }
            }

            if (cells.Drawn == true && d.Tiling is { } t && layout is { Grid: var grid })
            {
                long x0 = (2L * grid.OriginX) - grid.PitchX, y0 = (2L * grid.OriginY) - grid.PitchY;
                var lattice = new Box2(x0, y0, x0 + (2L * grid.Cols * grid.PitchX), y0 + (2L * grid.Rows * grid.PitchY));
                if (!lattice.Within(t.SheetWidth, t.SheetHeight))
                {
                    Error("validate.tileBoundary", "/cells/drawn",
                        "Drawn cell boundaries cross the tile boundary; only an undrawn cell region is clipped by the sheet (sections 3.6 and 3.12).", "24");
                }
            }
        }

        private List<PointDmm> CheckMarkers()
        {
            if (d.Fiducials is not { } f)
            {
                return [];
            }

            if (f.Scheme == "explicit")
            {
                if (f.Markers is null)
                {
                    Error("validate.markersRequired", "/fiducials/markers", "An explicit fiducial scheme needs a markers list (section 3.7).", "16");
                }

                return f.Markers?.Select(m => new PointDmm(m.X, m.Y)).ToList() ?? [];
            }

            var derivation = FiducialDerivation.Derive(d);
            if (derivation.Markers is not { } derived)
            {
                Error("validate.derivation", "/fiducials/scheme", derivation.Error!, "16");
                return [];
            }

            if (derived.Positions.Count < 4)
            {
                Error("validate.markerCount", "/fiducials", $"{f.Scheme} leaves {derived.Positions.Count} markers; a registration needs at least 4 (section 7).", "22");
            }
            else if (derived.Positions.Count < 8)
            {
                Warn("validate.markerCount", "/fiducials", $"{f.Scheme} leaves only {derived.Positions.Count} markers; fewer than 8 is fragile (section 7).", "22");
            }

            int dictionary = MarkerIds.DictionarySize(f.Family);
            if (dictionary == 0)
            {
                Error("validate.family", "/fiducials/family", "A derived fiducial scheme needs a marker family with identifiers.", "16");
                return [.. derived.Positions];
            }

            var assignment = MarkerIds.Assign(derived.Positions, d.Tiling, 0, dictionary);
            if (assignment.Wrapped)
            {
                Warn("validate.markerIdsWrap", "/fiducials",
                    $"The assembly needs {assignment.AssemblyCount} markers and the family holds {dictionary}, so identifiers repeat and only the tile index disambiguates them (section 3.7).", "34");
            }

            if (f.Markers is { } stored && !stored.SequenceEqual(assignment.Markers))
            {
                Error("validate.markersMismatch", "/fiducials/markers",
                    $"The stored marker list does not match the {assignment.Markers.Count} markers {f.Scheme} derives for tile 0; a mismatch is an error, not a repair (section 3.7).", "16");
            }

            return [.. derived.Positions];
        }

        private List<Element> CheckCodePositions()
        {
            if (d.Codes is not { } c)
            {
                return [];
            }

            if (c.Count > 0 && c.Positions.Count == 0)
            {
                Error("validate.codePositions", "/codes/positions", $"{c.Count} codes are declared and no positions are stored (section 3.8).", "26d");
            }

            IReadOnlyList<PointDmm> positions = c.Positions;
            int side;
            if (c.Placement == CodePlacement.Corners1)
            {
                side = Corners1.FootprintModules * c.ModuleSize;
                if (!Corners1.Supports(c.Count))
                {
                    Error("validate.codeCount", "/codes/count", $"corners-1 places 0, 2 or 4 codes, not {c.Count}.", "26d");
                }
                else
                {
                    var derived = Corners1.Positions(d.Page.Width, d.Page.Height, d.DataBlock?.Height ?? 0, c.Count, c.ModuleSize);
                    if (c.Positions.Count > 0 && !c.Positions.SequenceEqual(derived))
                    {
                        Error("validate.codePositions", "/codes/positions",
                            $"The stored code positions differ from the corners-1 centres {string.Join(" ", derived.Select(p => $"({p.X},{p.Y})"))}; a mismatch is an error, not a repair (section 3.8).", "26d");
                    }

                    positions = derived;
                }
            }
            else
            {
                side = (((4 * (c.Version ?? Binary.Projection.CodeVersion)) + 17) * c.ModuleSize) + (2 * (c.QuietZone ?? (4 * c.ModuleSize)));
            }

            return positions.Select((p, i) => new Element(Kind.Code, $"code {i}", $"/codes/positions/{i}", Box2.Square(p.X, p.Y, side))).ToList();
        }

        private void CheckDataBlock()
        {
            if (d.DataBlock is { } b && b.Reserve > b.Height)
            {
                Error("validate.reserve", "/dataBlock/reserve", $"The {b.Reserve} dmm reserved square does not fit a {b.Height} dmm block (section 3.10).", "25");
            }
        }

        private void CheckGrids(List<PointDmm> markers, List<Element> codes)
        {
            int footprint = d.Fiducials is { } f ? FiducialDerivation.Footprint(f) : 0;
            int dataBlockHeight = d.DataBlock?.Height ?? 0;
            for (int i = 0; i < (d.Grids?.Count ?? 0); i++)
            {
                var g = d.Grids![i];
                string path = $"/grids/{i}";
                var offsets = MeasurementGridLines.Offsets(g.Half, g.Divisions);
                for (int k = 1; k < offsets.Count; k++)
                {
                    if (offsets[k] <= offsets[k - 1])
                    {
                        Error("validate.gridLines", path, $"Minor lines {k - 1} and {k} coincide; a {g.Half} dmm half-extent cannot hold {g.Divisions} divisions (section 3.13).", "37");
                        break;
                    }
                }

                var field = Box2.Centred(g.CentreX, g.CentreY, 2L * g.Half, 2L * g.Half);
                int left = g.CentreX - g.Half - SafeMargin;
                int right = d.Page.Width - SafeMargin - (g.CentreX + g.Half);
                int top = g.CentreY - g.Half - SafeMargin;
                int bottom = d.Page.Height - SafeMargin - dataBlockHeight - (g.CentreY + g.Half);
                int band = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
                if (band < footprint + FiducialDerivation.Clearance)
                {
                    Error("validate.gridBand", path,
                        $"The grid field leaves {band} dmm inside the safe margins, less than a {footprint} dmm marker footprint plus {FiducialDerivation.Clearance} dmm of clearance (section 7).", "26");
                }

                if (Math.Min(left, right) < SideBand)
                {
                    Warn("validate.gridSideBand", path, $"The side band is {Math.Min(left, right)} dmm, below the {SideBand} dmm field-ring-1 is sized for.", "26");
                }

                int row = footprint + (2 * FiducialDerivation.Clearance);
                var above = codes.Where(c => c.Box.Y1 <= field.Y0).ToList();
                var below = codes.Where(c => c.Box.Y0 >= field.Y1).ToList();
                if (above.Count > 0)
                {
                    long topBand = ((field.Y0 - above.Max(c => c.Box.Y1)) / 2) - FiducialDerivation.Clearance;
                    if (topBand < row)
                    {
                        Warn("validate.markerRowBand", $"{path}/top",
                            $"The top band between the grid field and the code row is {topBand} dmm, below the {row} dmm a full marker row needs; that row survives only away from the codes.", "26");
                    }
                }

                if (below.Count > 0)
                {
                    long bottomBand = ((below.Min(c => c.Box.Y0) - field.Y1) / 2) - FiducialDerivation.Clearance;
                    if (bottomBand < row)
                    {
                        Warn("validate.markerRowBand", $"{path}/bottom",
                            $"The bottom band between the grid field and the code row is {bottomBand} dmm, below the {row} dmm a full marker row needs; that row survives only away from the codes.", "26");
                    }
                }

                foreach (var p in markers)
                {
                    if (Box2.Square(p.X, p.Y, footprint).Overlaps(field, FiducialDerivation.Clearance))
                    {
                        Error("validate.overlap", path, $"The marker at ({p.X}, {p.Y}) intrudes on the grid field.", "14");
                    }
                }

                foreach (var code in codes.Where(c => c.Box.Overlaps(field, FiducialDerivation.Clearance)))
                {
                    Error("validate.overlap", path, $"{code.Name} intrudes on the grid field.", "14");
                }
            }
        }

        private void CheckGeometry(List<PointDmm> markers, List<Element> codes)
        {
            var elements = new List<Element>();
            var outer = _ringSets.ToDictionary(s => s.Key, s => s.Value.Discs.Count == 0 ? 0 : s.Value.Discs.Max(disc => disc.Diameter), StringComparer.Ordinal);
            for (int i = 0; i < d.Bulls.Count; i++)
            {
                var b = d.Bulls[i];
                elements.Add(new Element(Kind.Bull, $"bull {i}", $"/bulls/{i}", Box2.Square(b.X, b.Y, outer.GetValueOrDefault(b.RingSet))));
            }

            int footprint = d.Fiducials is { } f ? FiducialDerivation.Footprint(f) : 0;
            elements.AddRange(markers.Select((p, i) => new Element(Kind.Marker, $"marker at ({p.X}, {p.Y})", $"/fiducials/markers/{i}", Box2.Square(p.X, p.Y, footprint))));
            elements.AddRange(codes);
            if (d.DataBlock is { } block)
            {
                elements.Add(new Element(Kind.DataBlock, "the data block", "/dataBlock", Box2.FromRect(block.X, block.Y, block.Width, block.Height)));
            }

            // Test 14 includes labels, at the position the renderer prints them (docs/SPEC-ERRATA.md C6).
            if (LabelLayout.Printed(d))
            {
                for (int i = 0; i < d.Bulls.Count; i++)
                {
                    if (outer.TryGetValue(d.Bulls[i].RingSet, out int diameter) && LabelLayout.Run(d.Bulls[i], diameter, default) is { } run)
                    {
                        elements.Add(new Element(Kind.Label, $"the label of bull {i}", $"/bulls/{i}/label", LabelLayout.Box(run)));
                    }
                }
            }

            foreach (var e in elements)
            {
                if (!e.Box.Within(d.Page.Width, d.Page.Height))
                {
                    Error("validate.offPage", e.Path, $"{Capitalise(e.Name)} extends beyond the page.", "13");
                }
                else if (e.Box.X0 < 2 * TightEdge || e.Box.Y0 < 2 * TightEdge || e.Box.X1 > 2L * (d.Page.Width - TightEdge) || e.Box.Y1 > 2L * (d.Page.Height - TightEdge))
                {
                    Warn("validate.tightMargin", e.Path, $"{Capitalise(e.Name)} comes within {TightEdge} dmm of the page edge.", null);
                }
            }

            if (d.Tiling is { } t)
            {
                if (t.SheetWidth != d.Page.Width || t.SheetHeight != d.Page.Height)
                {
                    Error("validate.tileSheet", "/tiling", $"A tile's sheet is its page (section 3.12): {t.SheetWidth} by {t.SheetHeight} dmm against a {d.Page.Width} by {d.Page.Height} dmm page.", "24");
                }

                foreach (var e in elements.Where(e => !e.Box.Within(t.SheetWidth, t.SheetHeight)))
                {
                    Error("validate.tileBoundary", e.Path, $"{Capitalise(e.Name)} crosses the tile boundary (section 3.12).", "24");
                }
            }

            for (int i = 0; i < elements.Count; i++)
            {
                for (int j = i + 1; j < elements.Count; j++)
                {
                    var (a, b) = (elements[i], elements[j]);
                    if (a.Box.Overlaps(b.Box))
                    {
                        string test = (a.Kind, b.Kind) switch
                        {
                            (Kind.Code, Kind.DataBlock) or (Kind.DataBlock, Kind.Code) => "26a",
                            (Kind.DataBlock, _) or (_, Kind.DataBlock) => "25",
                            _ => "14",
                        };
                        Error("validate.overlap", b.Path, $"{Capitalise(a.Name)} overlaps {b.Name}.", test);
                    }
                    else if (a.Kind == Kind.Marker && b.Kind == Kind.Marker)
                    {
                        if (a.Box.Overlaps(b.Box, MarkerClearance))
                        {
                            Warn("validate.clearance", b.Path, $"{Capitalise(a.Name)} is under {MarkerClearance} dmm from {b.Name}.", null);
                        }
                    }
                    else if (a.Kind is not (Kind.Marker or Kind.Label) && b.Kind is not (Kind.Marker or Kind.Label) && a.Box.Overlaps(b.Box, MajorClearance))
                    {
                        bool codeAndBlock = (a.Kind, b.Kind) is (Kind.Code, Kind.DataBlock) or (Kind.DataBlock, Kind.Code);
                        Warn("validate.clearance", b.Path, $"{Capitalise(a.Name)} is under {MajorClearance} dmm from {b.Name}.", codeAndBlock ? "26a" : null);
                    }
                }
            }

            if (d.Fiducials is { } fid)
            {
                foreach (var p in markers)
                {
                    foreach (var (bull, i) in d.Bulls.Select((b, i) => (b, i)))
                    {
                        long reach = outer.GetValueOrDefault(bull.RingSet) + FiducialDerivation.Footprint(fid);
                        long dx = 2L * (p.X - bull.X), dy = 2L * (p.Y - bull.Y);
                        if ((dx * dx) + (dy * dy) < reach * reach)
                        {
                            Error("validate.markerNearBull", $"/bulls/{i}",
                                $"The marker at ({p.X}, {p.Y}) is within the exclusion radius of bull {i}: its outermost disc radius plus half a marker footprint (section 7).", "15");
                        }
                    }
                }
            }
        }

        private static string Capitalise(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
    }
}
