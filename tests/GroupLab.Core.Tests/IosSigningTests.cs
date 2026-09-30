using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 7: when Alan's eight Apple secrets appear (request 55), the nightly signs and sends the
/// iOS build to TestFlight with no code change; until then it builds unsigned and never fails for want of them; and a malformed secret fails
/// loudly, naming the secret and never its value. scripts/ios-signing.py decides, and its self-test holds each case with made-up values.
/// </summary>
public class IosSigningTests
{
    [Fact]
    public void TheSecretsCheckPassesItsOwnSelfTest()
    {
        foreach (string python in new[] { "python3", "python" })
        {
            System.Diagnostics.Process? run;
            try
            {
                run = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, "scripts/ios-signing.py --self-test")
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

            Assert.True(run.ExitCode == 0, "scripts/ios-signing.py --self-test failed:\n" + said);
            return;
        }

        Assert.True(Environment.GetEnvironmentVariable("CI") is null, "no python on this CI runner, so the signing check's self-test did not run, and it must");
    }

    [Fact]
    public void TheNightlyBuildsIosSignsOnlyOnTheChecksWordAndNeverHoldsUpPublishing()
    {
        string nightly = File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml")).ReplaceLineEndings("\n");
        int ios = nightly.IndexOf("\n  ios:\n", StringComparison.Ordinal);
        int publish = nightly.IndexOf("\n  publish:\n", StringComparison.Ordinal);
        Assert.True(ios > 0 && publish > ios, "the nightly has an ios job before publish");
        string job = nightly[ios..publish];
        Assert.Contains("runs-on: macos-26", job, StringComparison.Ordinal);
        Assert.Contains("continue-on-error: true", job, StringComparison.Ordinal);
        Assert.Contains("python3 scripts/ios-signing.py --check", job, StringComparison.Ordinal);
        Assert.Contains("if: steps.signing.outputs.sign == 'true'", job, StringComparison.Ordinal);
        Assert.Contains("-p:EnableCodeSigning=false", job, StringComparison.Ordinal);

        // Entry 292 section 2.3: the share extension signed with its own profile, never the application's, and that proved every night.
        Assert.Contains("IOS_SHARE_PROFILE: ${{ secrets.IOS_SHARE_PROFILE }}", job, StringComparison.Ordinal);
        Assert.Contains("scripts/ios-signing.py --properties", job, StringComparison.Ordinal);
        Assert.Contains("sh ios/signing-dry-run.sh", job, StringComparison.Ordinal);
        Assert.DoesNotContain("-p:CodesignProvision=", job, StringComparison.Ordinal);
        string needs = nightly[publish..].Split('\n').First(l => l.TrimStart().StartsWith("needs:", StringComparison.Ordinal));
        Assert.DoesNotContain("ios", needs, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCheckNamesTheEightSecretsTheSigningPlanNames()
    {
        string script = File.ReadAllText(Repo.PathTo("scripts", "ios-signing.py"));
        string plan = File.ReadAllText(Repo.PathTo("docs", "IOS-PLAN.md"));
        foreach (string secret in new[] { "APPLE_TEAM_ID", "IOS_DIST_CERT_P12", "IOS_DIST_CERT_PASSWORD", "IOS_PROFILE", "IOS_SHARE_PROFILE", "APPLE_API_ISSUER_ID", "APPLE_API_KEY_ID", "APPLE_API_KEY_P8" })
        {
            Assert.Contains($"\"{secret}\"", script, StringComparison.Ordinal);
            Assert.Contains(secret, plan, StringComparison.Ordinal);
        }

        // Each project turns its own property into its profile, so one profile can never reach both.
        Assert.Contains("<CodesignProvision Condition=\"'$(GroupLabAppProvision)' != ''\">$(GroupLabAppProvision)</CodesignProvision>",
            File.ReadAllText(Repo.PathTo("ios", "GroupLab.iOS", "GroupLab.iOS.csproj")), StringComparison.Ordinal);
        Assert.Contains("<CodesignProvision Condition=\"'$(GroupLabShareProvision)' != ''\">$(GroupLabShareProvision)</CodesignProvision>",
            File.ReadAllText(Repo.PathTo("ios", "GroupLab.Share", "GroupLab.Share.csproj")), StringComparison.Ordinal);
    }

    /// <summary>
    /// Alan, 2026-09-30: TestFlight must not ask the export question for each build. The application and its share extension each say they
    /// use only the encryption iOS provides, and the nightly checks both again inside the signed package.
    /// </summary>
    [Fact]
    public void NeitherBundleMakesTestFlightAskAboutEncryption()
    {
        foreach (string project in new[] { "GroupLab.iOS", "GroupLab.Share" })
        {
            var plist = System.Xml.Linq.XDocument.Load(Repo.PathTo("ios", project, "Info.plist"));
            var keys = plist.Root!.Element("dict")!.Elements().ToList();
            int at = keys.FindIndex(e => e.Name == "key" && e.Value == "ITSAppUsesNonExemptEncryption");
            Assert.True(at >= 0, project + "'s Info.plist does not say whether it uses encryption");
            Assert.Equal("false", keys[at + 1].Name.LocalName);
        }

        Assert.Contains(".get(\"ITSAppUsesNonExemptEncryption\"))", File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml")),
            StringComparison.Ordinal);
    }
}
