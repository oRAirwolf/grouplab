using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>NOTES-FROM-PLANNING.md entry 113: the queue after entry 112, driven headlessly against the scanned-sheet fixture of entry 109.</summary>
public class Entry113Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>
    /// Entry 113 section 1: the thumbnail draws the sheet from its definition, every bull and every shot on it, and a click on a bull selects
    /// its shot on the plot as well; the full CEP table and bivariate fit sit behind one disclosure that remembers it was opened, and give the
    /// plot's own circular CEP.
    /// </summary>
    [AvaloniaFact]
    public void TheThumbnailSelectsByBullAndTheFullFiguresPanelRemembersItWasOpened()
    {
        var (window, path, result) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            Settle();

            var thumbnail = window.Thumbnail;
            Assert.True(thumbnail.IsVisible);
            Assert.NotNull(thumbnail.Artwork);
            Assert.Equal(result.Definition!.Bulls.Count, thumbnail.Bulls.Count);
            Assert.Equal(window.Session.State.Shots.Count(s => s.IsShot && s.Bull is not null), thumbnail.Shots.Count);
            Assert.Equal(8.5, thumbnail.PageWidthInches, 6);

            var shot = window.Session.State.Shots.First(s => s.IsShot && s.Bull is not null && result.Definition.Bulls[s.Bull.Value].Scoring);
            var picked = thumbnail.ClickBull(shot.Bull!.Value);
            Settle();
            Assert.Equal([shot.Id], picked);
            Assert.Contains(shot.Id, window.Plot.Selected);
            Assert.Contains(shot.Id, thumbnail.Selected);

            var panel = window.FullFiguresPanel;
            Assert.True(panel.IsVisible);
            var text = window.FullFiguresText.ToList();
            Assert.Contains("Grubbs-Patnaik", text);
            Assert.Contains(text, t => t.StartsWith("correlation across with up ", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith(UnitSettings.Imperial.Number(window.Plot.Cep50Inches!.Value) + " (", StringComparison.Ordinal));
            Assert.DoesNotContain(text, t => t.StartsWith("Without exclusions", StringComparison.Ordinal));

            window.BackToEditor();
            window.Session.SetExclusion(shot.Id, ExclusionReason.CalledFlyer);
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            Assert.Contains(window.FullFiguresText, t => t.StartsWith("Without exclusions, ", StringComparison.Ordinal));

            Assert.False(panel.IsExpanded);
            panel.IsExpanded = true;
            Settle();
            Assert.True(window.SettingsStore.LoadWhyOpen("full-figures"));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Entry 113 section 2: two sessions chosen in Session records compare side by side, in the order chosen, with the tests, their verdicts and
    /// what each could have detected; identical groups are said not to be separated. Sessions at different distances are compared as angles and
    /// the screen says so; and a sheet's subgroups compare the same way.
    /// </summary>
    [AvaloniaFact]
    public void ChosenSessionsAndSubgroupsCompareWithoutRankingByPointEstimate()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa), null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            long first = window.CurrentSession!.Value;
            long second = window.Sessions!.Save(window.Sessions.Get(first)! with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", ShotDate = "2099-01-01", Load = "Factory 150" });

            window.ShowSessions();
            Settle();
            Assert.Equal(2, window.GetLogicalDescendants().OfType<CheckBox>().Count(c => c.IsVisible && (Avalonia.Automation.AutomationProperties.GetName(c) ?? "").StartsWith("Compare the session", StringComparison.Ordinal)));
            window.ChooseSession(first, true);
            window.ChooseSession(second, true);
            window.CompareChosen();
            Settle();
            Assert.True(window.ShowingCompare);
            var report = window.Comparison!;
            Assert.Equal(["H4350 41.5", "Factory 150"], report.Groups.Select(g => g.Name));
            Assert.Equal("These two loads are not distinguishable on this evidence.", report.Headline);
            var text = window.CompareText.ToList();
            Assert.Contains(report.Headline, text);
            Assert.Contains(text, t => t.StartsWith("The sigma intervals overlap, so the data do not separate them.", StringComparison.Ordinal));
            Assert.All(report.Tests, t => Assert.Contains(t.Power, text));
            Assert.Contains("Tests run", text);
            Assert.Contains("10 percent", text);

            // At different distances the groups are put on one footing as angles, and the screen says so.
            window.Sessions.Save(window.Sessions.Get(second)! with { DistanceInches = 7200 });
            window.CompareChosen();
            Settle();
            Assert.Contains(window.CompareText, t => t.Contains("compared as angles", StringComparison.Ordinal));
            Assert.Equal(report.Groups[1].Rayleigh.Sigma.Value / 2, window.Comparison!.Groups[1].Rayleigh.Sigma.Value, 9);

            // A sheet's subgroups compare the same way.
            window.ShowCompare(false);
            foreach (var bull in window.Session.State.Bulls.Where(b => b.Scoring))
            {
                window.Session.SetSubgroup(bull.Index, bull.Index < 12 ? "41.5 gr" : "42.1 gr");
            }

            window.CompareSubgroups();
            Settle();
            Assert.Equal(["41.5 gr", "42.1 gr"], window.Comparison!.Groups.Select(g => g.Name));

            // Entry 131 section 10: velocity and SD on the cards where the record book knows them, and nothing on the card where it does not.
            Assert.DoesNotContain(window.CompareText, t => t.Contains("ft/s", StringComparison.Ordinal));
            window.Book = window.Book.With(new GroupLab.Core.Marking.Load("41.5 gr", null)
            {
                MuzzleVelocityFps = 2850,
                MuzzleVelocitySdFps = 11,
                MuzzleVelocitySdFrom = "24 readings, 20 September 2026",
            });
            window.CompareSubgroups();
            Settle();
            Assert.Contains(window.CompareText, t => t == "2850 ft/s, SD 11 ft/s from 24 readings, 20 September 2026");

            // The card follows the units in force, as every other figure on the screen does.
            window.SetUnits(GroupLab.Core.Marking.UnitSettings.Metric);
            window.CompareSubgroups();
            Settle();
            Assert.Contains(window.CompareText, t => t.StartsWith("869 m/s, SD 3 m/s", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Entry 113 section 3 on screen: the analysed group carried to 600 yd is labelled a prediction, gives its sigma with the interval's ends and
    /// the chance of a hit as a range; with no velocity or wind spread it says it is angular scaling and nothing more; and a velocity SD too
    /// large for the group is refused.
    /// </summary>
    [AvaloniaFact]
    public void TheAnalysedGroupCarriesToAnotherDistanceAsAPrediction()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var rifle = new Rifle("Tikka T3x", 0.25, AngularUnit.Moa) { SightHeightInches = 1.75, ZeroDistanceYards = 100 };
            var load = new Load("H4350 41.5", null)
            {
                MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = GroupLab.Core.Ballistics.DragModel.G7,
                BcReference = GroupLab.Core.Ballistics.ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
            };
            window.Book = RecordBook.Empty.With(rifle).With(load);
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(rifle, null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            window.ShowBallistics();
            Settle();

            window.ProjectGroup("600", 0, "4", "4", "2");
            var text = window.ProjectionText.ToList();
            Assert.Contains("Predicted at 600 yd, not measured", text);
            Assert.Contains(text, t => t.StartsWith("Sigma across ", StringComparison.Ordinal) && t.Contains(" to ", StringComparison.Ordinal));
            Assert.Contains(text, t => t.StartsWith("Of that, the load's velocity SD of 10 ft/s gives ", StringComparison.Ordinal));
            var hit = Assert.Single(text, t => t.StartsWith("Chance of a hit on a 4.000 in circle at 600 yd: ", StringComparison.Ordinal));
            Assert.Matches(@": \d+ percent, between \d+ and \d+ percent across the sigma interval", hit);
            Assert.Contains("Aerodynamic jump is not modelled.", text);

            window.Book = window.Book.With(load with { MuzzleVelocitySdFps = null });
            window.ProjectGroup("600", 1, "6", "12", "");
            text = [.. window.ProjectionText];
            Assert.Contains("No velocity SD or crosswind uncertainty is given, so this is the group scaled by angle and nothing more.", text);
            Assert.Contains(text, t => t.StartsWith("Chance of a hit on a 6.000 by 12.000 in rectangle at 600 yd: ", StringComparison.Ordinal));

            window.Book = window.Book.With(load with { MuzzleVelocitySdFps = 500 });
            window.ProjectGroup("600", 0, "4", "4", "");
            Assert.Contains(window.ProjectionText, t => t.Contains("too large for this group", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Entry 113 section 4: the editor's way to say a sheet is not one shot a bull. Two on the bulls named reads bull labels and ranges, a name
    /// that is no bull is refused, nearest bull is a rule of its own, and the default takes the rule away.
    /// </summary>
    [AvaloniaFact]
    public void TheEditorSaysHowManyShotsEachBullHolds()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.SetShotsPerBull(2, "1-10");
            var rule = window.Session.State.Rule!;
            Assert.False(rule.NearestOnly);
            Assert.Equal(10, rule.PerBull.Count);
            Assert.All(rule.PerBull.Values, v => Assert.Equal(2, v));

            window.SetShotsPerBull(2, "1-10, 99");
            Assert.Equal(10, window.Session.State.Rule!.PerBull.Count);
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Name the bulls holding two shots by their numbers, such as 1-10 or 1, 3, 5.");

            window.SetShotsPerBull(1, "");
            Assert.True(window.Session.State.Rule!.NearestOnly);
            window.SetShotsPerBull(0, "");
            Assert.Null(window.Session.State.Rule);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Entry 113 section 5: the print screen offers the volunteer pack, the sheet and its instruction page in one PDF.</summary>
    [AvaloniaFact]
    public void ThePrintScreenPrintsAVolunteerPack()
    {
        var print = new PrintWindow();
        print.Show();
        print.Select("GL-CF25-LTR.gltd.json");
        Settle();
        Assert.Contains(print.GetLogicalDescendants().OfType<Button>(), b => Equals(b.Content, "Print a volunteer pack"));
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-pack-{Guid.NewGuid():N}.pdf");
        try
        {
            Assert.True(print.SaveVolunteerPack(path));
            string pdf = System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(path));
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(pdf, "/Type /Page /Parent").Count);
            Assert.Contains("pissinhot.com/targets", pdf, StringComparison.Ordinal);
            Assert.StartsWith("Saved the volunteer pack, 2 pages", print.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
            print.Close();
        }
    }

    /// <summary>
    /// Entry 113 section 7: every screen entries 112 and 113 added, in all four themes. Every visible text there takes its colour from its
    /// theme's text roles, which ThemeTests holds to their contrast ratios on every surface; so no new screen sets a colour the contrast tests
    /// never saw. And every control on them a person operates can take the keyboard's focus.
    /// </summary>
    [AvaloniaFact]
    public void EveryNewScreenUsesItsThemesTestedColoursAndTakesTheKeyboard()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var rifle = new Rifle("Tikka T3x", 0.25, AngularUnit.Moa) { SightHeightInches = 1.75, ZeroDistanceYards = 100 };
            var load = new Load("H4350 41.5", null)
            {
                MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = GroupLab.Core.Ballistics.DragModel.G7,
                BcReference = GroupLab.Core.Ballistics.ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
            };
            window.Book = RecordBook.Empty.With(rifle).With(load);
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(rifle, null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            long first = window.CurrentSession!.Value;
            long second = window.Sessions!.Save(window.Sessions.Get(first)! with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", Load = "Second load" });
            window.ChooseSession(first, true);
            window.ChooseSession(second, true);
            window.FullFiguresPanel.IsExpanded = true;

            var failures = new List<string>();
            var unfocusable = new List<string>();
            int checkedTexts = 0;
            foreach (var theme in new[] { ThemeChoice.Dark, ThemeChoice.Light, ThemeChoice.HighContrast, ThemeChoice.System })
            {
                window.SetTheme(theme);
                Settle();
                var palette = GroupLab.App.Theme.Tokens.For(window.ActualThemeVariant);
                var allowed = palette.TextColours.Select(c => c.Colour).Append(palette.OnAmber).ToHashSet();
                foreach (var (screen, show) in new (string, Action)[]
                {
                    ("analysis", () => window.ShowSessions(false)),
                    ("session records", () => window.ShowSessions()),
                    ("target library", () => window.ShowLibrary()),
                    ("ballistics", () => { window.ShowBallistics(); window.ProjectGroup("600", 0, "4", "4", "2"); }),
                    ("compare loads", window.CompareChosen),
                })
                {
                    show();
                    Settle();
                    foreach (var text in window.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text)))
                    {
                        if (text.Foreground is Avalonia.Media.ISolidColorBrush { Color: var colour } && ++checkedTexts > 0 && !allowed.Contains(colour))
                        {
                            failures.Add($"{theme} {screen}: \"{text.Text}\" in {colour}");
                        }
                    }

                    if (theme == ThemeChoice.Dark)
                    {
                        unfocusable.AddRange(window.GetLogicalDescendants().OfType<Control>()
                            .Where(c => c.IsEffectivelyVisible && c is Button or CheckBox or ComboBox or TextBox && (!c.Focusable || !KeyboardNavigation.GetIsTabStop(c)))
                            .Select(c => $"{screen}: {c.GetType().Name} {(c as ContentControl)?.Content}"));
                    }
                }
            }

            Assert.True(checkedTexts > 500, $"only {checkedTexts} texts had a colour to check");
            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Distinct().Take(40)));
            Assert.True(unfocusable.Count == 0, string.Join(Environment.NewLine, unfocusable.Distinct()));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
