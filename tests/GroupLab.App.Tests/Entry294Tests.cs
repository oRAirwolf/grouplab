using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 294 section 1 on the desktop: the first run asks whether the scope is in mil or MOA and keeps the answer, a
/// session in mil shows no MOA in any aiming figure unless asked, a rifle's scope unit overrides Settings, and Settings says it at the top.
/// </summary>
public class Entry294Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void TheFirstRunAsksTheScopeUnitAndKeepsTheAnswer()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry294-{Guid.NewGuid():N}");
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        try
        {
            Assert.Equal(ScopeAnswer.Unset, store.LoadScopeAnswer());
            var window = new MainWindow(store) { Width = 1400, Height = 900, AskScope = true };
            window.Show();
            window.ShowFirstRunIfDue();
            Assert.True(window.FirstRunShown);
            var said = window.FirstRunCard.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            Assert.Contains(UnitSettings.ScopeQuestion, said);

            window.PressSend("Sizes in inches");
            window.PressSend("Mil");
            Settle();
            Assert.False(window.FirstRunShown);
            Assert.Equal(AngularUnit.Mrad, window.Units.Angular);
            Assert.Equal(LinearUnit.Inch, window.Units.Linear);
            window.Close();

            // Kept: the store says so, and the next window does not ask again.
            var reopened = new AppSettingsStore(Path.Combine(folder, "settings.json"));
            Assert.Equal(ScopeAnswer.Mil, reopened.LoadScopeAnswer());
            Assert.Equal(AngularUnit.Mrad, reopened.LoadUnits().Angular);
            var again = new MainWindow(reopened) { Width = 1400, Height = 900, AskScope = true };
            again.Show();
            again.ShowFirstRunIfDue();
            Assert.False(again.FirstRunShown);
            again.Close();

            // "Both" keeps the unit Settings has and is remembered as both.
            reopened.SaveScopeAnswer(ScopeAnswer.Both, LinearUnit.Millimetre);
            Assert.Equal(ScopeAnswer.Both, reopened.LoadScopeAnswer());
            Assert.Equal(AngularUnit.Mrad, reopened.LoadUnits().Angular);
            Assert.Equal(DistanceUnit.Metre, reopened.LoadUnits().Distance);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    /// <summary>The value text of every aiming figure the analysis page shows: the figures, and the zero block.</summary>
    private static List<string> Aiming(MainWindow window) =>
        [.. window.KeptFigures.Select(k => k.Value), .. window.KeptBeneath, .. window.ZeroBlockText];

    [AvaloniaFact]
    public void AMilSessionShowsNoMoaUnlessAskedAndTheRifleWins()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetShotDistance(3600);
            window.SetUnits(UnitSettings.Imperial with { Angular = AngularUnit.Mrad });
            window.Analyse();
            Settle();
            Assert.Contains(Aiming(window), t => t.EndsWith(" mil", StringComparison.Ordinal));
            Assert.DoesNotContain(Aiming(window), t => t.Contains("MOA", StringComparison.Ordinal));

            // Settings in MOA, the session's rifle in mil: the rifle wins, and the clicks are its own.
            window.SetUnits(UnitSettings.Imperial);
            window.Session.SetEquipment(new Rifle("Comp rifle", 0.1, AngularUnit.Mrad), null, null);
            Settle();
            Assert.Equal(AngularUnit.Mrad, window.Aim.Angular);
            Assert.DoesNotContain(Aiming(window), t => t.Contains("MOA", StringComparison.Ordinal));
            Assert.Contains(Aiming(window), t => t.EndsWith(" mil", StringComparison.Ordinal));

            // And an MOA hunting rifle in a mil setting reads in MOA alone.
            window.SetUnits(UnitSettings.Imperial with { Angular = AngularUnit.Mrad });
            window.Session.SetEquipment(new Rifle("Hunting rifle", 0.25, AngularUnit.Moa), null, null);
            Settle();
            Assert.DoesNotContain(window.ZeroBlockText, t => t.EndsWith(" mil", StringComparison.Ordinal));
            Assert.Contains(window.ZeroBlockText, t => t.EndsWith(" MOA", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void SettingsSaysTheScopeUnitAtTheTopOfUnits()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.ShowSettings();
            Settle();
            var said = window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            int scope = said.IndexOf(UnitSettings.ScopeUnitLabel);
            Assert.True(scope > said.IndexOf("Units"));
            Assert.Contains(UnitSettings.ScopeUnitSays, said);
            Assert.True(scope < said.IndexOf("Lengths"));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
