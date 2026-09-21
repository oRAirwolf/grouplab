using GroupLab.Cli;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 125 section 1: `v0.2.0-nightly.12` was published with its version and its commit stamped in and its train
/// missing, so it called itself a development build, would never have offered an update and would never have installed one.
/// <para>
/// The cause was an initialisation order: <c>AppInfo.Build</c> was a static field initialiser written above <c>AppInfo.Train</c>, and C# runs
/// those in the order they appear, so it read the train while it was still null. It is now worked out on first use instead, which no
/// declaration order can break.
/// </para>
/// <para>
/// That cause is fixed in one place, but the claim can only really be checked on a packaged build, which is what <c>build-stamp</c> is for and
/// what the packaging workflow runs before it publishes anything. These tests hold the checker itself: a checker that passed everything would
/// be worse than none, because it would be believed.
/// </para>
/// </summary>
public class BuildStampTests
{
    /// <summary>The application assembly this test run was built against, which carries whatever train this build was made with.</summary>
    private static string TheApplication => typeof(MainWindow).Assembly.Location;

    [Fact]
    public void ABuiltAssemblyCarriesATrainAndAVersion()
    {
        var (train, version) = BuildStampVerb.ReadStamp(TheApplication);

        // Directory.Build.props stamps every build, defaulting to development, so this is never absent however the build was made. An
        // assembly with no train at all is the fault entry 125 is about.
        Assert.False(string.IsNullOrWhiteSpace(train), "the application assembly carries no GroupLabTrain, so a published build of it would call itself a development build");
        Assert.False(string.IsNullOrWhiteSpace(version));
    }

    [Fact]
    public void WhatIsStampedIsWhatTheApplicationSaysItIs()
    {
        var (train, version) = BuildStampVerb.ReadStamp(TheApplication);

        // The two paths to the same answer: read out of the file, and read by the application from its own assembly. They agreed even while
        // the defect was live, because both said development; what this holds is that they cannot drift apart as the reading changes.
        var fromTheFile = BuildIdentity.Read(version, train);
        Assert.Equal(fromTheFile.Train, GroupLab.App.Diagnostics.AppInfo.Build.Train);
        Assert.Equal(fromTheFile.Version.Number, GroupLab.App.Diagnostics.AppInfo.Build.Version.Number);
    }

    [Fact]
    public void ABuildStampedForTheWrongTrainIsRefusedAndSaysWhy()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        // A development build offered as a nightly is exactly what was published as v0.2.0-nightly.12.
        int code = BuildStampVerb.Run(TheApplication, "nightly", output, error);

        var (train, _) = BuildStampVerb.ReadStamp(TheApplication);
        if (string.Equals(train, "nightly", StringComparison.OrdinalIgnoreCase))
        {
            // This run was itself built for the nightly train, so there is nothing to refuse; the refusal is checked below with a train
            // nothing is ever built for.
            Assert.Equal(0, code);
        }
        else
        {
            Assert.Equal(1, code);
            Assert.Contains("calls itself a development build", error.ToString(), StringComparison.Ordinal);
            Assert.Contains("must not be published", error.ToString(), StringComparison.Ordinal);
        }

        var otherError = new StringWriter();
        Assert.Equal(1, BuildStampVerb.Run(TheApplication, "no-such-train", new StringWriter(), otherError));
        Assert.Contains("no-such-train", otherError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AFileThatIsNotAnAssemblyIsReportedRatherThanPassed()
    {
        var error = new StringWriter();
        Assert.Equal(2, BuildStampVerb.Run(Repository("README.md"), "nightly", new StringWriter(), error));
        Assert.Contains("not a .NET assembly", error.ToString(), StringComparison.Ordinal);

        var missing = new StringWriter();
        Assert.Equal(2, BuildStampVerb.Run(Repository("no-such-file.dll"), "nightly", new StringWriter(), missing));
        Assert.Contains("there is no assembly", missing.ToString(), StringComparison.Ordinal);
    }

    private static string Repository(string name, [System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", name));
}
