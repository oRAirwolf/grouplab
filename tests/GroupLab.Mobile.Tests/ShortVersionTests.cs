using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 6: the phone shows its version with the commit cut to seven characters, as git shortens it,
/// because the whole forty wrapped onto a line of their own; what a machine reads keeps the whole commit.
/// </summary>
public class ShortVersionTests
{
    [Fact]
    public void TheCommitIsCutToSevenCharacters()
    {
        Assert.Equal("0.2.0-nightly.134+76c32eb", AppInfo.Shortened("0.2.0-nightly.134+76c32ebc0123456789abcdef0123456789abcdef"));
        Assert.Equal("0.2.0-nightly.134+76c32eb", AppInfo.Shortened("0.2.0-nightly.134+76c32eb"));
        Assert.Equal("1.0.0", AppInfo.Shortened("1.0.0"));
        Assert.Equal(AppInfo.Shortened(AppInfo.Version), AppInfo.ShortVersion);
    }

    [AvaloniaFact]
    public void SettingsShowsTheShortForm()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-shortversion-{Guid.NewGuid():N}");
        try
        {
            if (Phone.Platform is null)
            {
                Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
            }

            var view = new SettingsView(new AppSettingsStore(Path.Combine(folder, "settings.json")));
            var window = new Window { Width = 412, Height = 915, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var texts = view.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            // Entry 488: the version as a person reads it, "GroupLab 1.0, build 226", with the exact short form under it for a report.
            Assert.Contains("GroupLab " + AppInfo.Words, texts);
            Assert.Contains(AppInfo.ShortVersion, texts);
            Assert.Contains(GroupLab.Core.Updates.ActiveDevelopment.Sentence, texts);
            if (AppInfo.ShortVersion != AppInfo.Version)
            {
                Assert.DoesNotContain(texts, t => t.Contains(AppInfo.Version, StringComparison.Ordinal));
            }

            window.Close();
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
