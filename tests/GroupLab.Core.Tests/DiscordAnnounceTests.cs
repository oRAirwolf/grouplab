using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 184: every published build announces itself in Discord's #builds, and a stable release in #announcements
/// too. The message is built by scripts/discord-announce.py, whose self-test covers normal notes, notes over the embed's limit, notes with
/// only "Under the hood" and a release with no notes; these hold the workflows to posting only after publishing, never failing a build for
/// it, and never letting a webhook address into the repository.
/// </summary>
public class DiscordAnnounceTests
{
    [Fact]
    public void TheMessageBuilderPassesItsOwnSelfTest()
    {
        foreach (string python in new[] { "python3", "python" })
        {
            System.Diagnostics.Process? run;
            try
            {
                run = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(python, "scripts/discord-announce.py --self-test")
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

            Assert.True(run.ExitCode == 0, "scripts/discord-announce.py --self-test failed:\n" + said);
            return;
        }

        Assert.True(Environment.GetEnvironmentVariable("CI") is null, "no python on this CI runner, so the announcement's self-test did not run, and it must");
    }

    [Fact]
    public void TheNightlyAnnouncesOnlyAfterPublishingAndNeverFailsForIt()
    {
        string nightly = File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml")).ReplaceLineEndings("\n");
        int publish = nightly.IndexOf("- name: Publish this build", StringComparison.Ordinal);
        int announce = nightly.IndexOf("- name: Announce the build in Discord", StringComparison.Ordinal);
        Assert.True(publish > 0 && announce > publish, "the announcement must come after the release is published");
        string step = nightly[announce..nightly.IndexOf("- name: ", announce + 10, StringComparison.Ordinal)];
        Assert.Contains("continue-on-error: true", step, StringComparison.Ordinal);
        Assert.Contains("${{ secrets.DISCORD_BUILDS_WEBHOOK }}", step, StringComparison.Ordinal);
        Assert.Contains("::add-mask::", step, StringComparison.Ordinal);
        Assert.DoesNotContain("DISCORD_ANNOUNCE_WEBHOOK", step, StringComparison.Ordinal);
        Assert.DoesNotContain("--stable", step, StringComparison.Ordinal);

        // A stable release, and only a published one, posts to both channels.
        string release = File.ReadAllText(Repo.PathTo(".github", "workflows", "release.yml")).ReplaceLineEndings("\n");
        string stable = release[release.IndexOf("- name: Announce the release in Discord", StringComparison.Ordinal)..];
        Assert.Contains("if: github.ref_type == 'tag'", stable, StringComparison.Ordinal);
        Assert.Contains("--stable", stable, StringComparison.Ordinal);
        Assert.Contains("continue-on-error: true", stable, StringComparison.Ordinal);
    }

    [Fact]
    public void NoWebhookAddressIsEverInTheRepository()
    {
        var found = new List<string>();
        foreach (string root in new[] { ".github", "scripts", "docs", "website", "src", "tests" })
        {
            foreach (string file in Directory.EnumerateFiles(Repo.PathTo(root), "*", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains("_site", StringComparison.Ordinal) || new FileInfo(file).Length > 2_000_000)
                {
                    continue;
                }

                // Built from halves so this file is not itself a match.
                if (File.ReadAllText(file).Contains("discord.com/api/" + "webhooks/", StringComparison.OrdinalIgnoreCase)
                    || File.ReadAllText(file).Contains("discordapp.com/api/" + "webhooks/", StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(Path.GetRelativePath(Repo.Root, file));
                }
            }
        }

        Assert.True(found.Count == 0, "a Discord webhook address is in the repository, where anyone could post with it: " + string.Join(", ", found));
    }
}
