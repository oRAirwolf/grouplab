using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// Runs where python is on the machine, which is everywhere the site is built and on every CI runner. Anywhere without it the test says so
/// rather than passing quietly. xunit v2 decides a skip at discovery, so the check is made here, as <c>CorrectedJavaScriptFactAttribute</c>
/// already does for Node.
/// </summary>
public sealed class NeedsPythonFactAttribute : FactAttribute
{
    public NeedsPythonFactAttribute()
    {
        if (SiteSyncTests.PythonOnThisMachine is null)
        {
            Skip = "the site sync script is python, and python is not on this machine. CI has it, and runs these there.";
        }
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 128 section 4: the sync script's refusals, driven with no network and no root.
/// <para>
/// The script runs as root on a public web server and unpacks an archive it downloaded. That is the most dangerous thing this project does,
/// so the cases that matter are the ones where it must refuse: an archive that would write outside its folder, one carrying a link or a
/// device, and a build that is not a whole site. Each is checked here against the real functions, by running the script's own code.
/// </para>
/// <para>
/// The happy path is not tested here, because installing a site needs root and a web server. What is tested is everything it refuses, which
/// is what stands between a bad archive and grouplab.org.
/// </para>
/// </summary>
public class SiteSyncTests
{
    private static string Script => Repo.PathTo("website", "server", "grouplab-site-sync.py");

    /// <summary>Python, or null where there is none. Worked out once, because the attribute asks for it at discovery.</summary>
    internal static string? PythonOnThisMachine { get; } = Python();

    private static string? Python()
    {
        foreach (string name in new[] { "python3", "python" })
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo(name, "--version") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false });
                p!.WaitForExit(20000);
                if (p.ExitCode == 0)
                {
                    return name;
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
            }
        }

        return null;
    }

    /// <summary>Runs a snippet against the script's own functions, so these test what the server will run and not a copy of it.</summary>
    private static (int Code, string Out, string Error) Drive(string snippet)
    {
        string file = Path.Combine(Path.GetTempPath(), $"grouplab-sync-check-{Guid.NewGuid():N}.py");
        string source = $"""
import importlib.util, sys
spec = importlib.util.spec_from_file_location("sync", r"{Script}")
sync = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sync)
{snippet}
""";
        File.WriteAllText(file, source);
        try
        {
            using var p = Process.Start(new ProcessStartInfo(PythonOnThisMachine!, $"\"{file}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            })!;
            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();
            p.WaitForExit(60000);
            return (p.ExitCode, output, error);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(file);
        }
    }

    /// <summary>Writes a tar.gz with the entries given, so an archive of any shape can be handed to the script.</summary>
    private static string Archive(params (string Name, string Body)[] entries)
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-sync-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "site.tar.gz");

        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        using var tar = new TarWriter(gzip, TarEntryFormat.Pax);
        foreach (var (name, body) in entries)
        {
            var entry = new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(Encoding.UTF8.GetBytes(body)) };
            tar.WriteEntry(entry);
        }

        return path;
    }

    private static string Page(string commit) => $"<!doctype html><meta name=\"grouplab-site-build\" content=\"{commit}\"><title>x</title>";

    [NeedsPythonFact]
    public void AnArchiveThatWouldUnpackOutsideItsFolderIsRefused()
    {
        foreach (string hostile in new[] { "../escaped.html", "/etc/passwd", "a/../../escaped.html" })
        {
            string archive = Archive((hostile, "x"), ("index.html", Page("abc")));
            var (code, output, _) = Drive($"""
import tarfile, tempfile, pathlib
with tempfile.TemporaryDirectory() as tmp:
    into = pathlib.Path(tmp)
    try:
        with tarfile.open(r"{archive}") as tar:
            sync.safe_members(tar, into)
        print("ACCEPTED")
    except ValueError as e:
        print("REFUSED:", e)
""");

            Assert.Equal(0, code);
            Assert.Contains("REFUSED", output, StringComparison.Ordinal);
            Assert.DoesNotContain("ACCEPTED", output, StringComparison.Ordinal);
        }
    }

    [NeedsPythonFact]
    public void AnArchiveCarryingALinkIsRefused()
    {
        // A symlink in a web root is a way to serve anything on the machine, so it is refused rather than followed.
        var (code, output, _) = Drive("""
import tarfile, tempfile, pathlib, io
with tempfile.TemporaryDirectory() as tmp:
    into = pathlib.Path(tmp)
    path = into / "hostile.tar"
    with tarfile.open(path, "w") as tar:
        link = tarfile.TarInfo("index.html")
        link.type = tarfile.SYMTYPE
        link.linkname = "/etc/passwd"
        tar.addfile(link)
    try:
        with tarfile.open(path) as tar:
            sync.safe_members(tar, into)
        print("ACCEPTED")
    except ValueError as e:
        print("REFUSED:", e)
""");

        Assert.Equal(0, code);
        Assert.Contains("REFUSED", output, StringComparison.Ordinal);
        Assert.Contains("it is a link", output, StringComparison.Ordinal);
    }

    [NeedsPythonFact]
    public void ABuildMissingARequiredPageIsRefused()
    {
        // Everything but download/index.html, which is enough to take the Download page off the site.
        var (code, output, _) = Drive($"""
import pathlib, tempfile
with tempfile.TemporaryDirectory() as tmp:
    folder = pathlib.Path(tmp)
    for name in ["index.html", "404.html", "support/index.html"]:
        p = folder / name
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text('{Page("abc")}', encoding="utf-8")
    (folder / "assets" / "css").mkdir(parents=True, exist_ok=True)
    (folder / "assets" / "css" / "site.css").write_text("body:after", encoding="utf-8")
    for i in range(30):
        (folder / ("filler" + str(i) + ".txt")).write_text("x", encoding="utf-8")
    try:
        sync.check_build(folder, "abc")
        print("ACCEPTED")
    except ValueError as e:
        print("REFUSED:", e)
""");

        Assert.Equal(0, code);
        Assert.Contains("REFUSED", output, StringComparison.Ordinal);
        Assert.Contains("download/index.html", output, StringComparison.Ordinal);
    }

    [NeedsPythonFact]
    public void ABuildWhosePagesDoNotSayWhatTheyAreIsRefused()
    {
        var (code, output, _) = Drive("""
import pathlib, tempfile
with tempfile.TemporaryDirectory() as tmp:
    folder = pathlib.Path(tmp)
    for name in ["index.html", "404.html", "download/index.html", "support/index.html"]:
        p = folder / name
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text("<!doctype html><title>no build tag here</title>", encoding="utf-8")
    (folder / "assets" / "css").mkdir(parents=True, exist_ok=True)
    (folder / "assets" / "css" / "site.css").write_text("body:after", encoding="utf-8")
    for i in range(30):
        (folder / ("filler" + str(i) + ".txt")).write_text("x", encoding="utf-8")
    try:
        sync.check_build(folder, None)
        print("ACCEPTED")
    except ValueError as e:
        print("REFUSED:", e)
""");

        Assert.Equal(0, code);
        Assert.Contains("REFUSED", output, StringComparison.Ordinal);
        Assert.Contains("what commit", output, StringComparison.Ordinal);
    }

    [NeedsPythonFact]
    public void AWholeSiteIsAccepted()
    {
        // The other side of the refusals: a build that is a site passes, so the checks are not simply refusing everything.
        var (code, output, _) = Drive($"""
import pathlib, tempfile
with tempfile.TemporaryDirectory() as tmp:
    folder = pathlib.Path(tmp)
    for name in ["index.html", "404.html", "download/index.html", "support/index.html"]:
        p = folder / name
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text('{Page("abc123")}', encoding="utf-8")
    (folder / "assets" / "css").mkdir(parents=True, exist_ok=True)
    (folder / "assets" / "css" / "site.css").write_text("body:after", encoding="utf-8")
    for i in range(30):
        (folder / ("filler" + str(i) + ".txt")).write_text("x", encoding="utf-8")
    sync.check_build(folder, "abc123")
    print("ACCEPTED", sync.commit_of(folder))
""");

        Assert.Equal(0, code);
        Assert.Contains("ACCEPTED abc123", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// The things the script must never do. A timer job on a shared web server is one bad line away from taking down a site that has
    /// nothing to do with this project, so the absence of those lines is worth holding.
    /// </summary>
    [Fact]
    public void TheSyncNeverTouchesNginxOrAnotherDomain()
    {
        string text = CodeOnly(File.ReadAllText(Script));

        // Read as code, not as prose: the script's own docstring says it never changes nginx, and a check that failed on that would be a
        // check that punishes saying so.
        foreach (string forbidden in new[] { "nginx", "pissinhot", "systemctl reload", "v-restart", "certbot" })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        }

        // It talks to the server over TLS properly: an unchecked certificate would make the live check meaningless.
        Assert.DoesNotContain("--insecure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("-k ", text, StringComparison.Ordinal);

        // And it only ever writes inside grouplab.org's own folders and its own state.
        Assert.Contains("/home/airwolf/web/grouplab.org/public_html", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 129 section 3.5.1: the worker that decodes a stranger's photograph has no network and cannot write
    /// outside the three folders it moves files between.
    /// <para>
    /// Decoding somebody else's file is the moment this machine is most exposed. If a decoder is ever made to run something, this is what
    /// decides whether it can reach anything: no network at all, everything read only but its own folders, and caps on memory, processor and
    /// time so that a decompression bomb cannot take the web server down with it.
    /// </para>
    /// </summary>
    [Fact]
    public void TheRebuildWorkerHasNoNetworkAndCannotWriteOutsideItsOwnFolders()
    {
        string unit = File.ReadAllText(Repo.PathTo("website", "server", "grouplab-intake-worker.service"));

        foreach (string wanted in new[]
        {
            "PrivateNetwork=yes", "IPAddressDeny=any", "ProtectSystem=strict", "NoNewPrivileges=yes",
            "MemoryMax=", "RuntimeMaxSec=", "CPUQuota=",
        })
        {
            Assert.Contains(wanted, unit, StringComparison.Ordinal);
        }

        // It is not the web user and not root: the thing that answers the internet and the thing that decodes what arrives are separate.
        Assert.Contains("User=airwolf", unit, StringComparison.Ordinal);

        var writable = unit.ReplaceLineEndings("\n").Split('\n')
            .Where(l => l.StartsWith("ReadWritePaths=", StringComparison.Ordinal))
            .Select(l => l["ReadWritePaths=".Length..].Trim())
            .ToList();

        Assert.NotEmpty(writable);
        foreach (string path in writable)
        {
            Assert.True(
                path.Contains("/private/quarantine", StringComparison.Ordinal)
                || path.Contains("/private/ready", StringComparison.Ordinal)
                || path.Contains("/private/refused", StringComparison.Ordinal)
                || path == "/home/airwolf/logs",
                $"the worker may write to {path}, which is not one of the folders it moves files between");
        }

        // And never into the site itself: a rebuilt photograph must not be able to land in a web root.
        Assert.DoesNotContain("public_html", unit, StringComparison.Ordinal);
    }

    /// <summary>The worker keeps nothing of the original file, which is the whole of its protection.</summary>
    [Fact]
    public void TheRebuildWorkerDeletesTheOriginalBytes()
    {
        string worker = CodeOnly(File.ReadAllText(Repo.PathTo("website", "server", "grouplab-intake-worker.py")));

        // The original is unlinked after the rebuild, and the rebuilt file is what moves on.
        Assert.Contains("original.unlink()", worker, StringComparison.Ordinal);

        // Nothing copies the original into the output: no shutil.copy of the uploaded file, and no exif carried across as bytes.
        Assert.DoesNotContain("copy2(original", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("info[\"exif\"]", worker, StringComparison.Ordinal);
    }

    /// <summary>A python file with its comments and its triple quoted blocks taken out, so a guard reads what runs.</summary>
    private static string CodeOnly(string python)
    {
        string withoutBlocks = System.Text.RegularExpressions.Regex.Replace(python, @"""""""[\s\S]*?""""""", "");
        var lines = withoutBlocks.ReplaceLineEndings("\n").Split('\n')
            .Select(l => l.Contains('#', StringComparison.Ordinal) ? l[..l.IndexOf('#', StringComparison.Ordinal)] : l);
        return string.Join('\n', lines);
    }

    /// <summary>The unit keeps the script inside the folders it is allowed to write, whatever the script does.</summary>
    [Fact]
    public void TheServiceIsConfinedToGroupLabsOwnFolders()
    {
        string unit = File.ReadAllText(Repo.PathTo("website", "server", "grouplab-site-sync.service"));

        foreach (string wanted in new[] { "ProtectSystem=strict", "PrivateTmp=yes", "NoNewPrivileges=yes" })
        {
            Assert.Contains(wanted, unit, StringComparison.Ordinal);
        }

        var writable = unit.ReplaceLineEndings("\n").Split('\n')
            .Where(l => l.StartsWith("ReadWritePaths=", StringComparison.Ordinal))
            .Select(l => l["ReadWritePaths=".Length..].Trim())
            .ToList();

        Assert.NotEmpty(writable);
        foreach (string path in writable)
        {
            Assert.True(
                path.Contains("grouplab.org", StringComparison.Ordinal) || path.Contains("grouplab-site-sync", StringComparison.Ordinal) || path == "/home/airwolf/logs",
                $"the service may write to {path}, which is not one of GroupLab's own folders");
        }
    }
    /// <summary>
    /// The public key the installer puts on the server is the public half of the key the application already trusts, NOTES-FROM-PLANNING.md
    /// entry 128 section 3.3: "same key as the update manifest, so no new secret exists". They are in two files, so a test holds them in step;
    /// were they to drift, the server would refuse every site release it was sent and the only sign would be a log nobody reads.
    /// </summary>
    [Fact]
    public void TheServersPublicKeyIsTheOneTheApplicationTrusts()
    {
        string pem = File.ReadAllText(Repo.PathTo("website/server/update-signing.pub"));

        Assert.StartsWith("-----BEGIN PUBLIC KEY-----", pem.Trim(), StringComparison.Ordinal);
        Assert.EndsWith("-----END PUBLIC KEY-----", pem.Trim(), StringComparison.Ordinal);

        string base64 = string.Concat(pem
            .Replace("-----BEGIN PUBLIC KEY-----", "", StringComparison.Ordinal)
            .Replace("-----END PUBLIC KEY-----", "", StringComparison.Ordinal)
            .Where(c => !char.IsWhiteSpace(c)));
        Assert.Equal(GroupLab.Core.Updates.UpdateKeys.PublicKey, base64);

        // And it is a key, not a string that looks like one.
        using var key = System.Security.Cryptography.ECDsa.Create();
        key.ImportFromPem(pem);
        Assert.Equal(256, key.KeySize);
    }

    /// <summary>
    /// A dry run changes nothing, which is the whole of what the words mean.
    /// <para>
    /// <b>It did not.</b> Alan ran <c>install.py --dry-run</c> on the server and it created <c>/var/lib/grouplab-site-sync</c>: the dry run
    /// said "would create" it, and the real run afterwards said it "is there". Two faults met. The installer passed a hard-coded false where
    /// it meant its own dry run flag, so it really executed the sync's dry run, and the sync made its state folder and its log folder on the
    /// way in rather than when it had something to put in them.
    /// </para>
    /// <para>
    /// This reads both scripts and requires that nothing outside a guarded branch makes a directory. It is a source check rather than a run,
    /// because the paths these scripts write to are absolute server paths that a test must never create.
    /// </para>
    /// </summary>
    [Fact]
    public void ADryRunCreatesNothing()
    {
        // The installer must hand its own dry run flag to the sync, never a constant.
        string installer = File.ReadAllText(Repo.PathTo("website/server/install.py"));
        Assert.DoesNotContain("--dry-run\"], False)", installer, StringComparison.Ordinal);
        Assert.Contains("--dry-run\"], args.dry_run)", installer, StringComparison.Ordinal);

        // And neither script may make a directory before it knows it is not a dry run. Every mkdir is checked by hand here rather than
        // counted, because a new one should have to be thought about.
        foreach (string file in new[] { "website/server/install.py", "website/server/grouplab-site-sync.py" })
        {
            var lines = File.ReadAllLines(Repo.PathTo(file));
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(".mkdir(", StringComparison.Ordinal))
                {
                    continue;
                }

                bool guarded = Guarded(lines, i);
                Assert.True(guarded, $"{file} line {i + 1} makes a directory where a dry run could reach it: {lines[i].Trim()}");
            }
        }
    }

    /// <summary>
    /// Whether a mkdir sits somewhere a dry run cannot reach: inside a function that only the real path calls, or after the point where the
    /// script has committed to changing something. The ones that are fine are named, so a new mkdir fails until somebody says which it is.
    /// </summary>
    private static bool Guarded(string[] lines, int at)
    {
        string line = lines[at].Trim();

        // The sync writes its state only once it has installed a site, and the installer makes folders only past its own dry run return.
        string[] allowed =
        [
            "STATE.mkdir(parents=True, exist_ok=True)",
            "path.mkdir(parents=True, exist_ok=True)",
            "BACKUPS.mkdir(parents=True, exist_ok=True)",
            "SITE_ROOT.mkdir(parents=True, exist_ok=True)",
            "ERROR_PAGES.mkdir(parents=True, exist_ok=True)",
            "target.parent.mkdir(parents=True, exist_ok=True)",
            "unpacked.mkdir()",
        ];

        return allowed.Contains(line, StringComparer.Ordinal);
    }

    /// <summary>The log must not make its own folder: that is the installer's job, and a dry run on a fresh machine left one behind.</summary>
    [Fact]
    public void TheLogDoesNotMakeItsOwnFolder()
    {
        string sync = File.ReadAllText(Repo.PathTo("website/server/grouplab-site-sync.py"));

        Assert.DoesNotContain("LOG.parent.mkdir", sync, StringComparison.Ordinal);
        Assert.Contains("if not LOG.parent.is_dir():", sync, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 171 section 6: the installer's closing reminder told the person to set the Turnstile secret and reload
    /// nginx every time, even with the secret set and nothing nginx reads changed. It now says the secret is present without opening it, and
    /// prints the nginx steps only where the include was written.
    /// </summary>
    [Fact]
    public void TheClosingReminderSaysOnlyWhatIsLeft()
    {
        string installer = File.ReadAllText(Repo.PathTo("website/server/install.py"));
        Assert.Contains("TURNSTILE_SECRET.stat().st_size", installer, StringComparison.Ordinal);
        Assert.DoesNotContain("TURNSTILE_SECRET.read", installer, StringComparison.Ordinal);
        Assert.DoesNotContain("open(TURNSTILE_SECRET", installer, StringComparison.Ordinal);
        Assert.Contains("if include in CHANGED:", installer, StringComparison.Ordinal);
        Assert.Contains("needs no reload", installer, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 174: the live check has to outlast nginx's hold on the old file, open_file_cache_valid 60s on the server,
    /// or a good deploy is rolled back whenever somebody is reading the home page steadily. It rolled entry 174's fix back four times.
    /// </summary>
    [Fact]
    public void TheLiveCheckOutlastsNginxsHoldOnTheOldPage()
    {
        string sync = File.ReadAllText(Repo.PathTo("website/server/grouplab-site-sync.py"));
        int tries = int.Parse(System.Text.RegularExpressions.Regex.Match(sync, @"(?m)^CHECK_TRIES = (\d+)").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        int wait = int.Parse(System.Text.RegularExpressions.Regex.Match(sync, @"(?m)^CHECK_WAIT_SECONDS = (\d+)").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.True((tries - 1) * wait > 60, $"the live check spans {(tries - 1) * wait} s, and nginx can serve the old page for 60");
    }
}
