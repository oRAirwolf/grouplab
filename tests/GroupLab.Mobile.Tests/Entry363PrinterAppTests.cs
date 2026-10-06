using Avalonia.Headless.XUnit;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Rendering;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 2a on the phone: a sheet shared for a thermal printer's app goes to the share sheet as a 300 dpi
/// picture named for it, or as a PDF, and the darkness test page goes the same way.
/// </summary>
public class Entry363PrinterAppTests
{
    private static TestPhone Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        return (TestPhone)Phone.Platform!;
    }

    [AvaloniaFact]
    public void ASheetAndTheDarknessPageGoToTheShareSheetForAPrinterApp()
    {
        var phone = Started();
        var sheet = new LibrarySheet("GL-SCALE-LTR-1.gltd.json", "Printer check", null, GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-SCALE-LTR-1.gltd.json")).Definition!);
        phone.Asked.Clear();

        Assert.Equal("", TargetsPage.ForPrinterApp(sheet, picture: true));
        Assert.Contains(("share", "GL-SCALE-LTR-1-thermal-300dpi.png"), phone.Asked);
        Assert.Equal("", TargetsPage.ForPrinterApp(sheet, picture: false));
        Assert.Equal("", TargetsPage.DarknessForPrinterApp(picture: true));
        Assert.Contains(("share", "darkness-test-thermal-300dpi.png"), phone.Asked);

        // Entry 377: Android's file provider shares only the cache's shared folder, and a picture written beside it crashed the share.
        string shared = Path.Combine(phone.CacheFolder, "shared") + Path.DirectorySeparatorChar;
        Assert.All(phone.SharedPaths, p => Assert.StartsWith(shared, p, StringComparison.Ordinal));
        Assert.Contains(@"<cache-path name=""shared"" path=""shared/"" />", File.ReadAllText(Repo.PathTo("android", "GroupLab.Android", "Resources", "xml", "share_paths.xml")), StringComparison.Ordinal);
    }
}
