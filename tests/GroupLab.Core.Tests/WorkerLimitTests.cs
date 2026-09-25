using System.Globalization;
using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 176: the intake worker was killed for memory on every run, so no submission ever reached ready, and its
/// quarantine sweep would have deleted the two it could not finish after an hour. These hold the decisions that fix it, read out of the
/// worker, its unit and the installer, so none can drift away from the others.
/// </summary>
public class WorkerLimitTests
{
    private static string Worker => File.ReadAllText(Repo.PathTo("website/server/grouplab-intake-worker.py"));

    private static string Unit => File.ReadAllText(Repo.PathTo("website/server/grouplab-intake-worker.service"));

    private static long Constant(string text, string name) =>
        long.Parse(Regex.Match(text, $@"(?m)^{name} = ([\d_]+)").Groups[1].Value.Replace("_", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);

    /// <summary>
    /// Section 3: the pixel cap and the memory limit are one decision. 600 megapixels needed several times the 1 GB limit, so a file the
    /// worker had accepted would have killed it. The cap at the worker's own peak bytes a pixel, and a margin for the interpreter, fits.
    /// </summary>
    [Fact]
    public void ThePixelCapFitsTheMemoryLimit()
    {
        long pixels = Constant(Worker, "MAX_PIXELS"), perPixel = Constant(Worker, "BYTES_PER_PIXEL_AT_PEAK"), megabytes = Constant(Worker, "MEMORY_MAX_MB");
        long unit = long.Parse(Regex.Match(Unit, @"(?m)^MemoryMax=(\d+)M\r?$").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.Equal(megabytes, unit);
        Assert.True((pixels * perPixel / 1_000_000) + 100 <= unit, $"{pixels:N0} pixels at {perPixel} bytes need more than MemoryMax={unit}M");
        Assert.True(pixels >= 108_000_000, "a 108 megapixel phone photograph must decode");
    }

    /// <summary>Section 9.3: nothing in quarantine is deleted for age, and what keeps failing is refused with its reason.</summary>
    [Fact]
    public void QuarantineIsNeverSweptAndStuckWorkIsRefused()
    {
        string worker = Worker;
        string sweep = worker[worker.IndexOf("def sweep()", StringComparison.Ordinal)..worker.IndexOf("def main()", StringComparison.Ordinal)];
        Assert.DoesNotContain("QUARANTINE", sweep, StringComparison.Ordinal);
        Assert.DoesNotContain("QUARANTINE_HOURS", worker, StringComparison.Ordinal);
        Assert.Equal(3, Constant(worker, "MAX_ATTEMPTS"));
        Assert.Contains("(folder / ATTEMPTS).write_text(str(n)", worker, StringComparison.Ordinal);
        Assert.Contains("refused.txt", worker, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entries 215 and 216: every folder a submission can wait in has a limit. Refused after seven days, ready after
    /// sixty whether or not it was pulled, and quarantine by attempts alone, as above; the error reports' two folders likewise.
    /// </summary>
    [Fact]
    public void EveryHoldingFolderHasALimit()
    {
        string worker = Worker;
        string sweep = worker[worker.IndexOf("def sweep()", StringComparison.Ordinal)..worker.IndexOf("def main()", StringComparison.Ordinal)];
        Assert.Contains("READY", sweep, StringComparison.Ordinal);
        Assert.Contains("REFUSED", sweep, StringComparison.Ordinal);
        Assert.Equal(60L, Constant(worker, "READY_DAYS"));
        Assert.Equal(7L, Constant(worker, "REFUSED_DAYS"));

        string errors = File.ReadAllText(Repo.PathTo("website", "server", "grouplab-error-worker.py"));
        Assert.Equal(30L, Constant(errors, "INCOMING_DAYS"));
        Assert.Equal(7L, Constant(errors, "REFUSED_DAYS"));
        Assert.Matches(@"def main\(\) -> int:\s+sweep\(\)", errors);

        // Entries 207 and 208: the survey's reports are counted and deleted; one never counted goes after thirty days whatever happens.
        string survey = File.ReadAllText(Repo.PathTo("website", "server", "grouplab-survey-worker.py"));
        Assert.Equal(30L, Constant(survey, "INCOMING_DAYS"));
        Assert.Equal(7L, Constant(survey, "REFUSED_DAYS"));
        Assert.Matches(@"def main\(\) -> int:\s+sweep\(\)", survey);
    }

    /// <summary>
    /// Sections 8 and 9.2: the daemon's client with an open file, never a path clamd cannot read, only Unix sockets, and a scanner that
    /// did not scan recorded per file for the pull script to count.
    /// </summary>
    [Fact]
    public void TheScannerIsTheDaemonAndAFailureToScanIsLoud()
    {
        string worker = Worker;
        Assert.Contains("\"--stream\"", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("\"--fdpass\"", worker, StringComparison.Ordinal);
        Assert.Contains("record[\"notScanned\"]", worker, StringComparison.Ordinal);
        Assert.Contains("RestrictAddressFamilies=AF_UNIX", Unit, StringComparison.Ordinal);
        Assert.Contains("PrivateNetwork=yes", Unit, StringComparison.Ordinal);
        Assert.Contains("notScanned", File.ReadAllText(Repo.PathTo("scripts/Get-TargetSubmissions.ps1")), StringComparison.Ordinal);
    }

    /// <summary>Section 9.1: the installer checks every dependency the worker has, naming the package for each, HEIC included.</summary>
    [Fact]
    public void TheInstallerChecksEverythingTheWorkerNeeds()
    {
        string installer = File.ReadAllText(Repo.PathTo("website/server/install.py"));
        foreach (string package in new[] { "python3-pil", "libheif-examples", "clamdscan", "clamav-daemon" })
        {
            Assert.Contains($"\"{package}\")", installer, StringComparison.Ordinal);
        }

        Assert.Contains("import PIL.Image", installer, StringComparison.Ordinal);
        Assert.Contains("heif-convert", Worker, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 182: the worker streams to clamd, so clamd's limits must exceed the largest file it streams, the rebuilt
    /// PNG at the pixel cap; the installer holds clamd.conf to the same figure the worker does.
    /// </summary>
    [Fact]
    public void ClamdsLimitsCoverTheLargestRebuiltFile()
    {
        string worker = Worker, installer = File.ReadAllText(Repo.PathTo("website/server/install.py"));
        long pixels = Constant(worker, "MAX_PIXELS"), limit = Constant(worker, "CLAMD_LIMIT_MB");
        Assert.True(pixels * 3 / 1_048_576 < limit, $"a rebuilt PNG at {pixels:N0} pixels can reach {pixels * 3 / 1_048_576} MB, over clamd's {limit} MB");
        Assert.Equal(limit, Constant(installer, "CLAMD_LIMIT_MB"));
        foreach (string key in new[] { "StreamMaxLength", "MaxFileSize", "MaxScanSize", "AlertExceedsMax" })
        {
            Assert.Contains(key, installer, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 183: the worker tried to decode the receiver's DO-NOT-PUBLISH marker as an image and refused every opted
    /// out submission. It rebuilds only what the receiver recorded in meta.json, knows the marker, and refuses where the two disagree; the pull
    /// script's check reads the opt out from either record. The real receiver, worker and check run together in CI's worker test.
    /// </summary>
    [Fact]
    public void TheWorkerRebuildsOnlyWhatTheReceiverRecordedAndKeepsTheOptOut()
    {
        string worker = Worker;
        Assert.DoesNotContain("p.name not in (\"meta.json\", ATTEMPTS)", worker, StringComparison.Ordinal);
        Assert.Contains("DO_NOT_PUBLISH = \"DO-NOT-PUBLISH\"", worker, StringComparison.Ordinal);
        Assert.Contains("record.get(\"files\")", worker, StringComparison.Ordinal);
        Assert.Contains("disagree about the opt out", worker, StringComparison.Ordinal);
        Assert.Contains("the receiver did not record it", worker, StringComparison.Ordinal);
        Assert.Contains("DO-NOT-PUBLISH", File.ReadAllText(Repo.PathTo("scripts/SubmissionCheck.ps1")), StringComparison.Ordinal);
        Assert.Contains("DO-NOT-PUBLISH", File.ReadAllText(Repo.PathTo("website/api/upload.php")), StringComparison.Ordinal);

        string test = File.ReadAllText(Repo.PathTo("tests/python/worker-tests.py"));
        Assert.Contains("tests/php/receive-one.php", test, StringComparison.Ordinal);
        Assert.Contains("SubmissionCheck.ps1", test, StringComparison.Ordinal);
        Assert.True(File.Exists(Repo.PathTo("tests/php/receiver-harness.php")));
    }
}
