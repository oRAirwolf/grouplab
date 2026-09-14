using System.Text.Json.Nodes;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd.Validation;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Validation;

/// <summary>One failing definition per validation rule of TARGET-SCHEMA.md section 10.</summary>
public class ValidatorTests
{
    private static readonly Lazy<IReadOnlyList<BuiltInTarget>> Library = new(() => LibraryBuilder.Build(Repo.PathTo("tools", "layout", "layouts.json")));

    [Fact]
    public void Section4ExampleHasNoErrorsButItsSightersOutsideTheLattice()
    {
        // Section 4 is GL-CF25-LTR as printed for Phase 0, frozen as GL-YCSK-DZZ1-R0VJ-4T5Y. Its three sighters sit outside
        // the marker lattice, which test 26f made an error in the geometry change that superseded it (NOTES-FROM-PLANNING.md
        // entry 13); the example is kept as printed, so that is the one finding it carries.
        var errors = Validate(Spec.Section4Node()).Where(d => d.Severity == Severity.Error).ToList();

        Assert.Equal(["/bulls/25", "/bulls/26", "/bulls/27"], errors.Select(d => d.Path));
        Assert.All(errors, d => Assert.Equal("26f", d.Test));
    }

    [Fact]
    public void Test12UnresolvedReferencesAreErrors()
    {
        var doc = Spec.Section4Node();
        doc["ringSets"]![0]!["discs"]![1]!["ink"] = "white";
        doc["bulls"]![3]!["ringSet"] = "big";

        var diagnostics = Validate(doc);

        AssertFinding(diagnostics, Severity.Error, "12", "/ringSets/0/discs/1/ink");
        AssertFinding(diagnostics, Severity.Error, "12", "/bulls/3/ringSet");
    }

    [Fact]
    public void Test12FiducialInkMustHaveTheFiducialRole()
    {
        var doc = Spec.Section4Node();
        doc["fiducials"]!["ink"] = "black";

        AssertFinding(Validate(doc), Severity.Error, "12", "/fiducials/ink");
    }

    [Fact]
    public void Test13GeometryOutsideThePageIsAnError()
    {
        var doc = Spec.Section4Node();
        doc["page"]!["width"] = 1950;

        AssertFinding(Validate(doc), Severity.Error, "13", "/bulls/4");
    }

    [Fact]
    public void Test14OverlappingElementsAreErrors()
    {
        var doc = Spec.Section4Node();
        doc["ringSets"]![0]!["discs"]![0]!["diameter"] = 400;

        AssertFinding(Validate(doc), Severity.Error, "14", "/bulls/1");
    }

    [Fact]
    public void Test15MarkerNearABullIsAnError()
    {
        var doc = Spec.Section4Node();
        doc["fiducials"]!["scheme"] = "explicit";
        // 151 dmm below bull 0: inside its 127 dmm radius plus the 30 dmm half footprint.
        doc["fiducials"]!["markers"] = new JsonArray(new JsonObject { ["id"] = 0, ["x"] = 320, ["y"] = 690 });

        AssertFinding(Validate(doc), Severity.Error, "15", "/bulls/0");
    }

    [Fact]
    public void Test16StoredMarkersThatDisagreeWithTheRuleAreAnError()
    {
        var doc = BuiltIn("GL-CF25-LTR");
        doc["fiducials"]!["markers"]![5]!["x"] = 511;

        AssertFinding(Validate(doc), Severity.Error, "16", "/fiducials/markers");
    }

    [Fact]
    public void Test17NonStandardNamedPageIsOnlyAWarning()
    {
        var doc = Spec.Section4Node();
        doc["page"]!["height"] = 2800;

        AssertFinding(Validate(doc), Severity.Warning, "17", "/page");
    }

    [Fact]
    public void Test18SmallQrModuleWarns()
    {
        var doc = Spec.Section4Node();
        doc["codes"]!["moduleSize"] = 3;

        AssertFinding(Validate(doc), Severity.Warning, "18", "/codes/moduleSize");
    }

    [Fact]
    public void Test19OddPitchUnderADerivedSchemeIsAnError()
    {
        var doc = Spec.Section4Node();
        doc["cells"]!["grid"]!["pitchX"] = 381;

        AssertFinding(Validate(doc), Severity.Error, "19", "/cells/grid");
    }

    [Fact]
    public void Test19HalfPitchSchemeNeedsAPitchDivisibleByFour()
    {
        var doc = Spec.Section4Node();
        doc["fiducials"]!["scheme"] = "grid-boundary-half-1";
        foreach (var bull in doc["bulls"]!.AsArray())
        {
            bull!["x"] = (int)bull["x"]! + (((int)bull["x"]! - 320) / 380 * 2);
        }

        doc["cells"]!["grid"]!["pitchX"] = 382;
        doc["cells"]!["grid"]!["originX"] = 129;

        AssertFinding(Validate(doc), Severity.Error, "19", "/cells/grid");
    }

    [Fact]
    public void Test20DiscsThatDoNotStrictlyDecreaseAreAnError()
    {
        var doc = Spec.Section4Node();
        doc["ringSets"]![0]!["discs"]![2]!["diameter"] = 238;

        AssertFinding(Validate(doc), Severity.Error, "20", "/ringSets/0/discs/2/diameter");
    }

    [Fact]
    public void Test21SecondPaperInkIsAnError()
    {
        var doc = Spec.Section4Node();
        doc["inks"]!.AsArray().Add(new JsonObject { ["key"] = "stock", ["srgb"] = "#F5F0E1", ["role"] = "paper" });

        AssertFinding(Validate(doc), Severity.Error, "21", "/inks/5");
    }

    [Fact]
    public void Test22StarvedDerivationIsAnError()
    {
        var doc = BuiltIn("GL-LR300-T");
        doc["fiducials"]!["scheme"] = "grid-boundary-1";
        doc["fiducials"]!.AsObject().Remove("markers");

        AssertFinding(Validate(doc), Severity.Error, "22", "/fiducials");
    }

    [Fact]
    public void Test23UndeclaredSighterGapWarns()
    {
        var doc = Spec.Section4Node();
        doc["cells"]!.AsObject().Remove("sighterGap");
        foreach (var bull in doc["bulls"]!.AsArray().Where(b => !(bool)b!["scoring"]!))
        {
            bull!["y"] = 2525;
        }

        AssertFinding(Validate(doc), Severity.Warning, "23", "/cells/sighterGap");
    }

    [Fact]
    public void Test23GapShortenedToBracketTheSightersDoesNotWarn()
    {
        // GL-CF25-LTR as printed drops the marker row below its sighters by 1 dmm; a 454 dmm gap keeps it (PHASE0-RESULTS.md 4.4).
        var doc = ShortenSighterGap(Frozen("GL-YCSK-DZZ1-R0VJ-4T5Y"), 2);

        Assert.DoesNotContain(Validate(doc), d => d.Test is "23" or "26f");
    }

    [Fact]
    public void Test23GapShortenedWhereTheLatticeAlreadyBracketsWarns()
    {
        var doc = ShortenSighterGap(BuiltIn("GL-CF25-A4"), 2);

        AssertFinding(Validate(doc), Severity.Warning, "23", "/cells/sighterGap");
    }

    [Fact]
    public void Test26fBullOutsideTheFiducialLatticeIsAnError()
    {
        // GL-CF25-LTR as printed for Phase 0: its sighters are 266 dmm below the lattice (PHASE0-RESULTS.md 4.4).
        var doc = Frozen("GL-YCSK-DZZ1-R0VJ-4T5Y");
        var diagnostics = Validate(doc);
        var bulls = doc["bulls"]!.AsArray();

        for (int i = 0; i < bulls.Count; i++)
        {
            if ((bool)bulls[i]!["scoring"]!)
            {
                Assert.DoesNotContain(diagnostics, d => d.Test == "26f" && d.Path == $"/bulls/{i}");
            }
            else
            {
                AssertFinding(diagnostics, Severity.Error, "26f", $"/bulls/{i}");
            }
        }
    }

    [Fact]
    public void Test26fBullOnTheLatticeEdgeConforms()
    {
        // The 300 yard tile's outermost markers share coordinates with its outermost bulls.
        Assert.DoesNotContain(Validate(BuiltIn("GL-LR300-T")), d => d.Test == "26f");
    }

    [Fact]
    public void Test24ElementsCrossingATileBoundaryAreErrors()
    {
        var doc = Spec.Section4Node();
        doc["tiling"] = new JsonObject { ["cols"] = 2, ["rows"] = 1, ["sheetWidth"] = 1800, ["sheetHeight"] = 2794, ["overlap"] = 0 };

        var diagnostics = Validate(doc);

        AssertFinding(diagnostics, Severity.Error, "24", "/tiling");
        AssertFinding(diagnostics, Severity.Error, "24", "/bulls/4");
    }

    [Fact]
    public void Test24aStoredCellGridThatDiffersFromTheDerivationIsAnError()
    {
        var doc = Spec.Section4Node();
        doc["cells"]!["grid"]!["originY"] = 350;

        AssertFinding(Validate(doc), Severity.Error, "24a", "/cells/grid");
    }

    [Fact]
    public void Test24UndrawnCellsPastTheSheetEdgeRaiseNothing()
    {
        // GL-LR300-T's derived lattice overhangs its sheet by 127 dmm top and bottom; a region is clipped by the paper.
        var doc = BuiltIn("GL-LR300-T");
        doc["cells"] = new JsonObject { ["mode"] = "grid", ["drawn"] = false };

        Assert.Empty(Validate(doc));
    }

    [Fact]
    public void Test24DrawnCellsPastTheTileBoundaryAreAnError()
    {
        var doc = BuiltIn("GL-LR300-T");
        doc["cells"] = new JsonObject { ["mode"] = "grid", ["drawn"] = true, ["ink"] = "black", ["stroke"] = 3 };

        AssertFinding(Validate(doc), Severity.Error, "24", "/cells/drawn");
    }

    [Fact]
    public void Test25ReserveTallerThanTheBlockIsAnError()
    {
        var doc = BuiltIn("GL-CF25-LTR-D");
        doc["dataBlock"]!["reserve"] = 320;

        AssertFinding(Validate(doc), Severity.Error, "25", "/dataBlock/reserve");
    }

    [Fact]
    public void Test25DataBlockOverABullIsAnError()
    {
        var doc = BuiltIn("GL-CF25-LTR-D");
        doc["dataBlock"]!["y"] = 2100;

        AssertFinding(Validate(doc), Severity.Error, "25", "/dataBlock");
    }

    [Fact]
    public void Test26GridFieldWithoutRoomForMarkersIsAnError()
    {
        var doc = BuiltIn("GL-ZERO-MIL-100M");
        doc["grids"]![0]!["half"] = 890;

        AssertFinding(Validate(doc), Severity.Error, "26", "/grids/0");
    }

    [Fact]
    public void Test26aCodeOverTheDataBlockIsAnError()
    {
        // corners-1 lifts the bottom codes above a block along the foot of the page, so only a block placed
        // elsewhere can reach one: this one sits across the bottom-left code.
        var doc = Spec.Section4Node();
        doc["dataBlock"] = new JsonObject
        {
            ["x"] = 120, ["y"] = 2500, ["width"] = 200, ["height"] = 100, ["layout"] = "fields-3x3-1",
            ["fieldSet"] = "standard-9", ["reserve"] = 100, ["ink"] = "black",
        };

        AssertFinding(Validate(doc), Severity.Error, "26a", "/dataBlock");
    }

    [Fact]
    public void Test26dStoredCodePositionsThatDisagreeWithTheRuleAreAnError()
    {
        var doc = Spec.Section4Node();
        doc["codes"]!["positions"]![3]!["y"] = 2540;

        AssertFinding(Validate(doc), Severity.Error, "26d", "/codes/positions");
    }

    [Fact]
    public void Test26dCodesWithoutPositionsAreAnError()
    {
        var doc = Spec.Section4Node();
        doc["codes"]!["positions"] = new JsonArray();

        AssertFinding(Validate(doc), Severity.Error, "26d", "/codes/positions");
    }

    private static JsonObject BuiltIn(string name) =>
        JsonNode.Parse(CanonicalJsonWriter.Write(Library.Value.Single(t => t.Name == name).Definition))!.AsObject();

    /// <summary>A definition a sample set was printed from, under <c>targets/frozen/phase0/</c> (NOTES-FROM-PLANNING.md entry 11).</summary>
    private static JsonObject Frozen(string id) =>
        JsonNode.Parse(File.ReadAllBytes(Repo.PathTo("targets", "frozen", "phase0", $"{id}.gltd.json")))!.AsObject();

    private static JsonObject ShortenSighterGap(JsonObject doc, int dmm)
    {
        doc["cells"]?.AsObject().Remove("sighterGap");
        doc["fiducials"]!.AsObject().Remove("markers");
        foreach (var bull in doc["bulls"]!.AsArray().Where(b => !(bool)b!["scoring"]!))
        {
            bull!["y"] = (int)bull["y"]! - dmm;
        }

        return doc;
    }

    private static IReadOnlyList<Diagnostic> Validate(JsonObject doc)
    {
        var read = GltdJsonReader.Read(Spec.Utf8(doc));
        Assert.Empty(read.Diagnostics);
        return GltdValidator.Validate(read.Definition!);
    }

    private static void AssertFinding(IReadOnlyList<Diagnostic> diagnostics, Severity severity, string test, string path) =>
        Assert.True(diagnostics.Any(d => d.Severity == severity && d.Test == test && d.Path == path),
            $"Expected a {severity} for test {test} at {path}; got:\n{string.Join("\n", diagnostics)}");
}
