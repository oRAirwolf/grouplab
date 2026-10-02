using System.Globalization;

namespace GroupLab.Core.Records;

/// <summary>What one chronograph reading goes with, NOTES-FROM-PLANNING.md entry 351: the choices of the phone's sheet and the computer's rows.</summary>
public enum ReadingGoesWith
{
    /// <summary>A shot of this group, named by <see cref="ReadingMark.ShotId"/>.</summary>
    Shot,

    /// <summary>Another group's: fired before or after this one.</summary>
    NotThisGroup,

    /// <summary>This group's clean bore shot, the fouling shot often fired off the paper, so beside no hole.</summary>
    CleanBore,

    /// <summary>This group's, and left out of the pairing and the figures.</summary>
    LeftOut,
}

/// <summary>One reading's mark: what it goes with, and the shot where it is one.</summary>
public sealed record ReadingMark(ReadingGoesWith Kind, int? ShotId = null);

/// <summary>How a mark is drawn: paired in teal, one that needs a look in amber, "Not this group" plain.</summary>
public enum MarkTone
{
    Paired,
    NeedsLook,
    Plain,
}

/// <summary>A pause between two readings long enough to split the string, drawn as a thin labelled divider after reading <see cref="After"/>.</summary>
public sealed record ReadingPause(int After, TimeSpan Length)
{
    public string Words => string.Create(CultureInfo.InvariantCulture, $"{Math.Max(1, (int)Math.Round(Length.TotalMinutes))} minute pause");
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 351, pairing A: a chronograph string's readings each with its own mark, in the order fired, starting from what
/// entry 342's proposal says and changed one reading at a time. Changing one mark never silently changes another: a reading given a shot
/// another reading has swaps places with it, and says so; a reading taken off its shot leaves that shot with no reading, and says that too.
/// The phone's sheet and the computer's rows offer the same choices in the same words (<see cref="Choices"/>).
/// </summary>
public sealed class ChronographMarks
{
    /// <summary>The sheet's question for a reading: "Reading 4, 1041 fps, goes with".</summary>
    public string Asks(int reading) => string.Create(CultureInfo.InvariantCulture, $"Reading {reading + 1}, {Speed(reading)} {Unit}, goes with");

    /// <summary>The four choices, in the sheet's order, with their words.</summary>
    public static IReadOnlyList<(ReadingGoesWith Kind, string Words)> Choices { get; } =
    [
        (ReadingGoesWith.NotThisGroup, NotThisGroupWords),
        (ReadingGoesWith.Shot, AShotWords),
        (ReadingGoesWith.CleanBore, CleanBoreWords),
        (ReadingGoesWith.LeftOut, LeaveOutWords),
    ];

    public const string NotThisGroupWords = "Not this group";

    public const string AShotWords = "A shot of this group";

    public const string CleanBoreWords = "This group, clean bore";

    public const string LeaveOutWords = "Leave it out";

    /// <summary>The words at the bottom of the phone's page, as entry 351 section 4 names them.</summary>
    public const string KeepWords = "Keep this pairing";

    public const string UnpairedWords = "Leave unpaired";

    private readonly ReadingMark[] marks;
    private readonly IReadOnlyList<ChronographShot>? recorded;
    private readonly Func<int, string> shotName;

    /// <param name="shots">The group's shots in the order fired, by id.</param>
    /// <param name="readings">The readings in ft/s, in the order the chronograph recorded them.</param>
    /// <param name="recorded">What the file says of each reading, its number, time and marks, where the string was imported and still matches it.</param>
    /// <param name="shotName">A shot's number as the screen shows it, from its id.</param>
    /// <param name="inMetres">Whether the string's own unit is m/s.</param>
    public ChronographMarks(IReadOnlyList<int> shots, IReadOnlyList<double> readings, IReadOnlyList<ChronographShot>? recorded, Func<int, string> shotName, bool inMetres = false)
    {
        ArgumentNullException.ThrowIfNull(shots);
        ArgumentNullException.ThrowIfNull(readings);
        Shots = shots;
        Readings = readings;
        this.recorded = recorded is { } r && r.Count == readings.Count && r.Count > 0 ? r : null;
        this.shotName = shotName ?? throw new ArgumentNullException(nameof(shotName));
        InMetres = inMetres;
        var proposal = this.recorded is null ? null : ChronographReconciliation.Propose(shots, this.recorded);
        Reasons = proposal?.Reasons ?? [];
        var pairs = proposal?.Pairs(shots, readings) ?? Chronograph.Pair(shots, readings);
        marks = new ReadingMark[readings.Count];
        for (int i = 0; i < marks.Length; i++)
        {
            marks[i] = new ReadingMark(ReadingGoesWith.LeftOut);
        }

        foreach (var pair in pairs.Where(p => p.Reading is not null))
        {
            int reading = pair.Reading!.Value;
            marks[reading] = pair.ShotId is { } shot ? new ReadingMark(ReadingGoesWith.Shot, shot)
                : proposal is not null && proposal.Kinds.TryGetValue(reading, out var kind) ? new ReadingMark(kind)
                : new ReadingMark(ReadingGoesWith.LeftOut);
        }

        Pauses = this.recorded is null ? [] : PausesOf(this.recorded);
    }

    public IReadOnlyList<int> Shots { get; }

    /// <summary>A shot's number as the screen shows it.</summary>
    public string ShotName(int shot) => shotName(shot);

    public IReadOnlyList<double> Readings { get; }

    public bool InMetres { get; }

    public string Unit => InMetres ? "m/s" : "fps";

    /// <summary>Why the proposal is as it is, entry 342's sentences; none for a list typed by hand.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>The pauses that split the string, from the chronograph's own times.</summary>
    public IReadOnlyList<ReadingPause> Pauses { get; }

    public ReadingMark this[int reading] => marks[reading];

    public int Count => marks.Length;

    /// <summary>A reading's speed in the string's own unit.</summary>
    public string Speed(int reading) => InMetres
        ? (Readings[reading] * 0.3048).ToString("0.0", CultureInfo.InvariantCulture)
        : Readings[reading].ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>A reading's time as the chronograph recorded it, hours and minutes, or empty.</summary>
    public string Time(int reading) => recorded?[reading].Time is { } t ? t.ToString(@"hh\:mm", CultureInfo.InvariantCulture) : "";

    /// <summary>Whether any reading has a time, so the time column is worth its room.</summary>
    public bool Timed => recorded?.Any(r => r.Time is not null) == true;

    /// <summary>The words on a reading's mark: "Shot 2", "Not this group", "Shot 5, left out in ShotView", "Shot 1, clean bore".</summary>
    public string Label(int reading)
    {
        var mark = marks[reading];
        string flags = (recorded?[reading] is { } r ? (r.LeftOutByChronograph ? ", left out in ShotView" : "") + (r.CleanBore ? ", clean bore" : "") : "");
        return mark.Kind switch
        {
            ReadingGoesWith.Shot => "Shot " + shotName(mark.ShotId!.Value) + flags,
            ReadingGoesWith.NotThisGroup => NotThisGroupWords,
            ReadingGoesWith.CleanBore => CleanBoreWords,
            _ => "Left out",
        };
    }

    /// <summary>How the mark is drawn: a plain pairing teal, one the chronograph flagged or one left apart amber, another group's plain.</summary>
    public MarkTone Tone(int reading) => marks[reading].Kind switch
    {
        ReadingGoesWith.Shot => recorded?[reading] is { LeftOutByChronograph: true } or { CleanBore: true } ? MarkTone.NeedsLook : MarkTone.Paired,
        ReadingGoesWith.NotThisGroup => MarkTone.Plain,
        _ => MarkTone.NeedsLook,
    };

    /// <summary>The reading that goes with a shot, or null.</summary>
    public int? ReadingOf(int shot)
    {
        for (int i = 0; i < marks.Length; i++)
        {
            if (marks[i] is { Kind: ReadingGoesWith.Shot } m && m.ShotId == shot)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// Gives a reading a new mark. A shot another reading has goes to this one and that reading takes this one's old mark: a swap, said. A
    /// reading taken off its shot leaves the shot with no reading, said. Nothing else changes. The sentence, or null where nothing changed.
    /// </summary>
    public string? Set(int reading, ReadingGoesWith kind, int? shot = null)
    {
        if (reading < 0 || reading >= marks.Length || (kind == ReadingGoesWith.Shot && (shot is null || !Shots.Contains(shot.Value))))
        {
            return null;
        }

        var was = marks[reading];
        var now = new ReadingMark(kind, kind == ReadingGoesWith.Shot ? shot : null);
        if (was == now)
        {
            return null;
        }

        var inv = CultureInfo.InvariantCulture;
        if (kind == ReadingGoesWith.Shot && ReadingOf(shot!.Value) is { } other && other != reading)
        {
            marks[reading] = now;
            marks[other] = was;
            return string.Create(inv, $"Reading {reading + 1} now goes with shot {shotName(shot.Value)}. Reading {other + 1}, which had it, swapped places with it and is now {Label(other)}.");
        }

        marks[reading] = now;
        return was is { Kind: ReadingGoesWith.Shot, ShotId: { } freed }
            ? string.Create(inv, $"Reading {reading + 1} is now {Label(reading)}, so shot {shotName(freed)} has no reading.")
            : string.Create(inv, $"Reading {reading + 1} is now {Label(reading)}.");
    }

    /// <summary>The pairing as it stands: each shot with its reading or none, in firing order, then the readings beside no shot.</summary>
    public IReadOnlyList<ChronographPair> Pairs()
    {
        var pairs = Shots.Select(s => new ChronographPair(s, ReadingOf(s))).ToList();
        pairs.AddRange(Enumerable.Range(0, marks.Length).Where(i => marks[i].Kind != ReadingGoesWith.Shot).Select(i => new ChronographPair(null, i)));
        return pairs;
    }

    /// <summary>The card's first line: what is proposed, in one phrase.</summary>
    public string Headline
    {
        get
        {
            var inv = CultureInfo.InvariantCulture;
            var group = Enumerable.Range(0, marks.Length).Where(i => marks[i].Kind != ReadingGoesWith.NotThisGroup).ToList();
            if (group.Count > 0 && group.Count < marks.Length && group[^1] - group[0] + 1 == group.Count)
            {
                return string.Create(inv, $"Proposed: readings {group[0] + 1} to {group[^1] + 1} are this group");
            }

            int paired = marks.Count(m => m.Kind == ReadingGoesWith.Shot);
            return paired == marks.Length && paired == Shots.Count
                ? "Proposed: each reading goes with the shot fired in its place"
                : string.Create(inv, $"Proposed: {paired} of {marks.Length} readings go with a shot");
        }
    }

    /// <summary>The card's words under the headline: the reasons, or for a typed list what the counts say, then how to change a mark.</summary>
    public string Says(bool touch)
    {
        var inv = CultureInfo.InvariantCulture;
        string why;
        if (Reasons.Count > 0)
        {
            why = string.Join(" ", Reasons);
        }
        else
        {
            int withoutReading = Shots.Count(s => ReadingOf(s) is null);
            int apart = marks.Count(m => m.Kind != ReadingGoesWith.Shot);
            why = withoutReading == 0 && apart == 0
                ? "The counts agree; check the order, because a chronograph can miss a shot and record a neighbor's."
                : string.Join(" and ", new[]
                {
                    withoutReading > 0 ? string.Create(inv, $"{withoutReading} shot{(withoutReading == 1 ? " has" : "s have")} no reading") : null,
                    apart > 0 ? string.Create(inv, $"{apart} reading{(apart == 1 ? " is" : "s are")} beside no shot") : null,
                }.OfType<string>()) + ", and nothing in what was typed says which.";
        }

        return why + (touch ? " Tap any mark to change it." : " Click any mark to change it.");
    }

    private static List<ReadingPause> PausesOf(IReadOnlyList<ChronographShot> readings)
    {
        var runs = ChronographReconciliation.Runs(readings);
        var pauses = new List<ReadingPause>();
        for (int i = 1; i < runs.Count; i++)
        {
            int after = runs[i - 1].Last;
            if (readings[after].Time is { } a && readings[runs[i].First].Time is { } b)
            {
                pauses.Add(new ReadingPause(after, b - a));
            }
        }

        return pauses;
    }
}
