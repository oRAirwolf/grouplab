using System.Diagnostics;
using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests.Updates;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 119 section 1 and entry 121: versions are ordered, and the ordering decides what the updater offers. Entry
/// 121 exists because <c>v0.1.0</c> was published while the version in the build was still 0.1.0, which would have made every nightly sort
/// below something already public and had the updater offer a downgrade as an upgrade.
/// </summary>
public partial class VersionTests
{
    [Fact]
    public void VersionsSortAsSemVerSaysTheyDo()
    {
        // Entry 119 section 1.3's own example, written as a test because it is the rule the updater turns on.
        var order = new[] { "0.1.0-nightly.12", "0.1.0-beta.1", "0.1.0", "0.2.0-nightly.1" }.Select(v => SemanticVersion.Parse(v)!).ToList();
        Assert.All(order, v => Assert.NotNull(v));
        for (int i = 1; i < order.Count; i++)
        {
            Assert.True(UpdateOrder.Compare(order[i - 1], order[i]) < 0, $"{order[i - 1]} should sort below {order[i]}");
            Assert.True(UpdateOrder.Compare(order[i], order[i - 1]) > 0, $"{order[i]} should sort above {order[i - 1]}");
        }

        // Strict SemVer disagrees about beta against nightly, because it compares those words as text. UpdateOrder ranks the trains by how
        // steady they are instead, which is what entry 119 section 1.3 asks for and what section 4.5 needs. Question 30 records it.
        Assert.True(SemanticVersion.Parse("0.1.0-beta.1")! < SemanticVersion.Parse("0.1.0-nightly.12")!);
        Assert.True(UpdateOrder.Compare(SemanticVersion.Parse("0.1.0-nightly.12"), SemanticVersion.Parse("0.1.0-beta.1")) < 0);

        // A pre-release is older than its own release, and nightly builds are ordered by their number rather than as text.
        Assert.True(UpdateOrder.Compare(SemanticVersion.Parse("0.2.0-nightly.9"), SemanticVersion.Parse("0.2.0-nightly.10")) < 0);
        Assert.True(UpdateOrder.Compare(SemanticVersion.Parse("0.2.0-nightly.14"), SemanticVersion.Parse("0.2.0")) < 0);
        Assert.True(UpdateOrder.Compare(SemanticVersion.Parse("0.2.0"), SemanticVersion.Parse("0.2.1-nightly.1")) < 0);
        Assert.True(UpdateOrder.IsNewer(SemanticVersion.Parse("0.2.0-nightly.9"), SemanticVersion.Parse("0.2.0-nightly.10")));
        Assert.False(UpdateOrder.IsNewer(SemanticVersion.Parse("0.2.0"), SemanticVersion.Parse("0.2.0-nightly.10")));

        // Build metadata carries no ordering, so two builds of one version are neither newer nor older.
        Assert.Equal(0, SemanticVersion.Parse("0.2.0+abc")!.CompareTo(SemanticVersion.Parse("0.2.0+def")));
        Assert.Null(SemanticVersion.Parse("not a version"));
        Assert.Equal("0.2.0-nightly.14", SemanticVersion.Parse("v0.2.0-nightly.14+deadbee")!.Number);
    }

    [Fact]
    public void AVersionBelongsToTheTrainItsNameGives()
    {
        Assert.Equal(UpdateTrain.Nightly, UpdateTrains.Of(SemanticVersion.Parse("0.2.0-nightly.14")!));
        Assert.Equal(UpdateTrain.Beta, UpdateTrains.Of(SemanticVersion.Parse("0.2.0-beta.1")!));
        Assert.Equal(UpdateTrain.Release, UpdateTrains.Of(SemanticVersion.Parse("0.2.0")!));

        // A build that names no train is a development build, and so is one whose train disagrees with its version.
        Assert.True(BuildIdentity.Read("0.2.0+deadbeef", null).IsDevelopment);
        Assert.True(BuildIdentity.Read("0.2.0-nightly.3+deadbeef", "release").IsDevelopment);
        var nightly = BuildIdentity.Read("0.2.0-nightly.3+deadbeefcafe", "nightly");
        Assert.False(nightly.IsDevelopment);
        Assert.Equal(UpdateTrain.Nightly, nightly.Train);
        Assert.Equal("deadbee", nightly.Commit);
        Assert.Contains("nightly build", nightly.Line, StringComparison.Ordinal);
        Assert.Contains("development build", BuildIdentity.Read("0.2.0", null).Line, StringComparison.Ordinal);
    }

    [Fact]
    public void ALessSteadyTrainTakesFromASteadierOneAndNeverTheOtherWay()
    {
        // Entry 119 section 4.5.
        Assert.Equal([UpdateTrain.Nightly, UpdateTrain.Beta, UpdateTrain.Release], UpdateTrain.Nightly.Accepts());
        Assert.Equal([UpdateTrain.Beta, UpdateTrain.Release], UpdateTrain.Beta.Accepts());
        Assert.Equal([UpdateTrain.Release], UpdateTrain.Release.Accepts());
        Assert.Empty(UpdateTrain.Development.Accepts());

        // Only nightly has builds today, and the others say so rather than pretending.
        Assert.True(UpdateTrain.Nightly.IsAvailable());
        Assert.False(UpdateTrain.Beta.IsAvailable());
        Assert.False(UpdateTrain.Release.IsAvailable());
        Assert.Equal(3, UpdateTrains.Choosable.Count);
    }

    /// <summary>
    /// Entry 121 section 2.1: the version in the build must be above every version already published as a tag, or a nightly of it sorts
    /// below something public and the updater offers older code. This is the test that stops it happening twice.
    /// </summary>
    [Fact]
    public void TheBuildsVersionIsAboveEveryTagAlreadyPublished()
    {
        var version = SemanticVersion.Parse(Repo.Version());
        Assert.NotNull(version);
        Assert.Null(version!.PreRelease);

        foreach (string tag in Tags())
        {
            var tagged = SemanticVersion.Parse(tag);
            if (tagged is null || tagged.IsPreRelease)
            {
                continue;
            }

            Assert.True(version > tagged,
                $"Directory.Build.props says {version}, and {tag} is already published. A nightly of {version} would sort below it and the "
                + "updater would offer a downgrade. Raise the version, as entry 121 raised it to 0.2.0.");
        }
    }

    /// <summary>Every tag in this clone. A clone with no tags proves nothing here and says so by passing.</summary>
    private static IEnumerable<string> Tags()
    {
        using var git = Process.Start(new ProcessStartInfo("git", "tag --list v*")
        {
            WorkingDirectory = Repo.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        if (git is null)
        {
            yield break;
        }

        string output = git.StandardOutput.ReadToEnd();
        git.WaitForExit(10_000);
        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return line.Trim();
        }
    }

    [GeneratedRegex(@"<Version>(?<version>[^<]+)</Version>")]
    internal static partial Regex VersionElement();
}
