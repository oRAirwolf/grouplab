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
}
