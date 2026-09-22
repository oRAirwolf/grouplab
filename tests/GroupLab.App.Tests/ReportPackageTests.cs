using System.IO.Compression;
using System.Security.Cryptography;
using GroupLab.App.Diagnostics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 41 sections 6 and 8, and entry 45 section 1: the package holds exactly the permitted entries and nothing else,
/// driven by a list so that a new file cannot be added without changing it; a photograph cannot be packaged; and the 2 MB cap drops the
/// previous run's log first and refuses to send a package still over it.
/// </summary>
public sealed class ReportPackageTests : IDisposable
{
    private readonly string root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-package-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        GroupLab.Tests.Support.Temp.Delete(root);
        GC.SuppressFinalize(this);
    }

    private string File(string name, string content)
    {
        string path = Path.Combine(root, name);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Text that does not compress, so a size cap is exercised by bytes that stay large inside the zip.</summary>
    private static string Incompressible(int bytes) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes * 3 / 4));

    [Fact]
    public void ThePermittedEntriesAreExactlyTheReceiversList()
    {
        Assert.Equal(
            [@"^crash-\d{8}-\d{6}-\d+\.json$", @"^grouplab-\d{8}-\d{6}-\d+\.log$", @"^environment\.txt$", @"^description\.txt$", @"^contact\.txt$"],
            ReportPackage.PermittedEntryPatterns);
        Assert.False(ReportPackage.IsPermitted("IMG_1580.jpg"));
        Assert.False(ReportPackage.IsPermitted("settings.json"));
        Assert.False(ReportPackage.IsPermitted("logs/grouplab-20260915-184207-1234.log"));
        Assert.False(ReportPackage.IsPermitted("..grouplab-20260915-184207-1234.log"));
        Assert.False(ReportPackage.IsPermitted("grouplab-2026O915-184207-1234.log"));
    }

    [Fact]
    public void AFullPackageHoldsExactlyThePermittedEntriesAndNoOthers()
    {
        string crash = File("crash-20260915-064212-4242.json", "{\"schema\":1}");
        string run = File("grouplab-20260915-064100-4242.log", "2026-09-15T06:41:00.000Z  INFO   app.start");
        string previous = File("grouplab-20260914-201500-3100.log", "2026-09-14T20:15:00.000Z  INFO   app.start");
        string zip = Path.Combine(root, "out", "grouplab-report-20260915-070000.zip");

        var result = ReportPackage.Build(zip, crash, run, previous, ReportPackage.EnvironmentText(1.5), "It closed when I chose a second sheet.", "alan@example.invalid");

        string[] expected = ["crash-20260915-064212-4242.json", "grouplab-20260915-064100-4242.log", "grouplab-20260914-201500-3100.log", "environment.txt", "description.txt", "contact.txt"];
        Assert.Equal(expected, result.Entries);
        using var archive = ZipFile.OpenRead(zip);
        Assert.Equal(expected, archive.Entries.Select(e => e.FullName));
        Assert.All(archive.Entries, e => Assert.True(ReportPackage.IsPermitted(e.FullName), e.FullName));
        Assert.False(result.TooLargeToSend);

        var minimal = ReportPackage.Build(Path.Combine(root, "minimal.zip"), null, run, null, ReportPackage.EnvironmentText(null), " ", null);
        Assert.Equal(["grouplab-20260915-064100-4242.log", "environment.txt"], minimal.Entries);
    }

    [Fact]
    public void AFileNotOnTheListIsRefusedRatherThanPackaged()
    {
        string photograph = File("IMG_1580.jpg", "not really a photograph");
        Assert.Throws<InvalidOperationException>(() => ReportPackage.Build(Path.Combine(root, "refused.zip"), photograph, null, null, "environment", null, null));
    }

    [Fact]
    public void OverTheCapThePreviousLogGoesFirstAndAPackageStillOverItIsNotSent()
    {
        string run = File("grouplab-20260915-064100-4242.log", Incompressible(300_000));
        string previous = File("grouplab-20260914-201500-3100.log", Incompressible(300_000));

        var trimmed = ReportPackage.Build(Path.Combine(root, "trimmed.zip"), null, run, previous, "environment", null, null, capBytes: 400_000);
        Assert.True(trimmed.DroppedPreviousLog);
        Assert.False(trimmed.TooLargeToSend);
        Assert.DoesNotContain("grouplab-20260914-201500-3100.log", trimmed.Entries);

        var tooLarge = ReportPackage.Build(Path.Combine(root, "large.zip"), null, run, previous, "environment", null, null, capBytes: 100_000);
        Assert.True(tooLarge.TooLargeToSend);
        Assert.True(System.IO.File.Exists(tooLarge.Path));
    }
}
