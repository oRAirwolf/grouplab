using Avalonia.Headless.XUnit;
using GroupLab.App;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 307 on the phone: Settings, Your data. Export hands one file to the share sheet; importing it again says it
/// would add nothing, and a damaged file is refused with a sentence.
/// </summary>
public class Entry307Tests
{
    [AvaloniaFact]
    public void ThePhoneExportsToTheShareSheetAndAReimportAddsNothing()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var settings = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-307-{Guid.NewGuid():N}.json"));
        var section = new DataSection(settings);
        _ = section.Controls().ToList();
        string path = section.Export()!;
        Assert.True(File.Exists(path));
        Assert.Contains(((TestPhone)Phone.Platform!).Asked, a => a == ("share", Path.GetFileName(path)));

        var plan = section.Plan(path)!;
        Assert.Equal(0, plan.Adds);
        Assert.Equal(0, section.Apply());

        string damaged = Path.Combine(Phone.Platform.CacheFolder, "damaged.grouplab");
        File.WriteAllText(damaged, "{\"format\":\"grouplab-data\",\"version\":1,\"sessions\":[{");
        Assert.Null(section.Plan(damaged));
    }
}
