using System.Text.RegularExpressions;
using System.Xml.Linq;
using GroupLab.App.Diagnostics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 234: the development build is its own application in every way, so nothing may name the package, and
/// a copy from Google Play, which cannot be debugged, still tells logcat enough.
/// </summary>
public partial class Entry234Tests
{
    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string here = "")
    {
        for (var dir = new DirectoryInfo(Path.GetDirectoryName(here)!); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GroupLab.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("the repository root");
    }

    private static string Android => Path.Combine(Root(), "android", "GroupLab.Android");

    [GeneratedRegex("\"org\\.grouplab\\.app[.\"]")]
    private static partial Regex PackageLiteral();

    [Fact]
    public void NothingNamesThePackageSoTheDevelopmentBuildIsItsOwnApplication()
    {
        // Entry 290: the screens moved to the shared mobile project, which is read too.
        var named = Directory.EnumerateFiles(Android, "*.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(Path.Combine(Root(), "mobile", "GroupLab.Mobile"), "*.cs", SearchOption.TopDirectoryOnly))
            .Where(f => PackageLiteral().IsMatch(File.ReadAllText(f))).Select(Path.GetFileName).ToList();
        Assert.Empty(named);

        string manifest = File.ReadAllText(Path.Combine(Android, "Properties", "AndroidManifest.xml"));
        Assert.Contains("android:authorities=\"${applicationId}.files\"", manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("\"org.grouplab.app", manifest, StringComparison.Ordinal);

        var project = XDocument.Load(Path.Combine(Android, "GroupLab.Android.csproj"));
        var dev = project.Descendants("PropertyGroup").Single(g => ((string?)g.Attribute("Condition"))?.Contains("GroupLabDev", StringComparison.Ordinal) == true);
        Assert.Equal("org.grouplab.app.dev", (string?)dev.Element("ApplicationId"));
        Assert.Contains("grouplabDebuggable=true", (string?)dev.Element("AndroidManifestPlaceholders"), StringComparison.Ordinal);

        // Entry 248: each build has its own icon from the desktop's mark, adaptive with a monochrome layer and PNGs for older launchers,
        // written by scripts/android-icons.py; the development build's is the light one, so the two are never confused.
        Assert.Contains("grouplabIcon=@mipmap/ic_launcher_dev;", (string?)dev.Element("AndroidManifestPlaceholders"), StringComparison.Ordinal);
        var release = project.Descendants("PropertyGroup").Single(g => ((string?)g.Element("AndroidManifestPlaceholders"))?.Contains("grouplabDebuggable=false", StringComparison.Ordinal) == true);
        Assert.Contains("grouplabIcon=@mipmap/ic_launcher;", (string?)release.Element("AndroidManifestPlaceholders"), StringComparison.Ordinal);
        Assert.Contains("android:roundIcon=\"${grouplabRoundIcon}\"", manifest, StringComparison.Ordinal);
        foreach (string icon in new[] { "ic_launcher", "ic_launcher_dev" })
        {
            Assert.Contains("<monochrome", File.ReadAllText(Path.Combine(Android, "Resources", "mipmap-anydpi-v26", icon + ".xml")), StringComparison.Ordinal);
            foreach (string density in new[] { "mdpi", "hdpi", "xhdpi", "xxhdpi", "xxxhdpi" })
            {
                foreach (string layer in new[] { "", "_round", "_foreground", "_monochrome" })
                {
                    Assert.True(File.Exists(Path.Combine(Android, "Resources", $"mipmap-{density}", icon + layer + ".png")), $"{icon}{layer} at {density}");
                }
            }
        }
    }

    [Fact]
    public void EveryLineButDebugAlsoGoesToTheMirror()
    {
        var seen = new List<(LogLevel Level, string Line)>();
        var before = DiagnosticLog.Mirror;
        DiagnosticLog.Mirror = (level, line) => seen.Add((level, line));
        try
        {
            var log = DiagnosticLog.Disabled("a test with no file");
            log.Write(LogLevel.Info, "analysis.done", [("ms", 5000)]);
            log.Write(LogLevel.Debug, "analysis.step", []);
            log.Write(LogLevel.Error, "analysis.failed", [("error", "OutOfMemory")]);
            Assert.Equal([LogLevel.Info, LogLevel.Error], seen.Select(s => s.Level));
            Assert.Contains("analysis.done", seen[0].Line, StringComparison.Ordinal);
            Assert.Contains("ms=5000", seen[0].Line, StringComparison.Ordinal);
        }
        finally
        {
            DiagnosticLog.Mirror = before;
        }
    }
}
