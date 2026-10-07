using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 288: "Never in a Play build: the updater lives in a build flavor ... and the AAB must not contain it." These
/// hold the project to that without building Android here: the updater's files, its WorkManager and its permission are all behind the one
/// property, the property is never on for an AAB, and the nightly both refuses an AAB whose manifest asks to install packages and publishes
/// the Dev APK in the signed manifest the updater reads.
/// </summary>
public class UpdaterFlavorTests
{
    private static string Android(params string[] parts) => Repo.PathTo(["android", "GroupLab.Android", .. parts]);

    private static string Project() => File.ReadAllText(Android("GroupLab.Android.csproj"));

    [Fact]
    public void TheUpdaterIsOnlyForASideloadedApk()
    {
        string project = Project();
        Assert.Contains("<GroupLabUpdater Condition=\"'$(GroupLabUpdater)' == '' and '$(AndroidPackageFormat)' != 'aab' and '$(GroupLabEmulator)' != 'true'\">true</GroupLabUpdater>", project, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"<ItemGroup Condition=""'\$\(GroupLabUpdater\)' != 'true'"">\s*<Compile Remove=""Updates\\\*\*"" />"), project);
        Assert.Matches(new Regex(@"<ItemGroup Condition=""'\$\(GroupLabUpdater\)' == 'true'"">\s*<PackageReference Include=""Xamarin.AndroidX.Work.Runtime"""), project);
        Assert.Contains("<Target Name=\"NoUpdaterInAnAab\"", project, StringComparison.Ordinal);

        // WorkManager appears nowhere else in the project.
        Assert.Single(Regex.Matches(project, "AndroidX.Work", RegexOptions.None));
    }

    [Fact]
    public void NothingOutsideTheUpdatesFolderReachesTheUpdaterUnlessTheFlavorIsOn()
    {
        foreach (string file in Directory.EnumerateFiles(Android(), "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(Android(), file).Replace('\\', '/');
            if (relative.StartsWith("Updates/", StringComparison.Ordinal) || relative.StartsWith("obj/", StringComparison.Ordinal) || relative.StartsWith("bin/", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            bool inside = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#if GROUPLAB_UPDATER", StringComparison.Ordinal))
                {
                    inside = true;
                }
                else if (line.StartsWith("#endif", StringComparison.Ordinal))
                {
                    inside = false;
                }
                else if (!inside)
                {
                    Assert.False(Regex.IsMatch(line, @"\bUpdates\.(SelfUpdate|UpdateCard|UpdateWorker|InstallResultReceiver)\b|RequestInstallPackages|AndroidX\.Work"),
                        $"{relative}:{i + 1} reaches the updater outside #if GROUPLAB_UPDATER");
                }
            }
        }
    }

    [Fact]
    public void ThePermissionToInstallIsAskedForOnlyInTheUpdatesFolder()
    {
        var asking = Directory.EnumerateFiles(Android(), "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && (f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".xml", StringComparison.Ordinal)))
            .Where(f => File.ReadAllText(f).Contains("RequestInstallPackages", StringComparison.Ordinal) || File.ReadAllText(f).Contains("REQUEST_INSTALL_PACKAGES", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(Android(), f).Replace('\\', '/'))
            .ToList();
        Assert.Equal(["Updates/SelfUpdate.cs"], asking);
    }

    [Fact]
    public void TheNightlyChecksTheAabAndPublishesTheDevApkInTheSignedManifest()
    {
        string nightly = File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml")).ReplaceLineEndings("\n");

        // The AAB's publish never turns on the Dev flavor or the updater.
        var aab = Regex.Match(nightly, @"for format in aab apk; do\n(?<body>.*?)\n\s*done", RegexOptions.Singleline);
        Assert.True(aab.Success);
        Assert.DoesNotContain("GroupLabDev", aab.Groups["body"].Value, StringComparison.Ordinal);
        Assert.DoesNotContain("GroupLabUpdater", aab.Groups["body"].Value, StringComparison.Ordinal);

        Assert.Contains("unzip -p signed/grouplab-android.aab base/manifest/AndroidManifest.xml | grep -aq REQUEST_INSTALL_PACKAGES", nightly, StringComparison.Ordinal);
        Assert.Contains("--asset android apk-dev release/grouplab-android-dev.apk \"$base/grouplab-android-dev.apk\"", nightly, StringComparison.Ordinal);
        Assert.Contains("-p:GroupLabTrain=nightly", nightly, StringComparison.Ordinal);
        Assert.Equal(GroupLab.Core.Updates.AndroidUpdates.DevKind, "apk-dev");

        // Entry 386: the plain APK too, on the nightly train, and checked to carry the updater.
        Assert.Contains("--asset android apk release/grouplab-android.apk \"$base/grouplab-android.apk\"", nightly, StringComparison.Ordinal);
        Assert.Contains("for apk in grouplab-android-dev.apk grouplab-android.apk; do", nightly, StringComparison.Ordinal);
        Assert.Contains("[ \"$format\" = apk ] && train=\"-p:GroupLabTrain=nightly\"", nightly, StringComparison.Ordinal);
        Assert.Equal(GroupLab.Core.Updates.AndroidUpdates.SideloadKind, "apk");
    }
}
