using System.Diagnostics;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 147 section 1.2: the macOS download is a proper <c>.app</c> bundle, not a bare executable.
/// <para>
/// <b>Nobody here can check this on a Mac</b>, which is the whole point of entry 147, so the parts that do not need one are checked where
/// they can be. A bundle is a folder shaped a particular way with a plist in it: that shape is decidable on any machine, and it is what goes
/// wrong first. A missing <c>CFBundleExecutable</c>, an icon that was not copied, or a plist macOS will not parse each produce the same
/// symptom on the only hardware that could tell us, which is "the application cannot be opened" and nothing else.
/// </para>
/// <para>
/// This runs <c>scripts/macos-bundle.py</c> against a stand-in publish folder. It does not need .NET published for macOS, or a Mac, or
/// anything but Python, which the runner has because the site is built with it.
/// </para>
/// </summary>
public class MacBundleTests
{
    private static string Script => Repo.PathTo("scripts", "macos-bundle.py");

    /// <summary>A folder shaped like a published build: the executable the bundle must point at, a native library, and a subfolder.</summary>
    private static string Published(string root)
    {
        string published = Path.Combine(root, "published");
        Directory.CreateDirectory(Path.Combine(published, "runtimes", "osx-arm64"));
        File.WriteAllText(Path.Combine(published, "GroupLab.App"), "not really an executable");
        File.WriteAllText(Path.Combine(published, "libSkiaSharp.dylib"), "nor this");
        File.WriteAllText(Path.Combine(published, "runtimes", "osx-arm64", "native.dylib"), "nor this");
        return published;
    }

    private static (int Code, string Output) Bundle(string published, string into, string version, string arch)
    {
        var start = new ProcessStartInfo("python", $"-B \"{Script}\" \"{published}\" \"{into}\" --version {version} --arch {arch}")
        {
            WorkingDirectory = Repo.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(start)!;
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit(120000);
        return (process.ExitCode, output);
    }

    [Fact]
    public void TheBundleHasTheShapeMacosExpects()
    {
        string root = GroupLab.Tests.Support.Temp.Folder("mac-bundle");
        try
        {
            var (code, output) = Bundle(Published(root), Path.Combine(root, "out"), "0.2.0-nightly.84", "macos-arm64");
            Assert.True(code == 0, "the bundle script failed: " + output);

            string app = Path.Combine(root, "out", "GroupLab.app");
            Assert.True(Directory.Exists(app), "no GroupLab.app was made");

            // The three things macOS looks for. Without PkgInfo it is a folder that happens to be named .app.
            Assert.True(File.Exists(Path.Combine(app, "Contents", "Info.plist")));
            Assert.True(File.Exists(Path.Combine(app, "Contents", "PkgInfo")));
            Assert.True(File.Exists(Path.Combine(app, "Contents", "MacOS", "GroupLab.App")), "the executable is not where the plist points");

            // Everything published came across, including the subfolders, or the application starts and immediately cannot find its runtime.
            Assert.True(File.Exists(Path.Combine(app, "Contents", "MacOS", "libSkiaSharp.dylib")));
            Assert.True(File.Exists(Path.Combine(app, "Contents", "MacOS", "runtimes", "osx-arm64", "native.dylib")));

            // The icon, which is the difference between an application and a white rectangle in the dock.
            Assert.True(File.Exists(Path.Combine(app, "Contents", "Resources", "grouplab.icns")), "the icon was not copied into the bundle");
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }

    /// <summary>
    /// The plist says what it has to say, and says the version in the two different shapes macOS wants.
    /// <para>
    /// <c>CFBundleShortVersionString</c> is what Finder shows in Get Info and macOS expects it to look like a version number. A nightly's
    /// <c>0.2.0-nightly.84</c> does not, so the plain part goes there and the whole string goes in <c>CFBundleVersion</c>, where a longer one
    /// is allowed. Getting that the wrong way round is not an error anywhere; it just shows a person something that looks broken.
    /// </para>
    /// </summary>
    [Fact]
    public void ThePlistSaysWhatMacosReads()
    {
        string root = GroupLab.Tests.Support.Temp.Folder("mac-bundle");
        try
        {
            var (code, output) = Bundle(Published(root), Path.Combine(root, "out"), "0.2.0-nightly.84", "macos-x64");
            Assert.True(code == 0, output);

            string plist = File.ReadAllText(Path.Combine(root, "out", "GroupLab.app", "Contents", "Info.plist"));

            Assert.Contains("<key>CFBundleExecutable</key>", plist, StringComparison.Ordinal);
            Assert.Contains("<string>GroupLab.App</string>", plist, StringComparison.Ordinal);

            // What Finder and the dock show. "GroupLab.App" there is the file's name and reads as a mistake.
            Assert.Contains("<key>CFBundleName</key>", plist, StringComparison.Ordinal);
            Assert.Contains("<string>GroupLab</string>", plist, StringComparison.Ordinal);

            Assert.Contains("<string>0.2.0</string>", plist, StringComparison.Ordinal);
            Assert.Contains("<string>0.2.0-nightly.84</string>", plist, StringComparison.Ordinal);

            Assert.Contains("grouplab.icns", plist, StringComparison.Ordinal);
            Assert.Contains("LSMinimumSystemVersion", plist, StringComparison.Ordinal);

            // Entry 147 section 1.6: labelled untested everywhere it appears, and this is the one label that travels with the file after it
            // has been unpacked and the download page is long forgotten.
            Assert.Contains("macos-x64", plist, StringComparison.Ordinal);
            Assert.Contains("nobody has run this on a real Mac", plist, StringComparison.Ordinal);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }

    /// <summary>It refuses a folder that is not a published GroupLab build, rather than making an empty bundle nobody can open.</summary>
    [Fact]
    public void ItRefusesAFolderThatIsNotAPublishedBuild()
    {
        string root = GroupLab.Tests.Support.Temp.Folder("mac-bundle");
        try
        {
            string empty = Path.Combine(root, "empty");
            Directory.CreateDirectory(empty);

            var (code, output) = Bundle(empty, Path.Combine(root, "out"), "0.2.0", "macos-arm64");

            Assert.True(code != 0, "it made a bundle out of an empty folder");
            Assert.Contains("GroupLab.App", output, StringComparison.Ordinal);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }
}
