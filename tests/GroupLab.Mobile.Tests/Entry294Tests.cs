using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 294 section 1 on the phone: the first run asks whether the scope is in mil or MOA and keeps the answer, the
/// aiming screens in mil show no MOA unless asked, a rifle's scope unit overrides Settings, and Settings starts with the scope unit.
/// </summary>
public class Entry294Tests
{
    private static readonly PointD[] Group = [new(0.3, 0.1), new(0.1, -0.2), new(0.5, 0.2), new(0.2, 0.3), new(0.4, -0.1), new(0.25, 0.05)];

    private static readonly UnitSettings Mil = UnitSettings.Imperial with { Angular = AngularUnit.Mrad };

    /// <summary>A group 2 in right and 3 in low of its aim at 100 yd.</summary>
    private static MarkingState Off(Rifle? rifle) => ShotCsv.Marking([.. Group.Select(p => new PointD(p.X + 2, p.Y + 3))], 3600) with { Rifle = rifle };

    private static List<string> Words(Control root)
    {
        var window = new Window { Width = 412, Height = 915, Content = root };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var said = root.GetLogicalDescendants().OfType<TextBlock>()
            .Select(t => t.Text ?? string.Concat(t.Inlines?.OfType<Run>().Select(r => r.Text) ?? [])).ToList();
        window.Close();
        return said;
    }

    private static AppSettingsStore Fresh(out string folder)
    {
        folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry294-{Guid.NewGuid():N}");
        return new AppSettingsStore(Path.Combine(folder, "settings.json"));
    }

    [AvaloniaFact]
    public void TheFirstRunAsksTheScopeUnitAndKeepsTheAnswer()
    {
        var store = Fresh(out string folder);
        try
        {
            Assert.True(FirstRunView.ScopeDue(store));
            bool done = false;
            var first = new FirstRunView(store, () => done = true);
            var window = new Window { Width = 412, Height = 915, Content = first };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(first.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == UnitSettings.ScopeQuestion);
            var mil = first.GetLogicalDescendants().OfType<Button>().First(b => b.Content is TextBlock { Text: "Mil" });
            mil.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            window.Close();

            Assert.Equal(ScopeAnswer.Mil, new AppSettingsStore(store.Path).LoadScopeAnswer());
            Assert.Equal(AngularUnit.Mrad, new AppSettingsStore(store.Path).LoadUnits().Angular);
            Assert.False(FirstRunView.ScopeDue(store));
            Assert.True(done || FirstRunView.Due(store));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [AvaloniaFact]
    public void ZeroFromThisGroupInMilSaysNoMoaAndTheRifleWins()
    {
        var said = Words(new ZeroFromPage(Off(null), Mil, () => { }, () => { }));
        Assert.Contains(said, s => s.Contains(" mil ", StringComparison.Ordinal));
        Assert.DoesNotContain(said, s => s.Contains("MOA", StringComparison.Ordinal));

        // Settings in MOA, the rifle in mil: the rifle's unit, and its clicks.
        var rifle = Words(new ZeroFromPage(Off(new Rifle("Comp rifle", 0.1, AngularUnit.Mrad)), UnitSettings.Imperial, () => { }, () => { }));
        Assert.DoesNotContain(rifle, s => s.Contains("MOA", StringComparison.Ordinal));
        Assert.Contains(rifle, s => s.Contains("clicks", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void TheFiguresFollowTheRiflesScopeUnit()
    {
        var state = Off(new Rifle("Comp rifle", 0.1, AngularUnit.Mrad));
        var figures = new FiguresView(state, UnitSettings.Imperial, new CompositePlot(), null);
        figures.Show(state, UnitSettings.Imperial);
        var said = Words(figures);
        Assert.DoesNotContain(said, s => s.Contains("MOA", StringComparison.Ordinal) && char.IsDigit(s[0]));
        Assert.Contains(said, s => s.Contains(" mil", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void ShotsNeededToZeroOffersTheScopeUnitsClickFirst()
    {
        var page = new ShotsToZeroPage(Off(null), Mil, () => { });
        var window = new Window { Width = 412, Height = 915, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var chosen = page.GetLogicalDescendants().OfType<Button>().Where(b => b.Classes.Contains(GroupLab.App.Theme.AppStyles.Chosen))
            .Select(b => b.Content as string).ToList();
        Assert.Contains("0.1 mil", chosen);
        Assert.Contains(page.GetLogicalDescendants().OfType<Button>(), b => b.Content as string == "0.05 mil");
        window.Close();
    }

    [AvaloniaFact]
    public void SettingsStartsWithTheScopeUnit()
    {
        var store = Fresh(out string folder);
        try
        {
            store.SaveUnits(Mil);
            if (Phone.Platform is null)
            {
                Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
            }

            var said = Words(new SettingsView(store));
            int scope = said.IndexOf(UnitSettings.ScopeUnitLabel);
            Assert.True(scope >= 0 && scope < said.IndexOf("Printers"));
            Assert.Contains(UnitSettings.ScopeUnitSays, said);
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
