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
        var named = Directory.EnumerateFiles(Android, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f => PackageLiteral().IsMatch(File.ReadAllText(f))).Select(Path.GetFileName).ToList();
        Assert.Empty(named);

        string manifest = File.ReadAllText(Path.Combine(Android, "Properties", "AndroidManifest.xml"));
        Assert.Contains("android:authorities=\"${applicationId}.files\"", manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("\"org.grouplab.app", manifest, StringComparison.Ordinal);

        var project = XDocument.Load(Path.Combine(Android, "GroupLab.Android.csproj"));
        var dev = project.Descendants("PropertyGroup").Single(g => ((string?)g.Attribute("Condition"))?.Contains("GroupLabDev", StringComparison.Ordinal) == true);
        Assert.Equal("org.grouplab.app.dev", (string?)dev.Element("ApplicationId"));
        Assert.Contains("grouplabDebuggable=true", (string?)dev.Element("AndroidManifestPlaceholders"), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(Android, "Resources", "drawable", "grouplab_dev.png")));
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
