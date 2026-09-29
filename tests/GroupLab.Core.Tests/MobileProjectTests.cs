using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 1: the phone's screens live once, in the shared mobile project, which both the Android
/// and the iOS heads link. It is a plain .NET library, so it cannot call Android or iOS without failing to build; these hold the rest: that
/// it stays plain, that it is where the screens are, and that each head reaches the operating system only through IPhonePlatform.
/// </summary>
public partial class MobileProjectTests
{
    private static string Mobile(params string[] parts) => Repo.PathTo(["mobile", "GroupLab.Mobile", .. parts]);

    [Fact]
    public void TheSharedProjectIsAPlainLibrary()
    {
        string project = File.ReadAllText(Mobile("GroupLab.Mobile.csproj"));
        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", project, StringComparison.Ordinal);
        Assert.DoesNotContain("-android", project, StringComparison.Ordinal);
        Assert.DoesNotContain("-ios", project, StringComparison.Ordinal);
    }

    [Fact]
    public void NoScreenCallsAnOperatingSystemDirectly()
    {
        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Mobile(), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                         && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimStart();
                if (!line.StartsWith("//", StringComparison.Ordinal) && OperatingSystemCall().IsMatch(line))
                {
                    found.Add($"{Path.GetFileName(file)}:{i + 1}");
                }
            }
        }

        Assert.True(found.Count == 0, "the shared screens reach Android or iOS only through IPhonePlatform: " + string.Join(", ", found));
    }

    [Fact]
    public void TheScreensAreInTheSharedProjectAndNotInTheAndroidHead()
    {
        foreach (string screen in new[] { "Shell", "CapturePage", "ResultView", "SettingsView", "SessionsPage", "TargetsPage", "BallisticsPage", "FirstRunView" })
        {
            Assert.True(File.Exists(Mobile(screen + ".cs")), screen + " is not in the shared project");
            Assert.False(File.Exists(Repo.PathTo("android", "GroupLab.Android", screen + ".cs")), screen + " is still in the Android head");
        }

        string android = File.ReadAllText(Repo.PathTo("android", "GroupLab.Android", "GroupLab.Android.csproj"));
        Assert.Contains(@"<ProjectReference Include=""..\..\mobile\GroupLab.Mobile\GroupLab.Mobile.csproj"" />", android, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"global::Android\b|\busing\s+(Android|AndroidX|Java|UIKit|Foundation|AVFoundation)\b|\bJava\.(IO|Lang|Util)\.|\bUIKit\.|\bMainActivity\b")]
    private static partial Regex OperatingSystemCall();
}
