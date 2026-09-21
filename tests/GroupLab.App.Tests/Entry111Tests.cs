using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 111 section 3: four small things on the screens. No enum's name reaches a person, each "why" sits beside
/// the item it explains, the Load block's labels stay at the top of their rows, and the shot count is stated once.
/// </summary>
public partial class Entry111Tests
{
    /// <summary>
    /// Every enum member in GroupLab whose name joins two or more words, "CalledFlyer" or "OneToOne", which is how an enum name looks when it
    /// reaches a person. A name of one word is an ordinary word and is not checked.
    /// </summary>
    private static List<string> CompoundEnumNames() =>
    [
        .. new[] { typeof(MarkingSession).Assembly, typeof(MainWindow).Assembly, typeof(GroupLab.Cli.TrajectoryVerb).Assembly }
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsEnum && t.Namespace?.StartsWith("GroupLab", StringComparison.Ordinal) == true)
            .SelectMany(Enum.GetNames)
            .Where(n => Compound().IsMatch(n))
            .Distinct(),
    ];

    /// <summary>Every piece of text the window shows: its texts, and the strings its lists offer.</summary>
    private static IEnumerable<string> Words(MainWindow window) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")
            .Concat(window.GetLogicalDescendants().OfType<ComboBox>().SelectMany(c => c.ItemsSource?.OfType<string>() ?? []))
            .Concat(window.GetLogicalDescendants().OfType<ContentControl>().Select(c => c.Content as string ?? ""));

    /// <summary>
    /// Entry 111 section 3: "CalledFlyer" reached the exclusion list as "OneToOne" once reached the scale's summary. This walks every screen,
    /// a shot excluded and selected, every stage of the timeline, the analysis with every "why" open, and the settings, and fails on any enum
    /// name in anything a person reads.
    /// </summary>
    [AvaloniaFact]
    public void NoEnumNameReachesAPerson()
    {
        var names = CompoundEnumNames();
        Assert.Contains("CalledFlyer", names);
        Assert.Contains("OneToOne", names);
        var (window, path, result) = Entry109Tests.Sheet();
        try
        {
            var found = new List<string>();
            void Check(string screen)
            {
                Dispatcher.UIThread.RunJobs();
                foreach (string text in Words(window))
                {
                    foreach (string name in names.Where(n => Regex.IsMatch(text, $@"\b{n}\b")))
                    {
                        found.Add($"{screen}: \"{name}\" in \"{text}\"");
                    }
                }
            }

            int shot = window.Session.State.Shots[0].Id;
            window.Session.SetExclusion(shot, ExclusionReason.CalledFlyer);
            window.Canvas.Selected = shot;
            window.Session.SetCalibre(Calibre.Of(0.308));
            Check("marking");
            window.ShowTrace(result.Measurement.Trace);
            window.SetShowWork(true, remember: false);
            for (int i = 0; i < window.Stages.Count; i++)
            {
                window.ShowStage(i);
                Check($"stage {window.Stages[i].Stage}");
            }

            window.CalibreAnswered();
            window.Analyse();
            window.SetEveryWhy(true);
            Check("analysis");
            window.ShowSettings();
            Check("settings");

            // Entry 113 section 7: every screen entries 112 and 113 added, and the report's pages.
            window.ShowSessions();
            Check("session records");
            window.ShowLibrary();
            Check("target library");
            window.ShowBallistics();
            Check("ballistics");
            long saved = window.CurrentSession!.Value;
            long copy = window.Sessions!.Save(window.Sessions.Get(saved)! with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", Load = "Second load" });
            window.ChooseSession(saved, true);
            window.ChooseSession(copy, true);
            window.CompareChosen();
            Check("compare loads");
            foreach (var page in GroupLab.Core.Reporting.ReportWriter.Pages(window.BuildReport()))
            {
                foreach (string text in page.Items.OfType<GroupLab.Core.Rendering.TextRun>().Select(t => t.Text))
                {
                    found.AddRange(names.Where(n => Regex.IsMatch(text, $@"\b{n}\b")).Select(n => $"report: \"{n}\" in \"{text}\""));
                }
            }

            Assert.True(found.Count == 0, string.Join(Environment.NewLine, found.Distinct()));

            window.ShowCompare(false);
            window.BackToEditor();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(Words(window), t => t.Contains("excluded as called flyer", StringComparison.Ordinal));
            Assert.Contains(window.GetLogicalDescendants().OfType<ComboBox>().SelectMany(c => c.ItemsSource?.OfType<string>() ?? []), t => t == "Called flyer");
            window.Close();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    /// <summary>
    /// Entry 111 section 3: each "why" sits beside the item it explains, in the item's own row, and opens beneath it, so it takes no row of its
    /// own. The Load block's labels sit at the top of their rows, and the shot count is stated once.
    /// </summary>
    [AvaloniaFact]
    public void EachWhySitsBesideItsItemAndTheCountIsStatedOnce()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var whys = window.GetLogicalDescendants().OfType<Button>().Where(b => b.Classes.Contains(AppStyles.Why) && Entry109Tests.Shown(b)).ToList();
            Assert.True(whys.Count >= 4, $"{whys.Count} whys on the analysis screen");
            foreach (var why in whys)
            {
                // The why shares a row with its item and nothing else.
                var row = Assert.IsType<Grid>(why.Parent);
                Assert.Equal(2, row.Children.Count);
                Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, why.HorizontalAlignment);
                Assert.True(row.Children[0].Margin.Right >= 40, "the item leaves no room for its why");
            }

            var calibre = window.GetLogicalDescendants().OfType<TextBlock>().Single(t => t.Text == "Calibre" && Entry109Tests.Shown(t));
            Assert.Equal(Avalonia.Layout.VerticalAlignment.Top, calibre.VerticalAlignment);

            var text = window.StatisticsText.ToList();
            Assert.DoesNotContain("Shots", text);
            Assert.Single(text, t => Regex.IsMatch(t, @"^\d+ shots: "));
            window.Close();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [GeneratedRegex("^([A-Z][a-z0-9]+){2,}$")]
    private static partial Regex Compound();
}
