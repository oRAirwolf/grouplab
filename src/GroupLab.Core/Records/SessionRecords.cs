using System.Globalization;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Records;

/// <summary>
/// A saved session as the desktop and the phone both make it, NOTES-FROM-PLANNING.md entry 112 section 1 and entry 219 item A4: the marking
/// with every edit and exclusion, the figures as computed, the definition it was analyzed against, a proof image, and the original's path
/// and SHA-256. One place, so a session saved on the phone opens on the desktop exactly as one saved there does.
/// </summary>
public static class SessionRecords
{
    /// <summary>The shots a session counts: every shot, less those on a sighter unless sighters are analyzed.</summary>
    public static int CountedShots(MarkingState state, bool analyseSighters)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Shots.Count(s => s.IsShot && (analyseSighters || !GroupAnalysis.OnSighter(state, s)));
    }

    /// <summary>
    /// The record for this marking. A second save of the same session passes the first as <paramref name="existing"/>, which keeps its id and
    /// the day it was first saved.
    /// </summary>
    public static SessionRecord Build(MarkingState state, TargetDefinition? definition, UnitSettings units, bool analyseSighters,
        SessionRecord? existing, string? imageSha256, byte[]? proof, string? proofType, DateTime utcNow, DateTime localNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        var figures = GroupAnalysis.Analyse(state).AllShots?.MeanRadius;
        return new SessionRecord(
            existing?.Id ?? 0,
            existing?.CreatedUtc ?? utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            existing?.ShotDate ?? localNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            definition?.Name ?? (state.ImagePath is { } named ? Path.GetFileName(named) : "Marked by hand"),
            definition is { } d ? GltdBinary.Encode(d).Encoding?.DefinitionId : null,
            definition is { } defined ? System.Text.Encoding.UTF8.GetString(CanonicalJsonWriter.Write(defined)) : null,
            state.ShotDistanceInches,
            state.Rifle?.Name,
            state.Barrel,
            state.Load,
            state.Calibre?.DiameterInches,
            MarkingFile.Write(state, units),
            CountedShots(state, analyseSighters),
            figures?.Value,
            figures?.Lower,
            figures?.Upper,
            state.ImagePath,
            imageSha256,
            proof,
            proofType);
    }
}
