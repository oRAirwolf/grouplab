using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;
using GroupLab.Mobile.Dev;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// Entry 474 section 2: the pages under the phone's places, which the walk of the places (<see cref="Entry462PhoneLayoutTests"/>) does not
/// open: every Settings page, Sync between devices, and the rifle and load forms with their Delete question. Each at the narrowest width
/// and at the largest text, in both themes, laid out whole and with every control named for a screen reader and large enough to touch.
/// </summary>
public class PagesUnderPlacesAuditTests
{
    private static void Press(Control within, string id)
    {
        var button = within.GetLogicalDescendants().OfType<Button>().First(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b) == id);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void EveryPageUnderAPlaceIsWholeAndNamedAtTheLargestText()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        var store = PhoneAnalysis.Store();
        var before = store.LoadBook();
        store.SaveBook(RecordBook.Empty.With(new Rifle("Sample rifle, 6.5 Creedmoor", 0.1, AngularUnit.Mrad)).With(new Barrel("Its barrel", "Sample rifle, 6.5 Creedmoor", 0))
            .With(new Load("Load A", null)));
        var all = new List<string>();
        try
        {
            foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                var shell = new Shell();
                var window = new Window { Width = 360, Height = 780, RequestedThemeVariant = theme, Content = shell };
                AndroidText.LargestUntilClosed(window);
                window.Show();
                Dispatcher.UIThread.RunJobs();
                void Audit(string where)
                {
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    Dispatcher.UIThread.RunJobs();
                    where += ", " + theme;
                    all.AddRange(LayoutAudit.Findings(window).Select(f => where + ": " + f));
                    all.AddRange(ScreenReaderAudit.Findings(window, null, smallestTarget: 48).Select(f => where + ": " + f));
                }

                try
                {
                    foreach (var category in SettingsMenu.All)
                    {
                        shell.ShowSettings(category);
                        Audit("Settings, " + category);
                    }

                    shell.ShowSync();
                    Audit("Sync between devices");

                    foreach (string form in new[] { "rifle", "load" })
                    {
                        var page = new BallisticsPage(openForm: form);
                        window.Content = page;
                        Audit($"Ballistics, the {form}");
                        Press(page, $"ballistics-{form}-delete");
                        Audit($"Ballistics, the {form}'s Delete question");
                    }
                }
                finally
                {
                    window.Close();
                }
            }
        }
        finally
        {
            store.SaveBook(before);
        }

        Assert.True(all.Count == 0, all.Count + " findings: " + string.Join("; ", all.Take(40)));
    }
}
