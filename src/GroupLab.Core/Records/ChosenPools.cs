using System.Globalization;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Records;

/// <summary>How Pool the chosen reads the sessions ticked.</summary>
public enum ChosenPoolKind
{
    /// <summary>Sheets of one set from Made for your optic, each counted by its own codes (entry 243).</summary>
    Set,

    /// <summary>Sheets of a set with loose sheets whose bulls are the set's, each standing in for a sheet missing (entry 512).</summary>
    StandIn,

    /// <summary>Any other sheets, each pooled about its own bulls with its own scale, in the order ticked (Alan's yes of 2026-11-24).</summary>
    Mixed,

    /// <summary>Nothing to pool: <see cref="ChosenPlan.Words"/> says why.</summary>
    Refused,
}

/// <summary>
/// What Pool the chosen will do with the sessions ticked: the sheets' markings in the order ticked, the set where it reads them as one, the
/// stand-ins where loose sheets take a set's places, and the words: a refusal, or for a mixed pool how it was pooled.
/// </summary>
public sealed record ChosenPlan(ChosenPoolKind Kind, IReadOnlyList<SessionRecord> Records, IReadOnlyList<MarkingState> States, TargetDefinition? Set,
    StandIn? StandIn, string? Words);

/// <summary>
/// Pool the chosen, on the computer and the phone alike. Report 57's suggestion: entry 512 said yes to a loose sheet standing in for a
/// set's sheet, and Alan said yes to the general case in his queue of 2026-11-24, so any sheets can be pooled: a loose sheet with sheets of a
/// set, sheets of different designs, or several loose sheets of one design. Sheets that read as one set still pool as one; anything else
/// pools each sheet about its own bulls with its own scale, as pooled pictures do (entry 423), because each shot is measured from its own
/// aim point either way. Sheets shot at different distances or with different calibers are refused, since their inches do not pool.
/// </summary>
public static class ChosenPools
{
    /// <summary>How far apart two sheets' distances may be and still pool, inches: half an inch, so 100 yd and 100 m are told apart.</summary>
    public const double DistanceTolerance = 0.5;

    /// <summary>How far apart two sheets' calibers may be and still pool, inches.</summary>
    public const double CalibreTolerance = 0.0005;

    /// <summary>Reads the sessions ticked, in the order given, and says how they pool.</summary>
    public static ChosenPlan Plan(IReadOnlyList<SessionRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var states = records.Select(r => MarkingFile.Read(r.MarkingJson).State).ToList();
        if (records.Count == 0)
        {
            return new ChosenPlan(ChosenPoolKind.Refused, records, states, null, null, PoolRefusal.NothingTicked);
        }

        var definitions = records.GroupBy(r => r.DefinitionId ?? "").ToDictionary(g => g.Key, g => g.First().DefinitionJson is { } json
            ? GltdJsonReader.Read(System.Text.Encoding.UTF8.GetBytes(json)).Definition : null);
        var ids = records.Select(r => r.DefinitionId ?? "").Distinct().ToList();
        string? why = null;
        if (ids.Count > 1 && SetPool.FindStandIn([.. records.Select((r, i) => (r.DefinitionId, definitions[r.DefinitionId ?? ""], states[i]))]) is { } standIn)
        {
            if (standIn.Loose.Count <= standIn.Places.Count)
            {
                return new ChosenPlan(ChosenPoolKind.StandIn, records, states, standIn.Set, standIn, null);
            }

            why = PoolRefusal.NoPlaceLeft(standIn.Set.Name, standIn.Loose.Count, standIn.Places.Count);
        }

        if (ids.Count == 1 && definitions[ids[0]] is { Tiling: { } t } set && t.Cols * t.Rows >= 2 && states.All(s => s.SetSheet is not null))
        {
            return new ChosenPlan(ChosenPoolKind.Set, records, states, set, null, null);
        }

        if (records.Count < 2)
        {
            return new ChosenPlan(ChosenPoolKind.Refused, records, states, null, null, PoolRefusal.OneSheet);
        }

        // A sheet's distance and caliber are its marking's, or where the marking has none, its session's.
        var distances = states.Select((s, i) => s.ShotDistanceInches ?? records[i].DistanceInches).OfType<double>().ToList();
        if (distances.Count > 1 && distances.Max() - distances.Min() > DistanceTolerance)
        {
            return new ChosenPlan(ChosenPoolKind.Refused, records, states, null, null, PoolRefusal.DifferentDistances);
        }

        var calibres = states.Select((s, i) => s.Calibre?.DiameterInches ?? records[i].CalibreInches).OfType<double>().ToList();
        if (calibres.Count > 1 && calibres.Max() - calibres.Min() > CalibreTolerance)
        {
            return new ChosenPlan(ChosenPoolKind.Refused, records, states, null, null, PoolRefusal.DifferentCalibres);
        }

        return new ChosenPlan(ChosenPoolKind.Mixed, records, states, null, null, PoolRefusal.Mixed(records.Count, why));
    }

    /// <summary>A mixed pool's marking: each sheet in the order ticked, about its own bulls, with its own scale.</summary>
    public static MarkingState MixedComposite(ChosenPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return SetPool.CompositeOfPictures(plan.States);
    }
}

/// <summary>What Pool the chosen says when it will not pool the sessions ticked (error report 57), or how it pooled them.</summary>
public static class PoolRefusal
{
    /// <summary>Said when nothing is ticked.</summary>
    public const string NothingTicked = "Tick the sheets to pool, then Pool the chosen.";

    /// <summary>One sheet ticked that is not a sheet of a set: there is nothing to pool it with.</summary>
    public const string OneSheet = "Only one sheet is ticked, and it is not a sheet of a set, so there is nothing to pool it with. Tick two or more, then Pool the chosen.";

    /// <summary>Sheets shot at different distances.</summary>
    public const string DifferentDistances = "These sheets were shot at different distances, so their groups in inches do not pool. Tick sheets shot at one distance, or use Compare to set them side by side as angles.";

    /// <summary>Sheets shot with different calibers.</summary>
    public const string DifferentCalibres = "These sheets were shot with different calibers, so the holes do not measure alike and the groups do not pool. Tick sheets of one caliber, or use Compare to set them side by side.";

    /// <summary>How a mixed pool was pooled, with why the set's numbering was not used where loose sheets could not all stand in.</summary>
    public static string Mixed(int sheets, string? why = null) =>
        (why is null ? "" : why + " ")
        + string.Create(CultureInfo.InvariantCulture, $"The {sheets} sheets are pooled as separate sheets, in the order ticked: each shot is measured about its own bull, with its own sheet's scale, and shot 2.5 is bull 5 on the second sheet.");

    /// <summary>Entry 512 section 3: more loose sheets ticked than the set has sheets missing from those ticked.</summary>
    public static string NoPlaceLeft(string set, int loose, int places) => places == 0
        ? $"Every sheet of {set} is ticked already, so the loose {(loose == 1 ? "sheet cannot stand in for one" : "sheets cannot stand in for any")}."
        : string.Create(CultureInfo.InvariantCulture, $"{loose} loose sheets are ticked, but only {places} {(places == 1 ? "sheet" : "sheets")} of {set} {(places == 1 ? "is" : "are")} missing from those ticked, so they cannot all stand in.");

    /// <summary>The question asked where a loose sheet could stand in for several of a set's sheets.</summary>
    public const string WhichSheetTitle = "Which sheet of the set does this one replace?";

    /// <summary>Why the question is asked.</summary>
    public static string WhichSheetWhy(string loose, string set) =>
        $"{loose} has the same bulls as every sheet of {set}, so it can count as any sheet missing from those ticked. Choose the one it replaces; each shot is still measured about its own bull.";

    /// <summary>One answer to the question.</summary>
    public static string SheetChoice(int place) => string.Create(CultureInfo.InvariantCulture, $"Sheet {place + 1}");

    /// <summary>Said once a loose sheet has its place.</summary>
    public static string CountsAs(string loose, int place) => string.Create(CultureInfo.InvariantCulture, $"{loose} counts as sheet {place + 1} of the set.");
}
