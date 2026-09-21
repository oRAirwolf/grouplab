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
            File.Delete(file);
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
}
