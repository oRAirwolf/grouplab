using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 177 section 1: the pull script crashed on the first real submissions because the worker writes "stored"
/// and the script read "stored_name", and a folder passed for the missing file. One check now serves the pull script, the removal script
/// and CI's run of the real worker (tests/python/worker-tests.py), so the three cannot disagree again.
/// </summary>
public class SubmissionScriptsTests
{
    private static string Script(string name) => File.ReadAllText(Repo.PathTo("scripts", name));

    [Fact]
    public void OneCheckServesBothScriptsAndReadsEitherKey()
    {
        string check = Script("SubmissionCheck.ps1");
        Assert.Contains("-contains 'stored'", check, StringComparison.Ordinal);
        Assert.Contains("-contains 'stored_name'", check, StringComparison.Ordinal);
        Assert.Contains("Test-Path $p -PathType Leaf", check, StringComparison.Ordinal);

        foreach (string name in new[] { "Get-TargetSubmissions.ps1", "Remove-ReadSubmissions.ps1" })
        {
            string script = Script(name);
            Assert.Contains(". (Join-Path $PSScriptRoot 'SubmissionCheck.ps1')", script, StringComparison.Ordinal);
            Assert.Contains("Test-SubmissionFolder -Folder", script, StringComparison.Ordinal);
            Assert.DoesNotContain(".stored_name", script, StringComparison.Ordinal);
        }

        Assert.Contains("SubmissionCheck.ps1", File.ReadAllText(Repo.PathTo("tests", "python", "worker-tests.py")), StringComparison.Ordinal);
    }

    /// <summary>
    /// The removal script reads the ledger as it is written, an object holding a submissions list, and its filter is not the loop's own
    /// variable: PowerShell names ignore case, so a parameter called Id was overwritten by the loop's id.
    /// </summary>
    [Fact]
    public void TheRemovalScriptReadsTheLedgerAsWrittenAndFiltersByName()
    {
        string remove = Script("Remove-ReadSubmissions.ps1");
        Assert.Contains("$listName = if ($CrashReports) { 'crash_reports' } else { 'submissions' }", remove, StringComparison.Ordinal);
        Assert.Contains("[string[]]$Only", remove, StringComparison.Ordinal);
        Assert.DoesNotContain("[string[]]$Id", remove, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entries 215 to 217: the pull removes a submission from the server only after its copy here verifies and the
    /// private archive has proven, by downloading it back, that it holds it; and only a folder named as a submission is ever removed.
    /// </summary>
    [Fact]
    public void ThePullRemovesFromTheServerOnlyWhatTheArchiveHasProven()
    {
        string pull = Script("Get-TargetSubmissions.ps1");
        int verify = pull.IndexOf("$r = Test-SubmissionFolder -Folder (Join-Path $LocalRoot $dir)", StringComparison.Ordinal);
        int archive = pull.IndexOf("Add-ToArchive -Folder", StringComparison.Ordinal);
        int remove = pull.IndexOf("sudo rm -rf -- '$RemoteRoot/$dir'", StringComparison.Ordinal);
        Assert.True(verify > 0 && archive > verify && remove > archive, "the pull must check here, then archive, then remove, in that order");
        Assert.Contains("if ($dir -notmatch '^\\d{4}-\\d{2}-\\d{2}_[0-9a-f]{8}$')", pull, StringComparison.Ordinal);
        Assert.Contains("[switch] $KeepOnServer", pull, StringComparison.Ordinal);
        Assert.Contains("[switch] $NoArchive", pull, StringComparison.Ordinal);
        Assert.Contains(". (Join-Path $PSScriptRoot 'SubmissionArchive.ps1')", pull, StringComparison.Ordinal);

        string archiveScript = Script("SubmissionArchive.ps1");
        Assert.Contains("gh release download $tag -R $Repo -p $asset", archiveScript, StringComparison.Ordinal);
        Assert.Contains("isPrivate", archiveScript, StringComparison.Ordinal);
        Assert.DoesNotContain("git lfs", archiveScript, StringComparison.Ordinal);
        Assert.Contains("Test-SubmissionFolder -Folder $unpacked", Script("Test-SubmissionsArchive.ps1"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 220: under Windows PowerShell 5.1, a program's stderr is fatal while $ErrorActionPreference is Stop, and gh's "release not found"
    /// stopped request 31's first pull. Every program these scripts run goes through the one helper that judges by the exit code alone;
    /// tests/powershell/archive-tests.ps1 runs it under both shells.
    /// </summary>
    [Theory]
    [InlineData("SubmissionArchive.ps1")]
    [InlineData("Get-TargetSubmissions.ps1")]
    [InlineData("Remove-ReadSubmissions.ps1")]
    [InlineData("Test-SubmissionsArchive.ps1")]
    public void EveryProgramTheScriptsRunGoesThroughTheOneHelper(string name)
    {
        string script = Script(name);
        var raw = System.Text.RegularExpressions.Regex.Matches(script, @"(?m)^[^#\n]*&\s+(gh|ssh|scp|tar|cmd|python|git)\b");
        Assert.True(raw.Count == 0, $"{name} runs a program directly: {string.Join(" | ", raw.Select(m => m.Value.Trim()))}");
        Assert.Contains("NativeCommand.ps1", name == "Get-TargetSubmissions.ps1" || name == "Test-SubmissionsArchive.ps1" ? Script("SubmissionArchive.ps1") : script, StringComparison.Ordinal);
    }
}
