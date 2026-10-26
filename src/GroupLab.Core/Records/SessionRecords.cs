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
    /// Error report 40, 2026-10-25: whether a marking is the same sheet as one saved before from the same picture, by its holes. Entry 420
    /// section 3 took the same picture for the same sheet, and somebody who marks every sheet by hand on copies of one clean picture of the
    /// target lost each sheet to the next. It is the same sheet where at least half the shots of each are within
    /// <paramref name="tolerancePixels"/> of a shot of the other, paired one to one; the picture is the same, so the pixels are comparable.
    /// </summary>
    public static bool SameHoles(MarkingState now, MarkingState before, double tolerancePixels)
    {
        ArgumentNullException.ThrowIfNull(now);
        ArgumentNullException.ThrowIfNull(before);
        var here = now.Shots.Where(s => s.IsShot).Select(s => s.Image).ToList();
        var there = before.Shots.Where(s => s.IsShot).Select(s => s.Image).ToList();
        if (here.Count == 0 || there.Count == 0)
        {
            return here.Count == there.Count;
        }

        // The closest pairs first, each shot used once.
        var pairs = (from i in Enumerable.Range(0, here.Count)
                     from j in Enumerable.Range(0, there.Count)
                     let d = Math.Sqrt(Math.Pow(here[i].X - there[j].X, 2) + Math.Pow(here[i].Y - there[j].Y, 2))
                     where d <= tolerancePixels
                     orderby d
                     select (i, j)).ToList();
        var usedHere = new HashSet<int>();
        var usedThere = new HashSet<int>();
        foreach (var (i, j) in pairs)
        {
            if (!usedHere.Contains(i) && !usedThere.Contains(j))
            {
                usedHere.Add(i);
                usedThere.Add(j);
            }
        }

        int matched = usedHere.Count;
        return matched * 2 >= here.Count && matched * 2 >= there.Count;
    }

    /// <summary>
    /// The record for this marking. A second save of the same session passes the first as <paramref name="existing"/>, which keeps its id and
    /// the day it was first saved.
    /// </summary>
    public static SessionRecord Build(MarkingState state, TargetDefinition? definition, UnitSettings units, bool analyseSighters,
        SessionRecord? existing, string? imageSha256, byte[]? proof, string? proofType, DateTime utcNow, DateTime localNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        var figures = GroupAnalysis.Analyse(state).Counted?.MeanRadius; // entry 278 section 5c: without the shots left out
        return new SessionRecord(
            existing?.Id ?? 0,
            existing?.CreatedUtc ?? utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            existing?.ShotDate ?? localNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            (definition?.Name ?? (state.ImagePath is { } named ? Path.GetFileName(named) : "Marked by hand")) + (state.SheetLabel is { } label ? ", " + label : ""),
            definition is { } d ? GltdBinary.Encode(d).Encoding?.DefinitionId : null,
            definition is { } defined ? System.Text.Encoding.UTF8.GetString(CanonicalJsonWriter.Write(defined)) : null,
            state.ShotDistanceInches,
            state.Rifle?.Name,
            state.Barrel,
            state.Load,
            state.Calibre?.DiameterInches,
            MarkingFile.Write(state, units),
            // Entry 278 section 5c: the count beside the mean radius is the shots it was measured on, without any left out.
            CountedShots(state, analyseSighters) - state.Shots.Count(s => s.IsShot && s.Exclusion is not null && (analyseSighters || !GroupAnalysis.OnSighter(state, s))),
            figures?.Value,
            figures?.Lower,
            figures?.Upper,
            state.ImagePath,
            imageSha256,
            proof,
            proofType);
    }
}
