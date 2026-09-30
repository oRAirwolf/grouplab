using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 311 section 3 item 3: GroupLab's log and Documents from an iPad on a USB cable. scripts/ipad-logs.py holds its
/// rules against a made-up device in its self-test, with no cable and no pymobiledevice3: only GroupLab's process is asked for, another
/// app's line is dropped, nothing outside Documents is read or written, and the device is described by model, never by serial number.
/// </summary>
public class IpadLogsTests
{
    /// <summary>Runs a script with its arguments under whichever python this machine has; null where there is none.</summary>
    internal static (int Exit, string Said)? Python(string arguments)
    {
        foreach (string python in new[] { "python3", "python" })
        {
            System.Diagnostics.Process? run;
            try
            {
                run = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, arguments)
                {
                    WorkingDirectory = Repo.Root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                continue;
            }

            string said = run!.StandardOutput.ReadToEnd() + run.StandardError.ReadToEnd();
            run.WaitForExit();
            if (run.ExitCode == 9009)
            {
                continue;   // the Windows store stub that stands in for a missing python
            }

            return (run.ExitCode, said);
        }

        Assert.True(Environment.GetEnvironmentVariable("CI") is null, "no python on this CI runner, so a script's self-test did not run, and it must");
        return null;
    }

    [Fact]
    public void TheCopyAndTheLogPassTheirOwnSelfTest()
    {
        if (Python("scripts/ipad-logs.py --self-test") is { } run)
        {
            Assert.True(run.Exit == 0, "scripts/ipad-logs.py --self-test failed:\n" + run.Said);
        }
    }

    [Fact]
    public void ADryRunTouchesNoDeviceAndNeedsNoPymobiledevice3()
    {
        if (Python("scripts/ipad-logs.py --pull --dry-run --only logs,sitting") is { } run)
        {
            Assert.True(run.Exit == 0, run.Said);
            Assert.Contains("Would copy org.grouplab.app's Documents (logs, sitting only)", run.Said, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheApplicationLetsItsDocumentsBeReadAndShownInFiles()
    {
        string plist = File.ReadAllText(Repo.PathTo("ios", "GroupLab.iOS", "Info.plist")).ReplaceLineEndings("\n");
        foreach (string key in new[] { "UIFileSharingEnabled", "LSSupportsOpeningDocumentsInPlace" })
        {
            Assert.Matches($@"<key>{key}</key>\s*<true/>", plist);
        }
    }
}
