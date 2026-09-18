using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace GroupLab.Core.Trace;

/// <summary>A stage's outcome, DETECTION-PIPELINE.md section 6.1.</summary>
public enum StageStatus
{
    Ok,
    Degraded,
    Failed,
}

/// <summary>A metric with its unit, DETECTION-PIPELINE.md section 6.1.</summary>
public sealed record NamedQuantity(string Name, double Value, string Unit);

/// <summary>
/// A resolved parameter, DETECTION-PIPELINE.md section 6.1: the value actually used, in physical and pixel units both,
/// so a wrong scale shows up as an absurd pixel figure rather than as a quietly poor result.
/// </summary>
public sealed record NamedParameter(string Name, string Value);

/// <summary>A fork the stage took, with the alternatives it did not, DETECTION-PIPELINE.md section 6.1.</summary>
public sealed record Decision(string What, string Chosen, IReadOnlyList<string> Alternatives, string Because);

/// <summary>Something the stage threw away, with its page position in inches when it has one, DETECTION-PIPELINE.md section 6.1.</summary>
public sealed record Rejection(string What, double? XInches, double? YInches, string Why);

/// <summary>
/// One stage's record, DETECTION-PIPELINE.md section 6.1. Phase 0 produces no raster artefacts, so the record carries
/// none; <see cref="Details"/> holds the continuation lines of the console form of section 6.3.
/// </summary>
public sealed class StageRecord
{
    private readonly List<string> _details = [];
    private readonly List<NamedQuantity> _metrics = [];
    private readonly List<Decision> _decisions = [];
    private readonly List<Rejection> _rejections = [];
    private readonly List<NamedParameter> _parameters = [];

    internal StageRecord(string stage, int sequence, DateTime startedUtc)
    {
        Stage = stage;
        Sequence = sequence;
        StartedUtc = startedUtc;
    }

    public string Stage { get; }

    public int Sequence { get; }

    public DateTime StartedUtc { get; }

    public long DurationMs { get; internal set; }

    public StageStatus Status { get; internal set; }

    public string Summary { get; internal set; } = "";

    public IReadOnlyList<string> Details => _details;

    public IReadOnlyList<NamedQuantity> Metrics => _metrics;

    public IReadOnlyList<Decision> Decisions => _decisions;

    public IReadOnlyList<Rejection> Rejections => _rejections;

    public IReadOnlyList<NamedParameter> Parameters => _parameters;

    internal void AddDetail(string line) => _details.Add(line);

    internal void AddMetric(NamedQuantity metric) => _metrics.Add(metric);

    internal void AddDecision(Decision decision) => _decisions.Add(decision);

    internal void AddRejection(Rejection rejection) => _rejections.Add(rejection);

    internal void AddParameter(NamedParameter parameter) => _parameters.Add(parameter);
}

/// <summary>Collects the records of one analysis in the order the stages finish.</summary>
public sealed class TraceRecorder
{
    private readonly List<StageRecord> _records = [];
    private int _sequence;

    public IReadOnlyList<StageRecord> Records => _records;

    public StageScope Begin(string stage) => new(this, new StageRecord(stage, ++_sequence, DateTime.UtcNow));

    /// <summary>
    /// Raised as each stage files its record, on the thread that ran it, DESIGN.md section 19 [r3]: a live run shows each stage as it lands.
    /// Nothing listens in a batch run, and an event nobody listens to costs nothing, which is the second of section 19's two constraints:
    /// the theatre must not slow the pipeline down.
    /// </summary>
    public event Action<StageRecord>? Filed;

    internal void Add(StageRecord record)
    {
        _records.Add(record);
        Filed?.Invoke(record);
    }
}

/// <summary>
/// A stage in progress. Disposing it files the record; a stage disposed without <see cref="Done"/> is recorded as
/// failed, so the trace is never the only place a failure is missing from (DETECTION-PIPELINE.md section 6.4).
/// </summary>
public sealed class StageScope : IDisposable
{
    private readonly TraceRecorder _recorder;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _done;
    private bool _disposed;

    internal StageScope(TraceRecorder recorder, StageRecord record)
    {
        _recorder = recorder;
        Record = record;
    }

    public StageRecord Record { get; }

    public void Metric(string name, double value, string unit) => Record.AddMetric(new NamedQuantity(name, value, unit));

    public void Parameter(string name, string value) => Record.AddParameter(new NamedParameter(name, value));

    public void Decide(string what, string chosen, string because, params string[] alternatives) =>
        Record.AddDecision(new Decision(what, chosen, alternatives, because));

    public void Reject(string what, string why, PointInches? at = null) =>
        Record.AddRejection(new Rejection(what, at?.X, at?.Y, why));

    public void Detail(string line) => Record.AddDetail(line);

    public void Done(StageStatus status, string summary)
    {
        Record.Status = status;
        Record.Summary = summary;
        _done = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_done)
        {
            Record.Status = StageStatus.Failed;
            Record.Summary = "did not complete";
        }

        Record.DurationMs = _clock.ElapsedMilliseconds;
        _recorder.Add(Record);
    }
}

/// <summary>A page position in inches, the unit DETECTION-PIPELINE.md section 6.1 gives rejections.</summary>
public readonly record struct PointInches(double X, double Y)
{
    public static PointInches FromDmm(double x, double y) => new(x / 254.0, y / 254.0);
}

/// <summary>
/// The console form of DETECTION-PIPELINE.md section 6.3. Verbosity 1 prints summary lines, 2 adds decisions, 3 adds
/// rejections.
/// </summary>
public static class TraceConsole
{
    private const int Indent = 37;

    public static string Format(StageRecord record, int verbosity)
    {
        ArgumentNullException.ThrowIfNull(record);
        var inv = CultureInfo.InvariantCulture;
        var s = new StringBuilder();
        string status = record.Status switch
        {
            StageStatus.Ok => "ok",
            StageStatus.Degraded => "degraded",
            _ => "failed",
        };
        s.Append(inv, $"[{record.Stage,-13}] {record.DurationMs,6}ms  {status,-9} {record.Summary}\n");
        string pad = new(' ', Indent);
        foreach (string line in record.Details)
        {
            s.Append(pad).Append(line).Append('\n');
        }

        if (verbosity >= 2)
        {
            foreach (var d in record.Decisions)
            {
                s.Append(pad).Append(inv, $"decided {d.What}: {d.Chosen}, because {d.Because}");
                s.Append(d.Alternatives.Count > 0 ? $"; not {string.Join(", ", d.Alternatives)}\n" : "\n");
            }

            foreach (var p in record.Parameters)
            {
                s.Append(pad).Append(inv, $"{p.Name}: {p.Value}\n");
            }
        }

        if (verbosity >= 3)
        {
            foreach (var r in record.Rejections)
            {
                string where = r.XInches is { } x && r.YInches is { } y ? string.Create(inv, $" at ({x:0.000}, {y:0.000}) in") : "";
                s.Append(pad).Append(inv, $"rejected {r.What}{where}: {r.Why}\n");
            }
        }

        return s.ToString();
    }
}
