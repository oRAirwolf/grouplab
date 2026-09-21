using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 134 section 4: the installer carries the GroupLab icon, and a check fails if it stops doing so.
/// <para>
/// <b>Why this is worth a test rather than a look.</b> Nothing breaks when <c>SetupIconFile</c> goes missing or its path rots: the installer
/// still builds, still installs, and still works. It just quietly goes back to Inno Setup's own icon, on the one file a person downloads and
/// double-clicks before they have ever seen GroupLab. That is exactly the kind of fault nobody reports and nobody notices for months.
/// </para>
/// </summary>
public class InstallerIconTests
{
    private static string Script() => File.ReadAllText(Repo.PathTo("packaging/windows/grouplab.iss"));

    /// <summary>The setting is there, and the file it names exists.</summary>
    [Fact]
    public void TheSetupIconIsNamedAndTheFileIsThere()
    {
        string? line = Script().Split('\n').Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("SetupIconFile=", StringComparison.OrdinalIgnoreCase));

        Assert.True(line is not null, "packaging/windows/grouplab.iss sets no SetupIconFile, so the installer would carry Inno Setup's own icon");

        string relative = line!["SetupIconFile=".Length..].Trim().Replace('\\', Path.DirectorySeparatorChar);
        string full = Path.GetFullPath(Path.Combine(Repo.PathTo("packaging/windows"), relative));

        Assert.True(File.Exists(full), $"SetupIconFile points at {relative}, which is not there");
    }

    /// <summary>
    /// It is the application's own icon and not a second copy of it. Two copies of a mark drift, and the one that drifts is always the one
    /// nobody looks at.
    /// </summary>
    [Fact]
    public void TheInstallerUsesTheApplicationsOwnIcon()
    {
        string line = Script().Split('\n').Select(l => l.Trim())
            .First(l => l.StartsWith("SetupIconFile=", StringComparison.OrdinalIgnoreCase));
        string relative = line["SetupIconFile=".Length..].Trim().Replace('\\', Path.DirectorySeparatorChar);
        string full = Path.GetFullPath(Path.Combine(Repo.PathTo("packaging/windows"), relative));

        Assert.Equal(
            Path.GetFullPath(Repo.PathTo("src/GroupLab.App/Assets/icons/grouplab.ico")),
            full);
    }

    /// <summary>
    /// The icon holds every size Windows asks for. A missing 256 is the one that shows: Explorer's large icons fall back to a scaled 48 and
    /// the mark goes soft on exactly the view people use to look at a download.
    /// </summary>
    [Fact]
    public void TheIconHoldsEverySizeWindowsAsksFor()
    {
        byte[] ico = File.ReadAllBytes(Repo.PathTo("src/GroupLab.App/Assets/icons/grouplab.ico"));

        Assert.Equal(0, BitConverter.ToUInt16(ico, 0));
        Assert.Equal(1, BitConverter.ToUInt16(ico, 2));
        int count = BitConverter.ToUInt16(ico, 4);

        var widths = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int entry = 6 + (i * 16);
            widths.Add(ico[entry] == 0 ? 256 : ico[entry]);
        }

        foreach (int size in new[] { 16, 24, 32, 48, 64, 256 })
        {
            Assert.True(widths.Contains(size), $"the icon has no {size} pixel image, and Windows asks for one. Sizes present: {string.Join(", ", widths.Order())}");
        }
    }

    /// <summary>The wizard images the script names exist, and are the BMP that Inno Setup will accept there.</summary>
    [Fact]
    public void TheWizardImagesAreThereAndAreBitmaps()
    {
        string? line = Script().Split('\n').Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("WizardSmallImageFile=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(line);

        foreach (string name in line!["WizardSmallImageFile=".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string full = Path.GetFullPath(Path.Combine(Repo.PathTo("packaging/windows"), name.Replace('\\', Path.DirectorySeparatorChar)));
            Assert.True(File.Exists(full), $"the wizard image {name} is not there");

            byte[] head = File.ReadAllBytes(full)[..2];
            Assert.True(head[0] == (byte)'B' && head[1] == (byte)'M', $"{name} is not a BMP, and Inno Setup takes nothing else here");
        }
    }

    /// <summary>Add or remove programs shows the application's icon, which it does by pointing at the installed executable.</summary>
    [Fact]
    public void AddOrRemoveProgramsShowsTheIcon()
    {
        Assert.Contains("UninstallDisplayIcon={app}\\GroupLab.App.exe", Script(), StringComparison.Ordinal);
    }
}
