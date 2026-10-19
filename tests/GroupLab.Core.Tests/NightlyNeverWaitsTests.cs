using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 450: a flaky test or a stuck phone build should not hold back the other platforms' nightly. Section 1: a
/// failed test is run once more in the same job, and only the tests that failed; a flake is named, never hidden. Section 2: the desktop
/// publish no longer waits for the Android build or the server's command line, which join the release when they finish.
/// </summary>
public partial class NightlyNeverWaitsTests
{
    private static string Workflow(string name) => File.ReadAllText(Repo.PathTo(".github", "workflows", name)).ReplaceLineEndings("\n");

    /// <summary>The job from its header to the next job at the same indentation.</summary>
    private static string Job(string yaml, string job)
    {
        int at = yaml.IndexOf($"\n  {job}:\n", StringComparison.Ordinal);
        Assert.True(at >= 0, $"the workflow has no {job} job");
        var next = NextJob().Match(yaml, at + job.Length + 4);
        return next.Success ? yaml[at..next.Index] : yaml[at..];
    }

    private static string Step(string job, string name)
    {
        int at = job.IndexOf($"- name: {name}\n", StringComparison.Ordinal);
        Assert.True(at >= 0, $"no step named {name}");
        int next = job.IndexOf("\n      - ", at + 10, StringComparison.Ordinal);
        return next > 0 ? job[at..next] : job[at..];
    }

    [Fact]
    public void AFailedTestIsRunOnceMoreInTheSameJobAndOnlyTheFailedOnes()
    {
        string test = Job(Workflow("ci.yml"), "test");
        string step = Step(test, "Test");

        // The suite still runs after the build, and the retry lives inside the test step: a build failure never reaches it, and the
        // workflow itself is never run again.
        Assert.True(test.IndexOf("- name: Build\n", StringComparison.Ordinal) < test.IndexOf("- name: Test\n", StringComparison.Ordinal));
        Assert.Contains("--no-build", step, StringComparison.Ordinal);
        Assert.Contains("--logger trx --results-directory \"$results/first/", step, StringComparison.Ordinal);
        Assert.Contains("scripts/test-retry.py plan \"$results/first\" $failed --out retry-plan.txt", step, StringComparison.Ordinal);
        Assert.Contains("--filter \"$filter\"", step, StringComparison.Ordinal);
        Assert.Contains("--results-directory \"$results/retry/", step, StringComparison.Ordinal);
        Assert.Contains("scripts/test-retry.py verdict", step, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(step, @"dotnet test ").Count);
        Assert.DoesNotContain("gh run rerun", Workflow("ci.yml"), StringComparison.Ordinal);

        // Every leg, and each flake goes up as an artifact the record is built from.
        Assert.Contains("os: [windows-latest, ubuntu-latest, macos-latest]", test, StringComparison.Ordinal);
        string upload = Step(test, "Tests that passed only on their retry");
        Assert.Contains("name: flaky-tests-${{ matrix.os }}", upload, StringComparison.Ordinal);
        Assert.Contains("if-no-files-found: ignore", upload, StringComparison.Ordinal);
        Assert.Contains("test-retry-output.txt", Step(test, "Test counts"), StringComparison.Ordinal);

        // The gatherer reads exactly those artifacts and writes the file the planning session reads.
        string gather = File.ReadAllText(Repo.PathTo("scripts", "flaky-tests.py"));
        Assert.Contains("flaky-tests-{leg}", gather, StringComparison.Ordinal);
        Assert.Contains("\"flaky-tests.md\"", gather, StringComparison.Ordinal);
        Assert.Contains("TIMES = 3", gather, StringComparison.Ordinal);
        Assert.Contains("FORTNIGHT = 14", gather, StringComparison.Ordinal);
    }

    /// <summary>
    /// The script's decisions, on results files made here: a test that passes on its retry lets the leg pass and is named as a warning;
    /// one that fails twice fails the leg; a project that failed with no failed test, or wrote no results, is not retried at all.
    /// </summary>
    [Fact]
    public void TheRetryPassesAFlakeNamesItAndFailsWhatFailsTwice()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-retry-{Guid.NewGuid():N}");
        try
        {
            Trx(folder, "first", ("Flaky", "Failed"), ("Broken", "Failed"), ("Fine", "Passed"));
            Trx(folder, "once", ("Flaky", "Passed"), ("Broken", "Passed"));
            Trx(folder, "twice", ("Flaky", "Passed"), ("Broken", "Failed"));
            Trx(folder, "clean", ("Fine", "Passed"));

            string plan = Path.Combine(folder, "plan.txt");
            var planned = IpadLogsTests.Python($"scripts/test-retry.py plan \"{Path.Combine(folder, "first")}\" tests/GroupLab.Core.Tests --out \"{plan}\"");
            if (planned is null)
            {
                return;
            }

            Assert.Equal(0, planned.Value.Exit);
            Assert.Equal(
                "tests/GroupLab.Core.Tests\tFullyQualifiedName=GroupLab.Core.Tests.Sample.Broken|FullyQualifiedName=GroupLab.Core.Tests.Sample.Flaky\n",
                File.ReadAllText(plan));

            string tsv = Path.Combine(folder, "flaky.tsv");
            var once = IpadLogsTests.Python($"scripts/test-retry.py verdict \"{Path.Combine(folder, "first")}\" \"{Path.Combine(folder, "once")}\" --leg macos-latest --run 7 --out \"{tsv}\"")!.Value;
            Assert.Equal(0, once.Exit);
            Assert.Contains("::warning title=Passed only on its retry::GroupLab.Core.Tests.Sample.Flaky failed on macos-latest", once.Said, StringComparison.Ordinal);
            // Both failed tests passed on their retry, so both are rows, and the one that passed the first time is not.
            string[] rows = File.ReadAllLines(tsv);
            Assert.Equal(2, rows.Length);
            Assert.Contains(rows, r => r.EndsWith("\tmacos-latest\t7\tGroupLab.Core.Tests.Sample.Flaky", StringComparison.Ordinal));
            Assert.DoesNotContain(rows, r => r.EndsWith(".Fine", StringComparison.Ordinal));

            var twice = IpadLogsTests.Python($"scripts/test-retry.py verdict \"{Path.Combine(folder, "first")}\" \"{Path.Combine(folder, "twice")}\" --leg macos-latest --run 8 --out \"{tsv}\"")!.Value;
            Assert.Equal(1, twice.Exit);
            Assert.Contains("::error title=Failed twice::GroupLab.Core.Tests.Sample.Broken", twice.Said, StringComparison.Ordinal);

            // Not a failed test, so nothing to retry: the leg fails as it always did.
            Assert.Equal(1, IpadLogsTests.Python($"scripts/test-retry.py plan \"{Path.Combine(folder, "clean")}\" tests/GroupLab.Core.Tests --out \"{plan}\"")!.Value.Exit);
            Assert.Equal(1, IpadLogsTests.Python($"scripts/test-retry.py plan \"{Path.Combine(folder, "none")}\" tests/GroupLab.App.Tests --out \"{plan}\"")!.Value.Exit);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    /// <summary>A results file in the shape the test platform writes, in run/GroupLab.Core.Tests/, with the given outcomes.</summary>
    private static void Trx(string folder, string run, params (string Method, string Outcome)[] tests)
    {
        string dir = Path.Combine(folder, run, "GroupLab.Core.Tests");
        Directory.CreateDirectory(dir);
        string results = string.Concat(tests.Select((t, i) =>
            $"<UnitTestResult testId=\"id{i}\" testName=\"GroupLab.Core.Tests.Sample.{t.Method}\" outcome=\"{t.Outcome}\" />"));
        string definitions = string.Concat(tests.Select((t, i) =>
            $"<UnitTest name=\"GroupLab.Core.Tests.Sample.{t.Method}\" id=\"id{i}\"><TestMethod className=\"GroupLab.Core.Tests.Sample\" name=\"{t.Method}\" /></UnitTest>"));
        File.WriteAllText(Path.Combine(dir, "run.trx"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">"
            + $"<Results>{results}</Results><TestDefinitions>{definitions}</TestDefinitions><ResultSummary outcome=\"Failed\" /></TestRun>");
    }

    [Fact]
    public void TheDesktopPublishWaitsOnlyForThePackages()
    {
        string nightly = Workflow("nightly.yml");
        string publish = Job(nightly, "publish");
        Assert.Contains("needs: [name-it, package]\n", publish, StringComparison.Ordinal);
        Assert.Contains("pattern: '*-package'", publish, StringComparison.Ordinal);
        Assert.DoesNotContain("android", Step(publish, "Gather the assets"), StringComparison.Ordinal);
        Assert.DoesNotContain("cli-linux-arm64", publish, StringComparison.Ordinal);
        Assert.Contains("name: nightly-manifest-inputs", publish, StringComparison.Ordinal);

        // The server's command line is not named like a package, so the publish job's download leaves it alone.
        Assert.Contains("name: server-cli-arm64", Job(nightly, "cli-arm64"), StringComparison.Ordinal);
        Assert.Contains("name: android-signed", Job(nightly, "android"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheAndroidFilesAndTheCommandLineJoinTheReleaseWhenTheyExist()
    {
        string nightly = Workflow("nightly.yml");

        string android = Job(nightly, "android-files");
        Assert.Contains("needs: [name-it, publish, android]", android, StringComparison.Ordinal);
        Assert.Contains("if: ${{ !cancelled() && needs.publish.result == 'success' }}", android, StringComparison.Ordinal);
        Assert.Contains("continue-on-error: true", android, StringComparison.Ordinal);
        Assert.Contains("name: nightly-manifest-inputs", android, StringComparison.Ordinal);
        // The manifest is signed again with the Android downloads in it and the library as published, then put back on both releases, by
        // one script that android-files.yml also runs for a nightly whose files were not added (entry 454 section 1).
        Assert.Contains("run: bash scripts/nightly-android-files.sh", android, StringComparison.Ordinal);
        Assert.Contains("run: bash scripts/nightly-android-files.sh", Workflow("android-files.yml"), StringComparison.Ordinal);
        android = File.ReadAllText(Repo.PathTo("scripts", "nightly-android-files.sh")).ReplaceLineEndings("\n");
        Assert.Contains("bash scripts/nightly-manifest.sh", android, StringComparison.Ordinal);
        Assert.Contains("gh release download \"v$VERSION\" -p grouplab-target-library.json -D release", android, StringComparison.Ordinal);
        Assert.Contains("if [ ! -f release/grouplab-target-library.json ]; then", File.ReadAllText(Repo.PathTo("scripts", "nightly-manifest.sh")), StringComparison.Ordinal);
        Assert.Contains("gh release upload \"v$VERSION\" --clobber $manifests", android, StringComparison.Ordinal);
        Assert.Contains("gh release upload nightly --clobber $manifests release/grouplab-android.apk release/grouplab-android.aab release/grouplab-android-dev.apk", android, StringComparison.Ordinal);
        Assert.Contains("grep -qF \"**$VERSION**\"", android, StringComparison.Ordinal);
        Assert.Contains("android-native-symbols", android, StringComparison.Ordinal);

        string cli = Job(nightly, "server-command-line");
        Assert.Contains("needs: [name-it, publish, cli-arm64]", cli, StringComparison.Ordinal);
        Assert.Contains("continue-on-error: true", cli, StringComparison.Ordinal);
        Assert.Contains("openssl dgst -sha256 -sign cli-private.pem", cli, StringComparison.Ordinal);
        Assert.Contains("trap 'rm -f cli-private.der cli-private.pem' EXIT", cli, StringComparison.Ordinal);
        Assert.Contains("grep -qF \"**$VERSION**\"", cli, StringComparison.Ordinal);

        // The rolling release's body names the build, which is what both jobs check before touching it.
        Assert.Contains("The newest build, **$VERSION**.", Job(nightly, "publish"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Google Play is sent the bundle only once it is on the release: play-upload.yml starts when the whole nightly run has ended, and the
    /// run ends only after the android-files job, which is in the same workflow.
    /// </summary>
    [Fact]
    public void PlayUploadStillWaitsForTheAndroidFiles()
    {
        string play = Workflow("play-upload.yml");
        Assert.Contains("workflow_run:\n    workflows: [nightly]\n    types: [completed]", play, StringComparison.Ordinal);
        Assert.Contains("github.event.workflow_run.conclusion == 'success'", play, StringComparison.Ordinal);
        Assert.Contains("gh release download \"v$version\"", play, StringComparison.Ordinal);
        Assert.Contains("android-*.aab", play, StringComparison.Ordinal);
        Assert.Contains("\n  android-files:\n", Workflow("nightly.yml"), StringComparison.Ordinal);
        Assert.Contains("name: nightly\n", Workflow("nightly.yml"), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\n  [a-z0-9-]+:\n")]
    private static partial Regex NextJob();

    /// <summary>
    /// Nightly 203 (run 39206788728): the Android files job checked the repository out after downloading the signed files, and a checkout
    /// empties the workspace, so no Android file reached either release. Every download in a late job comes after its checkout.
    /// </summary>
    [Fact]
    public void ALateJobChecksOutBeforeItDownloadsAnything()
    {
        string job = Job(Workflow("nightly.yml"), "android-files");
        int checkout = job.IndexOf("actions/checkout@", StringComparison.Ordinal);
        Assert.True(checkout >= 0, "the Android files job no longer checks out; if it never does, this test can go");
        Assert.True(checkout < job.IndexOf("actions/download-artifact@", StringComparison.Ordinal), "a download comes before the checkout");
        Assert.DoesNotContain("actions/checkout@", job[(checkout + 10)..], StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 452 section 1 and 2: the website job and the ships list are checks no nightly waits for. They run on every push in their own
    /// workflow under the same names, and "build and test", whose success starts the nightly, holds only the test legs and the builds.
    /// </summary>
    [Fact]
    public void TheWebsiteAndTheShipsListNoLongerDecideTheNightly()
    {
        string ci = Workflow("ci.yml"), checks = Workflow("checks.yml");
        Assert.DoesNotContain("\n  website:\n", ci, StringComparison.Ordinal);
        Assert.DoesNotContain("shipping-gate.py --check-generated", ci, StringComparison.Ordinal);
        Assert.Contains("\n  tarball:\n", ci, StringComparison.Ordinal);
        Assert.Contains("name: build and test", ci, StringComparison.Ordinal);

        Assert.Contains("name: website builds", Job(checks, "website"), StringComparison.Ordinal);
        Assert.Contains("python3 scripts/shipping-gate.py --check-generated", Job(checks, "ships-list"), StringComparison.Ordinal);
        Assert.Contains("python3 scripts/release-notes.py --check HEAD", Job(checks, "release-notes"), StringComparison.Ordinal);
        Assert.Contains("fetch-depth: 0", Job(checks, "release-notes"), StringComparison.Ordinal);
        Assert.DoesNotContain("checks no nightly waits for", Workflow("nightly.yml"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 452 section 3: seven nightlies were lost at "Write the notes" with every build fine. A refused note now publishes the build with
    /// short notes and a warning, and docs/RELEASE-NOTES.md takes the notes the build went out with rather than asking the checker again.
    /// </summary>
    [Fact]
    public void ARefusedNoteNoLongerStopsTheNightly()
    {
        string publish = Job(Workflow("nightly.yml"), "publish");
        string notes = Step(publish, "Write the notes");
        Assert.Contains("> notes.md 2> notes-refused.txt; then", notes, StringComparison.Ordinal);
        Assert.Contains("python3 scripts/release-notes.py --fallback \"$VERSION\" \"$SHA\" > notes.md", notes, StringComparison.Ordinal);
        Assert.Contains("::warning::", notes, StringComparison.Ordinal);
        Assert.Contains("cp notes.md notes-written.md", notes, StringComparison.Ordinal);
        string history = Step(publish, "Write this build's notes into docs/RELEASE-NOTES.md");
        Assert.Contains("cp notes-written.md body.md", history, StringComparison.Ordinal);
        Assert.DoesNotContain("release-notes.py \"$VERSION\" \"$SHA\" > body.md", history, StringComparison.Ordinal);
    }
}
