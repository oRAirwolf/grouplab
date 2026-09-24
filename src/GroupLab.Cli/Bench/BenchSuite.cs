using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Analysis;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Printing;
using GroupLab.Core.Records;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Bench;

/// <summary>
/// Every case <c>grouplab bench</c> runs away from the window, NOTES-FROM-PLANNING.md entry 117 section 3a. The areas are the entry's own
/// list: definitions, rendering, images, measurement stage by stage, each statistic separately, the solver, storage, documents and end to end.
/// The screens and their controls are section 3b's, and are measured by the headless interface benchmark instead, because the window needs a
/// windowing platform this command does not carry.
/// </summary>
public static class BenchSuite
{
    /// <summary>The areas the record is grouped by, in the order it prints them.</summary>
    public static IReadOnlyList<string> Areas { get; } =
        ["definitions", "rendering", "images", "measurement", "statistics", "solver", "storage", "documents", "end to end"];

    /// <summary>
    /// What cannot be measured unattended, by name with its reason, entry 117 section 3b. Printing to a device would use paper and can open
    /// an application, a file dialog waits for a person, and deleting data destroys it; none of those belongs in something that runs on CI.
    /// </summary>
    public static IReadOnlyList<BenchExclusion> Exclusions { get; } =
    [
        new("printing", "print to a printer", "It uses paper and can open an application on the machine. The work before the device, laying the sheet out and checking it against the paper, is measured as \"sheet checked for a printer\"."),
        new("printing", "the system print dialog", "It waits for a person."),
        new("files", "open, save and export dialogs", "They are the operating system's own windows and wait for a person. What happens after the file is chosen is measured."),
        new("storage", "delete a session", "It destroys data. Saving, reading back and querying are measured."),
        new("network", "the update check and any link out", "GroupLab makes no network call, and a benchmark that made one would be the first."),
        new("interface", "every control on a screen", "Measured by the headless interface benchmark, which has a windowing platform; see docs/PERFORMANCE.md."),
    ];

    /// <summary>Every case, in record order.</summary>
    public static IReadOnlyList<BenchCase> All(BenchMaterial material)
    {
        ArgumentNullException.ThrowIfNull(material);
        var cases = new List<BenchCase>();
        cases.AddRange(Definitions(material));
        cases.AddRange(Rendering(material));
        cases.AddRange(Images(material));
        cases.AddRange(Measurement(material));
        cases.AddRange(Marking(material));
        cases.AddRange(Statistics(material));
        cases.AddRange(Solver());
        cases.AddRange(Storage(material));
        cases.AddRange(Documents(material));
        cases.AddRange(EndToEnd(material));
        return cases;
    }

    private static IEnumerable<BenchCase> Definitions(BenchMaterial m)
    {
        yield return new BenchCase("definitions", "read a sheet definition", "Reading GL-CF25-LTR.gltd.json from disk into the model.",
            _ => Describe(GltdJsonReader.ReadFile(m.DefinitionPath).Diagnostics.Count, "diagnostics"), ["GltdJsonReader"]);

        yield return new BenchCase("definitions", "validate a sheet definition", "Every conformance check the validator makes, on one sheet.",
            _ => Describe(GltdValidator.Validate(m.Definition).Count, "diagnostics"), ["GltdValidator"]);

        yield return new BenchCase("definitions", "derive the layout", "The bull lattice, the marker positions and ids, the code corners and the data block cells.",
            _ =>
            {
                var layout = BullLayout.Recognise(m.Definition);
                var derived = FiducialDerivation.Derive(m.Definition);
                var corners = Corners1.Positions(m.Definition.Page.Width, m.Definition.Page.Height, m.Definition.DataBlock?.Height ?? 0, m.Definition.Codes?.Count ?? 0, m.Definition.Codes?.ModuleSize ?? 4);
                // The 5 by 5 sheet has no data block and no measurement grid; the zeroing sheet has both, so both derivations are measured.
                var cells = m.Zeroing?.DataBlock is { } block ? DataBlockCells.Derive(block) : null;
                int lines = m.Zeroing?.Grids is { Count: > 0 } grids ? MeasurementGridLines.Positions(grids[0].CentreX, grids[0].Half, grids[0].Divisions).Count : 0;
                return string.Create(CultureInfo.InvariantCulture, $"{derived.Markers?.Positions.Count ?? 0} markers, {corners.Count} code corners, {(cells is null ? "no" : "a")} data block, {lines} grid lines, layout {(layout is null ? "not parametric" : "recognised")}");
            },
            ["BullLayout", "FiducialDerivation", "Corners1", "DataBlockCells", "MarkerIds", "MeasurementGridLines", "BullCells", "DerivedRounding", "Box2"]);

        yield return new BenchCase("definitions", "encode and decode the code payload", "The sheet to a GLTD-B body and frame, and back again.",
            _ =>
            {
                var encoded = GltdBinary.Encode(m.Definition);
                byte[] frame = GltdBinary.ReplicatedFrame(encoded.Encoding ?? throw new InvalidOperationException("the sheet did not encode, so there is nothing to decode back"));
                var decoded = GltdBinary.Decode([frame]);
                return string.Create(CultureInfo.InvariantCulture, $"{frame.Length} byte frame, decoded {(decoded.Definition is null ? "not" : "back")}");
            },
            ["GltdBinary", "BodyCodec", "FrameCodec", "Crc32", "DefinitionId", "ErasureCoder", "InstanceCodec", "Projection", "WireCodes", "BodyModel"]);
    }

    private static IEnumerable<BenchCase> Rendering(BenchMaterial m)
    {
        yield return new BenchCase("rendering", "sheet to PDF", "Building the sheet's pages and writing them as a PDF, which is what Save PDF does.",
            _ =>
            {
                var pages = SceneBuilder.Build(m.Definition).Pages;
                byte[] pdf = PdfWriter.Write(pages);
                return string.Create(CultureInfo.InvariantCulture, $"{pages.Count} page, {pdf.Length / 1024} kB");
            },
            ["SceneBuilder", "PdfWriter", "Scene", "HelveticaMetrics", "LabelLayout", "TargetRenderer", "Rgb"]);

        yield return new BenchCase("rendering", "sheet drawn for the printer", "Laying the sheet out for a 600 dpi Letter printer and checking every mark against the paper and the margins. The device itself is excluded.",
            _ =>
            {
                var printer = new PrinterPage("a 600 dpi Letter printer", 600, 600, 5100, 6600, 100, 100, 4900, 6400);
                string? refusal = PrintFit.Refusal(m.Pages, printer);
                int items = m.Pages.Sum(p => p.Items.Count);
                return string.Create(CultureInfo.InvariantCulture, $"{items} items, {(refusal is null ? "fits" : "refused")}");
            },
            ["PrintFit"]);

        yield return new BenchCase("rendering", "rasterise the sheet at 300 dpi", "Drawing the sheet into an image, which is what generates a sample and what the tests measure against.",
            _ =>
            {
                var image = SceneRasterizer.Rasterize(m.Pages[0], 300);
                return string.Create(CultureInfo.InvariantCulture, $"{image.Width} by {image.Height} px");
            },
            ["SceneRasterizer", "PixelRegion", "WarpRasterizer", "ArtworkFingerprint"]);

        yield return new BenchCase("rendering", "the icon set", "Every Windows and Linux icon size from the mark, which is a build step rather than something a person waits for.",
            _ =>
            {
                string svg = Path.Combine(m.Root, "src", "GroupLab.App", "Assets", "grouplab-mark.svg");
                if (!File.Exists(svg))
                {
                    throw new FileNotFoundException("the mark is not beside this build, so the icon set cannot be measured here", svg);
                }

                string into = Path.Combine(m.Temporary, "icons");
                IconSet.Write(svg, into, TextWriter.Null);
                return string.Create(CultureInfo.InvariantCulture, $"{Directory.GetFiles(into, "*", SearchOption.AllDirectories).Length} files");
            },
            ["IconSet"]);
    }

    private static IEnumerable<BenchCase> Images(BenchMaterial m)
    {
        yield return Loading("a 600 dpi Letter scan", m.Scan600);
        yield return Loading("the same sheet at 300 dpi", m.Scan300);
        yield return Loading("a phone photograph", m.Photograph);
        yield return Loading("the generated sample", m.GeneratedSheet);

        static BenchCase Loading(string what, string? path) =>
            new("images", "load " + what, "Decoding the file and building the grey and strongest-channel images the analysis works on.",
                _ =>
                {
                    if (path is null)
                    {
                        throw new FileNotFoundException($"{what} is not beside this build, so it cannot be measured here");
                    }

                    var (grey, _, metadata) = BenchMaterial.Load(path);
                    return string.Create(CultureInfo.InvariantCulture, $"{grey.Width} by {grey.Height} px, {metadata.Format}");
                },
                ["ImageLoader", "GrayImage", "ImageMetadata"]);
    }

    private static IEnumerable<BenchCase> Measurement(BenchMaterial m)
    {
        yield return new BenchCase("measurement", "identify the sheet from its codes", "Reading the printed codes and finding which definition they name, which is what happens before anything is measured.",
            _ =>
            {
                var identity = SheetIdentification.Identify(m.Loaded.Grey, SheetIdentification.Candidates([Path.Combine(m.Root, "targets")]), new OpenCvSharpBackend(), new TraceRecorder());
                return identity.Definition?.Name ?? "not identified: " + identity.Failure;
            },
            ["SheetIdentification"]);

        yield return new BenchCase("measurement", "the generated sheet, stage by stage", "One analysis of the 25 shot sample, with every stage of the pipeline filed as its own figure from the stage record.",
            sink =>
            {
                var trace = new TraceRecorder();
                var result = AutomaticMarking.Run(m.Loaded.Grey, m.Loaded.Value, m.Loaded.Metadata, m.Definition, new OpenCvSharpBackend(), trace, CancellationToken.None, null, artefacts: false);
                foreach (var record in trace.Records)
                {
                    sink.Part(record.Stage, record.DurationMs);
                }

                return string.Create(CultureInfo.InvariantCulture, $"{result.Detections.Count} holes, {result.MissingMarkers.Count} markers not found");
            },
            ["AutomaticMarking", "TraceRecorder", "StageRecord", "StageScope", "HoleSize"]);

        if (m.Scan600 is { } scan)
        {
            yield return new BenchCase("measurement", "a 600 dpi scan, stage by stage", "The same pipeline on Alan's own 600 dpi scan, which has no holes in it: the registration cost at the resolution a scanner gives.",
                sink =>
                {
                    var (grey, value, metadata) = BenchMaterial.Load(scan);
                    var trace = new TraceRecorder();
                    var result = AutomaticMarking.Run(grey, value, metadata, m.Definition, new OpenCvSharpBackend(), trace, CancellationToken.None, null, artefacts: false);
                    foreach (var record in trace.Records)
                    {
                        sink.Part(record.Stage, record.DurationMs);
                    }

                    return string.Create(CultureInfo.InvariantCulture, $"{result.Detections.Count} holes, {result.MissingMarkers.Count} markers not found");
                },
                ["AutomaticMarking"]);
        }
    }

    private static IEnumerable<BenchCase> Marking(BenchMaterial m)
    {
        yield return new BenchCase("measurement", "the review queue", "Everything the analysis wants settled before it will measure, raised from one marking.",
            _ =>
            {
                var items = ReviewQueue.For(State(m));
                return string.Create(CultureInfo.InvariantCulture, $"{items.Count} items, {ReviewQueue.Open(items)} open");
            },
            ["ReviewQueue", "MarkingSession", "ExclusionReasons"]);

        yield return new BenchCase("measurement", "what to say about the sheet", "The sentences the marking screen shows when a sheet is imperfect: the print scale, the doubt, and the refusal with what to do next.",
            _ =>
            {
                var sheet = Measured(m);
                string said = DetectionAdvice.PrintScale(sheet) ?? "nothing about the scale";
                said += DetectionAdvice.Suspect(sheet, m.Definition) ?? ", nothing to doubt";
                return said.Length > 60 ? said[..60] : said;
            },
            ["DetectionAdvice"]);

        yield return new BenchCase("measurement", "print the stage trace", "Laying the whole trace out as grouplab analyze prints it, at its fullest.",
            _ =>
            {
                // The analysis that produced the trace is measured by its own case, so this one is given a trace and times the printing.
                var records = Records(m);
                int length = records.Sum(r => TraceConsole.Format(r, 3).Length);
                return string.Create(CultureInfo.InvariantCulture, $"{length} characters over {records.Count} stages");
            },
            ["TraceConsole"]);

        yield return new BenchCase("rendering", "read the target library", "Every sheet in the library read and laid out, which is what the library screen shows.",
            _ =>
            {
                var sheets = TargetLibrary.Load(Path.Combine(m.Root, "targets"));
                return string.Create(CultureInfo.InvariantCulture, $"{sheets.Count} sheets");
            },
            ["TargetLibrary", "OwnSheets"]);

        yield return new BenchCase("storage", "a chronograph string", "Reading a string of velocities, pairing it with the shots and taking its spread, as the Ballistics screen does.",
            _ =>
            {
                var (velocities, refusal) = Chronograph.Read(string.Join(", ", Enumerable.Range(0, 25).Select(i => 2750 + (i % 7) - 3)));
                var shots = State(m).Shots.Where(x => x.IsShot).Select(x => x.Id).ToList();
                var pairs = Chronograph.Pair(shots, velocities);
                var spread = Chronograph.Spread(velocities);
                return refusal ?? string.Create(CultureInfo.InvariantCulture, $"{pairs.Count} pairs, SD {spread?.SdFps ?? 0:0.00} fps");
            },
            ["Chronograph"]);
    }

    private static IEnumerable<BenchCase> Statistics(BenchMaterial m)
    {
        var shots = Offsets(m);
        var (xx, xy, yy) = GroupStatistics.Covariance(shots);

        yield return new BenchCase("statistics", "sigma and mean radius with intervals", "The Rayleigh estimate and its exact interval, which every headline figure comes from.",
            _ =>
            {
                var estimate = GroupStatistics.Rayleigh(shots);
                return string.Create(CultureInfo.InvariantCulture, $"sigma {estimate.Sigma.Value:0.0000} in");
            },
            ["GroupStatistics", "RayleighEstimate", "SpecialFunctions", "Distributions", "Estimate"]);

        yield return new BenchCase("statistics", "the CEP table", "CEP 50 and CEP 90 under both the correlated normal and the Grubbs-Patnaik approximations.",
            _ => string.Create(CultureInfo.InvariantCulture, $"CEP 50 {GroupStatistics.CepCorrNormal(xx, xy, yy, 0.5):0.0000} in"),
            ["GroupStatistics"]);

        yield return new BenchCase("statistics", "the bootstrap interval", "The resampled interval around the mean radius, at the committed number of resamples.",
            _ =>
            {
                var interval = Bootstrap.Interval(shots, s => GroupStatistics.Radii(s, GroupStatistics.Centre(s)).Average());
                return string.Create(CultureInfo.InvariantCulture, $"{interval.Lower:0.000} to {interval.Upper:0.000} in");
            },
            ["Bootstrap", "StatisticsRandom"]);

        yield return new BenchCase("statistics", "the shape tests", "The circularity test with its resamples and the vertical stringing test, which are the two judgement cards.",
            _ =>
            {
                var circular = ShapeTests.Circularity(shots);
                var stringing = ShapeTests.VerticalStringing(shots);
                return string.Create(CultureInfo.InvariantCulture, $"circularity p {circular.PValue:0.000}, stringing correlation {stringing.Correlation:0.00}");
            },
            ["ShapeTests", "CircularAspect", "GroupGeometry"]);

        yield return new BenchCase("statistics", "the shot order trend", "Whether the group opened up as it was shot: a rank correlation against 9999 shuffles of the same shots, which is the slowest of the shape answers.",
            _ =>
            {
                var radii = GroupStatistics.Radii(shots, GroupStatistics.Centre(shots));
                var trend = ShotOrderTrend.Of(radii);
                return string.Create(CultureInfo.InvariantCulture, $"correlation {trend?.Correlation ?? 0:0.00}, p {trend?.PValue ?? 1:0.000}");
            },
            ["ShotOrderTrend"]);

        yield return new BenchCase("statistics", "the flyer calibration", "What the worst shot of a group this size is expected to be, which the worst-shot card is read against.",
            _ => string.Create(CultureInfo.InvariantCulture, $"worst expected at {Flyers.ExpectedWorstInSigmas(shots.Count):0.000} sigma"),
            ["Flyers", "SampleSize"]);

        yield return new BenchCase("statistics", "group comparison", "Two groups compared by dispersion and by centre, with the tests' verdicts and what each could have detected.",
            _ =>
            {
                var second = shots.Select(p => new PointD(p.X * 1.2, p.Y * 1.2)).ToList();
                var report = LoadComparison.Compare([("first", shots), ("second", second)], v => v.ToString("0.000", CultureInfo.InvariantCulture) + " in");
                return string.Create(CultureInfo.InvariantCulture, $"{report.Pairs.Count} pair");
            },
            ["LoadComparison", "GroupComparison", "Pooling", "ManovaResult", "TestResult"]);

        yield return new BenchCase("statistics", "hit probability", "The chance of a hit inside a named radius, at both ends of the sigma interval.",
            _ => string.Create(CultureInfo.InvariantCulture, $"{GroupStatistics.HitProbabilityCorrNormal(xx, xy, yy, 0.5) * 100:0.0} percent inside half an inch"),
            ["GroupStatistics"]);

        yield return new BenchCase("statistics", "the range statistic intervals", "Extreme spread and the other range statistics read off the simulated table, with their intervals.",
            _ =>
            {
                var sigma = RangeStatistics.Sigma(RangeStatistic.ExtremeSpread, 1.0, shots.Count);
                return string.Create(CultureInfo.InvariantCulture, $"sigma {sigma.Value:0.0000} in from a 1 in spread");
            },
            ["RangeStatistics", "RangeStatisticsTable", "RangeStatisticsSimulation", "IntervalCoverage", "Angular", "RangeMoments"]);

        yield return new BenchCase("statistics", "the zero correction", "The group centre's offset from the point of aim with its uncertainty, and the refusal when it cannot be told from chance.",
            _ =>
            {
                var state = State(m);
                var zero = Zeroing.For(state);
                return zero is null ? "no correction" : "a correction with its verdict";
            },
            ["ZeroCorrection", "Zeroing"]);

        yield return new BenchCase("statistics", "the whole analysis of a marking", "Every figure the analysis screen shows, from a marking: the path a person waits on after Accept.",
            _ =>
            {
                var report = GroupAnalysis.Analyse(State(m));
                return string.Create(CultureInfo.InvariantCulture, $"{report.AllShots?.Shots ?? 0} shots measured");
            },
            ["GroupAnalysis"]);
    }

    private static IEnumerable<BenchCase> Solver()
    {
        var input = new BallisticInput(0.243, DragModel.G7, 2750, 140, 1.8, 100, 59, null, 0, 50, 10, 0, ReferenceAtmosphere.Icao, 8, 1, 0.264, 1.35);

        yield return new BenchCase("solver", "one trajectory", "A single flight to 1000 yards at the solver's own step, which is what every dope table is made of.",
            _ =>
            {
                var trajectory = BallisticSolver.Solve(input, 1000, 100);
                return string.Create(CultureInfo.InvariantCulture, $"{trajectory.Points.Count} points, {trajectory.Points[^1].DropMoa:0.0} MOA at the far end");
            },
            ["BallisticSolver", "DragTables", "Atmosphere", "BallisticInput", "Trajectory", "Stability"]);

        yield return new BenchCase("solver", "a dope table", "The table the Ballistics screen shows, in the person's own units and clicks.",
            _ =>
            {
                var dope = SolverUse.Dope(input, 1000, 50);
                return string.Create(CultureInfo.InvariantCulture, $"{dope.Points.Count} rows");
            },
            ["SolverUse", "Projection"]);

        yield return new BenchCase("solver", "a hit probability", "Entry 156's answer at 600 yards on the middle confidence preset: ten thousand strings, the costs of every source and a curve against distance.",
            _ =>
            {
                var errors = new Dictionary<HitSource, HitUncertainty>(HitPresets.All[1].Errors(600)) { [HitSource.Velocity] = new(10), [HitSource.Zero] = new(0.05) };
                var setup = new HitSetup(input, 600, HitTarget.Circle(12), new HitPrecision(0.2, 18, 100), errors);
                var answer = HitProbability.Work(setup);
                var curve = HitProbability.Curve(setup, [300, 600, 900]);
                return string.Create(CultureInfo.InvariantCulture, $"{100 * answer.FirstRound.Value:0} percent first round, {answer.Costs.Count} costs, {curve.Count} curve points");
            },
            ["HitProbability", "HitPresets"]);
    }

    private static IEnumerable<BenchCase> Storage(BenchMaterial m)
    {
        yield return new BenchCase("storage", "save a session", "Writing one analysed sheet to the sessions database, as Save does.",
            _ =>
            {
                var store = SessionStore.Open(m.Database);
                long id = store.Save(Session(m, "bench"));
                return string.Create(CultureInfo.InvariantCulture, $"session {id}");
            },
            ["SessionStore", "SessionRecord"]);

        yield return new BenchCase("storage", "reopen a session", "Reading one back, which is what opening a session from the list does.",
            _ =>
            {
                var store = SessionStore.Open(m.Database);
                var summaries = store.List();
                var session = summaries.Count > 0 ? store.Get(summaries[0].Id) : null;
                return session is null ? "nothing to read" : string.Create(CultureInfo.InvariantCulture, $"{session.ShotCount} shots");
            },
            ["SessionStore"]);

        yield return new BenchCase("storage", "query a few hundred sessions", "The Session records list over a database the benchmark fills itself with 300 sessions.",
            _ =>
            {
                string path = Path.Combine(m.Temporary, "bench-many.db");
                if (!File.Exists(path))
                {
                    var seed = SessionStore.Open(path);
                    var one = Session(m, "bench");
                    for (int i = 0; i < 300; i++)
                    {
                        seed.Save(one with { Rifle = "rifle " + (i % 7), Load = "load " + (i % 13) });
                    }
                }

                var store = SessionStore.Open(path);
                int all = store.List().Count;
                int filtered = store.List(rifle: "rifle 3").Count;
                return string.Create(CultureInfo.InvariantCulture, $"{all} sessions, {filtered} on one rifle");
            },
            ["SessionStore", "SessionSummary"]);

        yield return new BenchCase("storage", "export and import the database", "The whole database out as JSON and back into an empty one, which is what a person's backup is.",
            _ =>
            {
                var store = SessionStore.Open(m.Database);
                string json = store.Export();
                string path = Path.Combine(m.Temporary, "bench-import.db");
                File.Delete(path);
                var into = SessionStore.Open(path);
                into.Import(json);
                return string.Create(CultureInfo.InvariantCulture, $"{json.Length / 1024} kB");
            },
            ["SessionStore", "ChronographString", "ShotVelocity"]);
    }

    private static IEnumerable<BenchCase> Documents(BenchMaterial m)
    {
        yield return Guide(m, "USER-GUIDE");
        yield return Guide(m, "TESTING-GUIDE");

        yield return new BenchCase("documents", "the volunteer pack", "The sheet and its page of instructions together, as the print screen gives them.",
            _ =>
            {
                byte[] pdf = VolunteerPack.Write(m.Definition, m.Pages);
                return string.Create(CultureInfo.InvariantCulture, $"{pdf.Length / 1024} kB");
            },
            ["VolunteerPack"]);

        static BenchCase Guide(BenchMaterial m, string name) =>
            new("documents", name.ToLowerInvariant() + " PDF", $"Laying {name}.md out and writing it as a PDF, pictures and all.",
                _ =>
                {
                    string path = Path.Combine(m.Root, "docs", name + ".md");
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException($"docs/{name}.md is not beside this build, so it cannot be measured here", path);
                    }

                    byte[] pdf = DocumentPdf.Write(File.ReadAllText(path), picture => GuideVerb.Picture(Path.Combine(m.Root, "docs", picture)));
                    return string.Create(CultureInfo.InvariantCulture, $"{pdf.Length / 1024} kB");
                },
                ["DocumentPdf", "ReportWriter"]);
    }

    private static IEnumerable<BenchCase> EndToEnd(BenchMaterial m)
    {
        yield return new BenchCase("end to end", "one sheet from file to figures", "Everything a person waits for after choosing an image: decoding it, identifying the sheet, registering, finding the holes, assigning them and measuring the group.",
            sink =>
            {
                var trace = new TraceRecorder();
                var (grey, value, metadata) = BenchMaterial.Load(m.GeneratedSheet);
                var result = SheetAnalysis.Run(m.GeneratedSheet, grey, value, metadata, m.Definition, new OpenCvSharpBackend(), trace);
                foreach (var record in trace.Records)
                {
                    sink.Part(record.Stage, record.DurationMs);
                }

                return string.Create(CultureInfo.InvariantCulture, $"{result.Shots.Count} shots, {(result.Failure is null ? "measured" : result.Failure)}");
            },
            ["SheetAnalysis"]);

        yield return new BenchCase("end to end", "a batch of ten sheets", "Ten analyses one after another, as analyze-folder runs them, where no picture is kept for a timeline.",
            _ =>
            {
                int shots = 0;
                for (int i = 0; i < 10; i++)
                {
                    var trace = new TraceRecorder();
                    var result = SheetAnalysis.Run(m.GeneratedSheet, m.Loaded.Grey, m.Loaded.Value, m.Loaded.Metadata, m.Definition, new OpenCvSharpBackend(), trace);
                    shots += result.Shots.Count;
                }

                return string.Create(CultureInfo.InvariantCulture, $"{shots} shots over ten sheets");
            },
            ["SheetAnalysis", "FolderVerbs"]);
    }

    private static MarkingState? state;
    private static SheetMeasurement? measured;
    private static IReadOnlyList<StageRecord>? records;
    private static IReadOnlyList<PointD>? offsets;

    /// <summary>The marking of the generated sheet, analysed once, which the statistics cases measure on.</summary>
    private static MarkingState State(BenchMaterial m)
    {
        if (state is not null)
        {
            return state;
        }

        var trace = new TraceRecorder();
        var result = SheetAnalysis.Run(m.GeneratedSheet, m.Loaded.Grey, m.Loaded.Value, m.Loaded.Metadata, m.Definition, new OpenCvSharpBackend(), trace);
        state = result.Marking ?? throw new InvalidOperationException("the benchmark's own sample did not register, so there is no marking to measure statistics on");
        measured = result.Automatic.Measurement;
        records = result.Trace;
        offsets = [.. result.Shots.Where(s => s.OffsetInches is not null).Select(s => s.OffsetInches!.Value)];
        return state;
    }

    private static IReadOnlyList<PointD> Offsets(BenchMaterial m)
    {
        State(m);
        return offsets!;
    }

    /// <summary>The stage records of that same analysis, so the case that prints them is not made to run an analysis first.</summary>
    private static IReadOnlyList<StageRecord> Records(BenchMaterial m)
    {
        State(m);
        return records!;
    }

    /// <summary>The sheet as the registration measured it, which is what the screen's sentences about the sheet are drawn from.</summary>
    private static SheetMeasurement Measured(BenchMaterial m)
    {
        State(m);
        return measured!;
    }

    private static SessionRecord Session(BenchMaterial m, string name) =>
        new(0, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), null, m.Definition.Name, m.Definition.Id, null, 100 * 36,
            name, null, name, 0.308, GroupAnalysis.Export(State(m)), Offsets(m).Count, null, null, null, null, null, null, null);

    private static string Describe(int count, string what) => string.Create(CultureInfo.InvariantCulture, $"{count} {what}");
}
