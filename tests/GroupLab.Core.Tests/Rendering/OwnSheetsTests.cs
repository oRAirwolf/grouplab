using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 112 section 3: the person's own sheets, kept as canonical GLTD-J in the data folder. Names stay unique, a
/// rename keeps the file and the printed codes, and a duplicate is a new sheet of their own.
/// </summary>
public sealed class OwnSheetsTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"grouplab-own-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void SheetsSaveRenameDuplicateAndDeleteWithoutTouchingTheirCodes()
    {
        var builtIn = GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR.gltd.json")).Definition!;
        var own = new OwnSheets(folder);
        Assert.Empty(own.List());

        var first = own.Save(builtIn with { Name = "My sheet" });
        var second = own.Save(builtIn with { Name = "My sheet" });
        Assert.Equal("My sheet", first.Definition.Name);
        Assert.Equal("My sheet 2", second.Definition.Name);
        Assert.NotEqual(first.File, second.File);
        Assert.All(own.List(), s => Assert.Equal(OwnSheets.Family, s.Family));
        Assert.Equal(CanonicalJsonWriter.Write(first.Definition), File.ReadAllBytes(Path.Combine(folder, first.File)));

        // The name is not in the printed codes, so a rename leaves the definition identifier as it was.
        var renamed = own.Rename(first, "Load ladder");
        Assert.Equal(first.File, renamed.File);
        Assert.Equal(GltdBinary.Encode(builtIn).Encoding!.DefinitionId, GltdBinary.Encode(renamed.Definition).Encoding!.DefinitionId);
        Assert.Throws<ArgumentException>(() => own.Rename(second, "load ladder"));
        Assert.Throws<ArgumentException>(() => own.Rename(second, "  "));

        var copy = own.Duplicate(renamed);
        Assert.Equal("Load ladder copy", copy.Definition.Name);
        Assert.Equal(["Load ladder", "Load ladder copy", "My sheet 2"], own.List().Select(s => s.Definition.Name));

        own.Delete(renamed);
        Assert.Equal(["Load ladder copy", "My sheet 2"], own.List().Select(s => s.Definition.Name));
    }
}
