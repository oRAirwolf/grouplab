using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3: the Android application asks what may be shared in the desktop's words and keeps the answers
/// with the desktop's code, by compiling those files as they are. These hold that on the desktop, where the tests run: every file the
/// Android project links still exists, the words of each choice are written once, and the phone's first run chooses nothing for the
/// person (entry 203 section 3).
/// </summary>
public sealed class AndroidSharingTests
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

        throw new InvalidOperationException($"No GroupLab.slnx above {here}.");
    }

    private static string Android => Path.Combine(Root(), "android", "GroupLab.Android");

    [Fact]
    public void EveryFileTheApplicationSharesWithTheDesktopExists()
    {
        var project = XDocument.Load(Path.Combine(Android, "GroupLab.Android.csproj"));
        var linked = project.Descendants("Compile").Select(c => (string)c.Attribute("Include")!).Where(i => !i.Contains('*', StringComparison.Ordinal)).ToList();
        Assert.Contains(linked, l => l.EndsWith("SharingWords.cs", StringComparison.Ordinal));
        Assert.Contains(linked, l => l.EndsWith("AppSettings.cs", StringComparison.Ordinal));
        Assert.Contains(linked, l => l.EndsWith("ErrorQueue.cs", StringComparison.Ordinal));
        foreach (string include in linked)
        {
            Assert.True(File.Exists(Path.GetFullPath(Path.Combine(Android, include.Replace('\\', Path.DirectorySeparatorChar)))), $"{include} is gone");
        }
    }

    [Fact]
    public void TheWordsOfEachChoiceAreWrittenOnlyInTheSharedFile()
    {
        var words = SharingWords.TargetChoices.Select(c => c.Words).Concat(SharingWords.ErrorChoices.Select(c => c.Words))
            .Append(SharingWords.TargetsQuestion).Append(SharingWords.ErrorsQuestion).Append(SharingWords.LevelHeading).ToList();
        var sources = Directory.GetFiles(Android, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(Root(), "src", "GroupLab.App"), "MainWindow*.cs"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        foreach (string file in sources)
        {
            string text = File.ReadAllText(file);
            foreach (string said in words)
            {
                Assert.False(text.Contains('"' + said + '"', StringComparison.Ordinal), $"{Path.GetFileName(file)} writes \"{said}\" itself");
            }
        }
    }

    [Fact]
    public void ThePhonesFirstRunChoosesNothingForThePerson()
    {
        string text = File.ReadAllText(Path.Combine(Android, "FirstRunView.cs"));
        var radios = Regex.Matches(text, @"Screens\.Radio\([^;]*\);");
        Assert.Equal(2, radios.Count);
        Assert.All(radios, r => Assert.EndsWith(", false);", r.Value, StringComparison.Ordinal));
        Assert.DoesNotContain("IsChecked = true", text, StringComparison.Ordinal);
    }
}
