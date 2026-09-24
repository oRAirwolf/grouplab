using System.Reflection;
using GroupLab.Cli.Bench;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Bench;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 117 section 3a: "a hand-written list of cases rots the moment somebody adds a feature. So the bench's
/// coverage is checked, in the manner of the README's own tests: a test enumerates the things that can be measured and fails when one has no
/// bench case."
/// <para>
/// A thing that can be measured is a class in GroupLab.Core that does work: a static class of methods, or a class with public methods that is
/// not a record. Records are data and are measured through whatever computes them. Everything else is covered by a bench case naming it, or
/// sits in <see cref="NotMeasured"/> with a reason in one line, which is section 3b's rule brought up a level: exclusion by name, never by
/// silence.
/// </para>
/// </summary>
public class BenchCoverageTests
{
    /// <summary>The namespaces the benchmark is answerable for: everything GroupLab computes.</summary>
    private static readonly string[] Namespaces =
    [
        "GroupLab.Core.Analysis",
        "GroupLab.Core.Ballistics",
        "GroupLab.Core.Gltd.Binary",
        "GroupLab.Core.Gltd.Derivation",
        "GroupLab.Core.Gltd.Json",
        "GroupLab.Core.Gltd.Validation",
        "GroupLab.Core.Marking",
        "GroupLab.Core.Printing",
        "GroupLab.Core.Records",
        "GroupLab.Core.Rendering",
        "GroupLab.Core.Rendering.Pdf",
        "GroupLab.Core.Reporting",
        "GroupLab.Core.Statistics",
        "GroupLab.Core.Trace",
    ];

    /// <summary>
    /// What has no bench case on purpose, each with its reason. A name here is a decision; a name missing from both here and the bench is a
    /// gap, and the test says so.
    /// </summary>
    private static readonly Dictionary<string, string> NotMeasured = new(StringComparer.Ordinal)
    {
        ["CalibreList"] = "A generated document, written by grouplab calibres at release time and not by anything a person waits for.",
        ["AimedBulls"] = "It sorts a sheet's bulls into rows and builds a dictionary from them. The matching it feeds is measured; this is the sentence before it.",
        ["CalibreGuessList"] = "Twenty-two numbers and the nearest one to a reading. There is nothing in it whose speed or accuracy a figure could report.",
        ["CartridgeTable"] = "Forty names and fourteen diameters read once from an embedded file, and a list of strings for a box somebody is typing in. Entry 163.",
        ["TargetMaterial"] = "Two short lists of words and a check that a value is one of them. Entry 162.",
        ["CanonicalJsonWriter"] = "Writing a definition back out happens in the editor's save, which is measured as a control.",
        ["MarkingFile"] = "Measured as part of saving and reopening a session, which is what writes and reads it.",
        ["ShotLabels"] = "A few dozen string comparisons inside the analysis, below the resolution of any figure here.",
        ["ChangeWords"] = "The undo tooltip's words, read from two markings once per edit, below the resolution of any figure here.",
        ["ShotCsv"] = "Reading or writing one CSV of shot coordinates when a person asks, a few hundred rows at most.",
        ["Glossary"] = "One small list read once, and a word search over a label or a line when it is shown.",
        ["Snapping"] = "It runs under a person's finger on the marking canvas, so it is measured as a control and not here.",
        ["ViewRotation"] = "The same: it is what the rotate buttons do, and those are measured as controls.",
        ["PointInches"] = "A point on the page, carried by the stage records that are already timed.",
        ["AssignmentCertainties"] = "It reads the review queue that is already in memory and counts two kinds of item. There is nothing in it to measure that the analysis it sits beside does not already dominate.",
        ["StatedResolutionScale"] = "Two comparisons against a number the image file already carries. It does not read the image.",
        ["FigureExplanations"] = "A fixed list of sentences looked up by name. There is nothing in it that takes time.",
        ["FourUnits"] = "Four divisions and a rounding, on one number the analysis has already worked out.",
        ["MeanRadiusScale"] = "One division to put a figure on a scale, and a sentence chosen by the shot count.",
        ["CalibreConfirmation"] = "A median over the holes already in memory, and a sentence built from it. It runs when a person opens the calibre field.",
        ["EquipmentForm"] = "It reads a few dozen fields off at most a few hundred records to offer earlier values back as a person types, which is a person's typing speed and not a measurement.",
        ["ShotEditor"] = "It builds the buttons of a popover from the state, looping over the bulls once, and converts an arrow key press through three calls to the scale. It runs under a person's finger like the canvas controls beside it.",
    };

    [Fact]
    public void EverythingThatCanBeMeasuredHasABenchCase()
    {
        using var material = BenchMaterial.Prepare(Repo.Root);
        var covered = BenchSuite.All(material).SelectMany(c => c.Covers).ToHashSet(StringComparer.Ordinal);

        var missing = Measurable()
            .Where(t => !covered.Contains(t.Name) && !NotMeasured.ContainsKey(t.Name))
            .Select(t => t.Namespace + "." + t.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} things can be measured and have no bench case. Give each one a case in BenchSuite, naming it in Covers, or a "
            + $"line in NotMeasured saying why not: {string.Join(", ", missing)}");
    }

    /// <summary>A name in the exclusion list that no longer exists is a stale excuse, and is removed rather than left to look like coverage.</summary>
    [Fact]
    public void NothingIsExcludedThatIsNotThere()
    {
        var names = Measurable().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var stale = NotMeasured.Keys.Where(k => !names.Contains(k)).Order(StringComparer.Ordinal).ToList();
        Assert.True(stale.Count == 0, $"these are excluded from the benchmark and no longer exist: {string.Join(", ", stale)}");
    }

    /// <summary>Every case names an area the record prints, so nothing lands in a section nobody reads.</summary>
    [Fact]
    public void EveryCaseBelongsToAnAreaAndSaysWhatItMeasures()
    {
        using var material = BenchMaterial.Prepare(Repo.Root);
        var cases = BenchSuite.All(material);
        Assert.NotEmpty(cases);
        Assert.All(cases, c =>
        {
            Assert.Contains(c.Area, BenchSuite.Areas);
            Assert.False(string.IsNullOrWhiteSpace(c.What), $"{c.Name} does not say what it measures");
            Assert.NotEmpty(c.Covers);
        });

        // Two cases of the same name would be two rows of the same name in the record, which cannot be read as a difference.
        Assert.Equal(cases.Count, cases.Select(c => c.Area + ": " + c.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(BenchSuite.Exclusions, e => Assert.False(string.IsNullOrWhiteSpace(e.Why), $"{e.Name} is excluded with no reason"));
    }

    private static IEnumerable<Type> Measurable() =>
        typeof(GroupAnalysis).Assembly.GetExportedTypes()
            .Where(t => t.Namespace is { } n && Namespaces.Contains(n, StringComparer.Ordinal))
            .Where(DoesWork);

    /// <summary>
    /// A type that does work rather than holds data: a static class, or a class with a public method of its own that is not a record's
    /// generated members. Enumerations, interfaces, exceptions and records are data or shape, and are measured through what computes them.
    /// </summary>
    private static bool DoesWork(Type type)
    {
        if (type.IsEnum || type.IsInterface || typeof(Exception).IsAssignableFrom(type) || IsRecord(type))
        {
            return false;
        }

        if (type is { IsAbstract: true, IsSealed: true })
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Length > 0;
        }

        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(m => !m.IsSpecialName && m.DeclaringType == type);
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;
}
