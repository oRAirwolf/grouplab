using GroupLab.Cli.Spike;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Library;

/// <summary>
/// The definitions a sample set was printed from, frozen under <c>targets/frozen/</c> (NOTES-FROM-PLANNING.md entry 11).
/// They are inputs to a measurement and never edited: a failure here after a schema or validator change is a finding to
/// report, not a fixture to repair.
/// </summary>
public class FrozenDefinitionTests
{
    public static TheoryData<string> Phase0() =>
        [.. Directory.EnumerateFiles(Repo.PathTo("targets", "frozen", "phase0"), "*.gltd.json").Select(f => Path.GetFileName(f)).Order()];

    [Fact]
    public void EveryPhase0SampleResolvesToAFrozenDefinition()
    {
        foreach (string file in SampleSet.All.Select(s => s.Definition).Distinct())
        {
            Assert.True(File.Exists(Repo.PathTo("targets", "frozen", "phase0", file)), $"{file} is not frozen.");
        }
    }

    [Theory]
    [MemberData(nameof(Phase0))]
    public void FrozenDefinitionLoadsAndCarriesTheIdentifierItIsNamedBy(string file)
    {
        byte[] bytes = File.ReadAllBytes(Repo.PathTo("targets", "frozen", "phase0", file));
        var read = GltdJsonReader.Read(bytes);
        Assert.Empty(read.Diagnostics);

        var encoded = GltdBinary.Encode(read.Definition!);
        string named = file[..^".gltd.json".Length];

        Assert.Empty(encoded.Diagnostics);
        Assert.Equal(named, encoded.Encoding!.DefinitionId);
        Assert.Equal(named, read.Definition!.Id);
        Assert.Equal(CanonicalJsonWriter.Write(read.Definition), bytes);
    }

    /// <summary>
    /// Test 26f is exempt: GL-YCSK-DZZ1-R0VJ-4T5Y, GL-CF25-LTR as printed, has its sighters outside the marker lattice by
    /// construction, which is the defect the geometry change of PHASE0-RESULTS.md section 4.4 fixes.
    /// </summary>
    [Theory]
    [MemberData(nameof(Phase0))]
    public void FrozenDefinitionValidates(string file)
    {
        var definition = GltdJsonReader.Read(File.ReadAllBytes(Repo.PathTo("targets", "frozen", "phase0", file))).Definition!;

        var errors = GltdValidator.Validate(definition).Where(d => d.Severity == Severity.Error && d.Test != "26f");

        Assert.Empty(errors);
    }
}
