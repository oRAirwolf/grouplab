using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Theme;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 413, Alan chose B: at the text sizes where a place's name would not fit its fifth of the bar along the
/// bottom, the bar shows its five icons alone, larger, all at once; below that it is as it was. Each tab keeps its name for TalkBack and
/// stays a thumb high, the current place is still marked, and the bar changes back and forth with the setting while GroupLab is open.
/// </summary>
public class Entry413Tests
{
    private static readonly string[] Places = ["Capture", "Sessions", "Ballistics", "Targets", "Settings"];

    private static (Window Window, NavBar Bar) Shown(double width)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var window = new Window { Width = width, Height = 640, Content = new Shell() };
        window.Show();
        ((Shell)window.Content).Show(Shell.Place.Targets);
        Dispatcher.UIThread.RunJobs();
        return (window, window.GetVisualDescendants().OfType<NavBar>().Single());
    }

    private static List<Button> Tabs(NavBar bar) => bar.Children.OfType<Button>().ToList();

    private static List<TextBlock> Names(NavBar bar) =>
        Tabs(bar).Select(t => t.GetVisualDescendants().OfType<TextBlock>().Single(b => b.Classes.Contains(PhoneStyles.NavLabel))).ToList();

    [AvaloniaFact]
    public void AtTheLargestSizeTheBarShowsItsIconsAloneAndComesBackWithItsNames()
    {
        var (window, bar) = Shown(360);
        try
        {
            Assert.False(bar.IconsOnly);
            Assert.All(Names(bar), n => Assert.True(n.IsVisible));
            Assert.Equal(Places, Names(bar).Select(n => n.Text));

            TextSize.Follow(AndroidText.Largest);
            Dispatcher.UIThread.RunJobs();
            Assert.True(bar.IconsOnly);
            Assert.All(Names(bar), n => Assert.False(n.IsVisible));
            Assert.Equal(Places, Tabs(bar).Select(AutomationProperties.GetName));
            Assert.Equal(Places, Tabs(bar).Select(t => ToolTip.GetTip(t) as string));
            Assert.All(Tabs(bar), t => Assert.True(t.Bounds.Height >= 48 && t.Bounds.Width + t.Margin.Left + t.Margin.Right >= bar.Bounds.Width / 5 - 1, $"{t.Bounds}"));
            Assert.All(Tabs(bar), t => Assert.Equal(NavBar.LoneIcon, t.GetVisualDescendants().OfType<PathIcon>().Single().Bounds.Width));
            Assert.Equal("tab-targets", AutomationProperties.GetAutomationId(Tabs(bar).Single(t => t.Classes.Contains(PhoneStyles.On))));
            var pill = Tabs(bar)[0].GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains(PhoneStyles.NavPill));
            Assert.Equal((60.0, 40.0), (pill.Bounds.Width, pill.Bounds.Height));

            // Live, without a restart: back to the plain size, the names come back and the icons go back to their size.
            TextSize.Follow(size => size);
            Dispatcher.UIThread.RunJobs();
            Assert.False(bar.IconsOnly);
            Assert.All(Names(bar), n => Assert.True(n.IsVisible));
            Assert.All(Tabs(bar), t => Assert.Equal(NavBar.NamedIcon, t.GetVisualDescendants().OfType<PathIcon>().Single().Bounds.Width));
            Assert.Equal(Places, Tabs(bar).Select(AutomationProperties.GetName));
        }
        finally
        {
            TextSize.Follow(size => size);
            window.Close();
        }
    }

    /// <summary>The names are measured against the bar's own width, so a screen wide enough keeps them at the largest size.</summary>
    [AvaloniaFact]
    public void AWideScreenKeepsItsNamesAtTheLargestSize()
    {
        var (window, bar) = Shown(900);
        try
        {
            TextSize.Follow(AndroidText.Largest);
            Dispatcher.UIThread.RunJobs();
            Assert.False(bar.IconsOnly);
            Assert.All(Names(bar), n => Assert.True(n.IsVisible && n.Bounds.Width > 0));
        }
        finally
        {
            TextSize.Follow(size => size);
            window.Close();
        }
    }
}
