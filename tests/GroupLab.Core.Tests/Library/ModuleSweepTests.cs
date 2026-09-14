using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering.Markers;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Library;

/// <summary>The marker module sweep of FIDUCIAL-DECISION.md section 10, measurement 2, built by <see cref="ModuleSweep"/>.</summary>
public class ModuleSweepTests
{
    private static readonly Lazy<IReadOnlyList<SweepSheet>> Sheets = new(() => ModuleSweep.Build(
        Repo.PathTo("tools", "layout", "layouts.json"), Repo.PathTo("scans", "phase1", "module-sweep", "layouts.json")));

    [Fact]
    public void TheSweepPrintsTheFiveModulesMeasurementTwoNames()
    {
        // 0.3, 0.4, 0.5, 0.6 and 0.8 mm: eight modules across the printed square, two of quiet zone.
        Assert.Equal(Tag36h11.Modules, ModuleSweep.TagModules);
        Assert.Equal([3, 4, 5, 6, 8], Sheets.Value.Select(s => s.Definition.Fiducials!.MarkerSize / Tag36h11.Modules));
        Assert.All(Sheets.Value, s => Assert.Equal(s.Definition.Fiducials!.MarkerSize, Tag36h11.Modules * s.ModuleDmm));
        Assert.All(Sheets.Value, s => Assert.Equal(s.Definition.Fiducials!.QuietZone, ModuleSweep.QuietModules * s.ModuleDmm));
    }

    [Fact]
    public void EachLatticeMatchesTheLayoutTool()
    {
        Assert.All(Sheets.Value, s => Assert.Equal(s.LayoutMarkers, s.Definition.Fiducials!.Markers!.Select(m => new PointDmm(m.X, m.Y))));
    }

    [Fact]
    public void OnlyTheFiducialsDifferFromTheReferenceSheet()
    {
        var reference = GltdJsonReader.Read(File.ReadAllBytes(Repo.PathTo("targets", "GL-CF25-LTR.gltd.json"))).Definition!;
        Assert.All(Sheets.Value, s =>
        {
            Assert.Equal(reference.Bulls, s.Definition.Bulls);
            // Records compare a list member by reference, so the code positions are compared as a sequence.
            Assert.Equal(reference.Codes! with { Positions = null! }, s.Definition.Codes! with { Positions = null! });
            Assert.Equal(reference.Codes.Positions, s.Definition.Codes.Positions);
            Assert.Equal(reference.Page, s.Definition.Page);
        });
    }

    [Fact]
    public void TheHalfMillimetreSheetIsTheReferenceGeometry()
    {
        // Identifiers hash the body, which carries no name, so the 0.5 mm sheet is the Phase 0 sheet by identifier.
        var reference = GltdJsonReader.Read(File.ReadAllBytes(Repo.PathTo("targets", "GL-CF25-LTR.gltd.json"))).Definition!;
        Assert.Equal(reference.Id, Sheets.Value.Single(s => s.ModuleDmm == 5).Definition.Id);
        Assert.Equal(5, Sheets.Value.Select(s => s.Definition.Id).Distinct().Count());
    }

    [Fact]
    public void CommittedFilesAreCurrent()
    {
        foreach (var sheet in Sheets.Value)
        {
            string path = Repo.PathTo("scans", "phase1", "module-sweep", sheet.FileName);
            Assert.True(File.Exists(path), $"{path} is missing; run grouplab sweep module.");
            Assert.Equal(CanonicalJsonWriter.Write(sheet.Definition), File.ReadAllBytes(path));
        }
    }
}
