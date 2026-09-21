using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 125 section 1: `v0.2.0-nightly.12` was published with its version and its commit stamped in and its train
/// missing, so it called itself a development build, would never have offered an update and would never have installed one.
/// <para>
/// The cause was an initialisation order in the application, fixed there, and the packaged build is checked before anything is published.
/// This holds the rule underneath both: a version is not a train, and a build that names no train is a development build however it is
/// numbered. Without that rule the check above it would have nothing to mean.
/// </para>
/// </summary>
public class BuildTrainTests
{
    /// <summary>The build that was published without its train, which is what this is all about.</summary>
    private const string ThePublishedVersion = "0.2.0-nightly.12+862aab2fb323c47c9d513f0c6058f8777a8b0699";

    [Fact]
    public void AVersionWithoutATrainIsADevelopmentBuildHoweverItIsNumbered()
    {
        Assert.Equal(UpdateTrain.Nightly, BuildIdentity.Read(ThePublishedVersion, "nightly").Train);
        Assert.Equal(UpdateTrain.Development, BuildIdentity.Read(ThePublishedVersion, null).Train);
        Assert.Equal(UpdateTrain.Development, BuildIdentity.Read(ThePublishedVersion, "").Train);
    }

    [Fact]
    public void ADevelopmentBuildOffersNothing()
    {
        // Which is why the published nightly could never have updated itself, whatever was on the train.
        Assert.True(BuildIdentity.Read(ThePublishedVersion, null).IsDevelopment);
        Assert.False(BuildIdentity.Read(ThePublishedVersion, "nightly").IsDevelopment);
        Assert.Contains("development build", BuildIdentity.Read(ThePublishedVersion, null).Line, StringComparison.Ordinal);
        Assert.Contains("nightly build", BuildIdentity.Read(ThePublishedVersion, "nightly").Line, StringComparison.Ordinal);
    }

    /// <summary>A train the version disagrees with is not taken on trust: only the workflow stamps one, and it stamps the matching pair.</summary>
    [Fact]
    public void ATrainTheVersionDisagreesWithIsRefused()
    {
        Assert.Equal(UpdateTrain.Development, BuildIdentity.Read("0.2.0-nightly.12+abc", "release").Train);
        Assert.Equal(UpdateTrain.Development, BuildIdentity.Read("0.2.0+abc", "nightly").Train);
        Assert.Equal(UpdateTrain.Release, BuildIdentity.Read("0.2.0+abc", "release").Train);
    }
}
