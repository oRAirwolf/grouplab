using GroupLab.Cli;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Reporting;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 264: the donor pack is built from the library, not copied, so a redrawn sheet changes the pack. Each PDF the
/// site offers is exactly what <c>grouplab donor-pack</c> makes from today's library; the site's publish runs this first, so a pack that no
/// longer matches the library is never published.
/// </summary>
public class DonorPackTests
{
    [Fact]
    public void EveryDonorSheetIsWhatTheLibraryMakesToday()
    {
        string repository = Repo.PathTo();
        var stale = new List<string>();
        foreach (string file in DonorPackVerb.Sheets(repository))
        {
            string path = Path.Combine(repository, "website", "donor", file + ".pdf");
            if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(DonorPackVerb.Pack(repository, file)))
            {
                stale.Add(file);
            }
        }

        Assert.True(stale.Count == 0, "the donor pack does not match the library; run grouplab donor-pack and commit: " + string.Join(", ", stale));
    }
}
