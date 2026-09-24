using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 112: session records in the database DESIGN.md section 15 names, the report, the target library and the
/// solver on screen. These drive the window headlessly against the scanned-sheet fixture of entry 109.
/// </summary>
public class Entry112Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static Button Named(Control root, string content) =>
        root.GetLogicalDescendants().OfType<Button>().First(b => Equals(b.Content, content) && Entry109Tests.Shown(b));

    /// <summary>
    /// Entry 112 section 1: Accept and analyse saves the session, with the marking and its figures, and a second Accept on the same marking
    /// updates it rather than adding another. Reopening returns to the analysis with the marking as it was saved, edits and exclusions included.
    /// </summary>
    [AvaloniaFact]
    public void AcceptSavesTheSessionAndReopeningReturnsToItsAnalysis()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa), "Bartlein 26", "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            long id = Assert.IsType<long>(window.CurrentSession);
            var saved = window.Sessions!.Get(id)!;
            Assert.Equal("GroupLab 5x5 Load Development, Letter", saved.SheetName);
            Assert.Equal("Tikka T3x", saved.Rifle);
            Assert.Equal(3600, saved.DistanceInches);
            Assert.NotNull(saved.MeanRadiusInches);
            Assert.True(saved.MeanRadiusLowerInches < saved.MeanRadiusInches && saved.MeanRadiusInches < saved.MeanRadiusUpperInches);
            Assert.Equal(path, saved.ImagePath);
            Assert.Equal(64, saved.ImageSha256!.Length);
            Assert.Equal("image/jpeg", saved.ProofImageType);
            Assert.InRange(saved.ProofImage!.Length, 10_000, 2_000_000);
            Assert.NotNull(saved.DefinitionJson);

            // An exclusion made after the first Accept is in the session once it is accepted again, and it is the same session.
            window.BackToEditor();
            int excluded = window.Session.State.Shots[0].Id;
            window.Session.SetExclusion(excluded, ExclusionReason.PulledShot);
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            Assert.Equal(id, window.CurrentSession);
            Assert.Single(window.Sessions.List());

            // Reopened after the marking has moved on: the saved marking comes back, with its exclusion, in the analysis state.
            window.BackToEditor();
            window.Session.SetExclusion(excluded, null);
            window.OpenSession(id);
            Settle();
            Assert.True(window.Analysing);
            Assert.Equal(ExclusionReason.PulledShot, window.Session.State.Find(excluded)!.Exclusion);
            Assert.StartsWith("Reopened the session of", window.StatusText, StringComparison.Ordinal);
            Assert.DoesNotContain("not where it was", window.StatusText, StringComparison.Ordinal);

            // No image is needed to reopen a session: the original moved, and the analysis still opens from the saved marking.
            string moved = path + ".moved";
            File.Move(path, moved);
            window.OpenSession(id);
            Settle();
            Assert.True(window.Analysing);
            Assert.Contains("not where it was", window.StatusText, StringComparison.Ordinal);
            Assert.NotEmpty(window.Plot.Discs);
            File.Move(moved, path);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Entry 112 section 1: the Session records screen lists every session newest first with its date, sheet, rifle, load, distance, shot
    /// count and mean radius with its interval; filters by rifle and by load; opens a session from its row; and asks before deleting one.
    /// </summary>
    [AvaloniaFact]
    public void SessionRecordsListFilterOpenAndDeleteAfterAsking()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa), null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            long first = window.CurrentSession!.Value;
            var copy = window.Sessions!.Get(first)! with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", ShotDate = "2099-01-01", Rifle = "Old Mauser", Load = "Factory 150" };
            long second = window.Sessions.Save(copy);

            window.ShowSessions();
            Settle();
            Assert.True(window.ShowingSessions);
            var rows = window.SessionRowTexts;
            Assert.Equal(2, rows.Count);
            Assert.StartsWith("2099-01-01 | GroupLab 5x5", rows[0], StringComparison.Ordinal);
            Assert.Contains("Tikka T3x | H4350 41.5 | 100 yd | 24 | ", rows[1], StringComparison.Ordinal);
            Assert.Matches(@"\d\.\d{3} in \(\d\.\d{3} to \d\.\d{3}\)$", rows[1]);

            window.FilterSessions("Old Mauser", null);
            Assert.Single(window.SessionRowTexts);
            window.FilterSessions(null, "H4350 41.5");
            Assert.Contains("Tikka T3x", Assert.Single(window.SessionRowTexts), StringComparison.Ordinal);
            window.FilterSessions(null, null);

            // Delete asks first; Keep it leaves the session, Delete removes it.
            var list = window.GetLogicalDescendants().OfType<ScrollViewer>().First(s => s.IsVisible && s.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == "Session records"));
            Named(list, "Delete").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Contains(list.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Delete this session?");
            Named(list, "Keep it").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Equal(2, window.Sessions.List().Count);
            Named(list, "Delete").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Named(list, "Delete").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Equal([first], window.Sessions.List().Select(s => s.Id));
            Assert.Null(window.Sessions.Get(second));

            // A row opens its session to the analysis.
            list.GetLogicalDescendants().OfType<Button>().First(b => b.Classes.Contains(AppStyles.TableRow)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.False(window.ShowingSessions);
            Assert.True(window.Analysing);
            Assert.Equal(first, window.CurrentSession);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Every line a report page carries, in order, with runs of whitespace made single so wrapped sentences can be found whole.</summary>
    private static string PageText(Scene page) =>
        string.Join(" ", page.Items.OfType<TextRun>().Select(t => t.Text)).Replace("  ", " ", StringComparison.Ordinal);

    private static string Flat(string text) => System.Text.RegularExpressions.Regex.Replace(ReportWriter.Plain(text), @"\s+", " ");

    private static IEnumerable<string> Strings(SessionReport report) =>
        new[] { report.Title, report.Plot.Caption }
            .Concat(report.Particulars.Concat(report.Identity).SelectMany(p => new[] { p.Label, p.Value }))
            .Concat(report.Summary).Concat(report.Figures.SelectMany(f => f.Details.Prepend(f.Value).Prepend(f.Label)))
            .Concat(report.Cards.Append(report.Zero).SelectMany(c => c.Evidence.Concat(c.Why).Prepend(c.Verdict)))
            .Concat(report.ShotHeadings).Concat(report.Shots.SelectMany(r => r.Cells)).Concat(report.Exclusions).Concat(report.Unmade)
            .Concat(report.Registration).Concat(report.Why.SelectMany(w => w.Lines.Prepend(w.Heading)));

    /// <summary>
    /// Entry 112 section 2: the report of a session with no exclusion and of the same session with one. Page 1 carries the particulars, the
    /// plot, the figures with intervals, the zero verdict and the two cards; page 2 the shot table, exclusions, unmade decisions, registration,
    /// every "why", and the version and identifiers. Each figure and card line is one the screen shows, word for word, so the paper says
    /// nothing the screen does not; with an exclusion every figure is given both ways and the excluded shot is struck through, never dropped;
    /// the stringing power statement is printed wherever the screen prints it; and no character is lost to the PDF's encoding.
    /// </summary>
    [AvaloniaFact]
    public void TheReportPrintsTheScreenWithAndWithoutAnExclusion()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa), "Bartlein 26", "H4350 41.5");
            foreach (bool withExclusion in new[] { false, true })
            {
                window.BackToEditor();
                int first = window.Session.State.Shots[0].Id;
                window.Session.SetExclusion(first, withExclusion ? ExclusionReason.PulledShot : null);
                window.CalibreAnswered();
                window.Analyse();
                Settle();
                var report = window.BuildReport();
                var pages = ReportWriter.Pages(report);
                Assert.True(pages.Count >= 2);
                string one = PageText(pages[0]), rest = string.Join(" ", pages.Skip(1).Select(PageText));

                // Page 1: particulars, plot, figures, zero, cards. Page 2 onwards: the rest.
                foreach (string expected in new[] { "GroupLab 5x5 Load Development, Letter", "Tikka T3x", "Bartlein 26", "H4350 41.5", "100 yd", "Mean radius", "Sigma", "CEP 90", "Zero correction", "Shape", "Worst shot" })
                {
                    Assert.Contains(expected, one, StringComparison.Ordinal);
                }

                Assert.Contains("% interval", one, StringComparison.Ordinal);
                Assert.Contains(Flat(report.Zero.Verdict), Flat(one), StringComparison.Ordinal);
                Assert.StartsWith("Shots ", PageText(pages[1]), StringComparison.Ordinal);
                foreach (string expected in new[] { "Exclusions", "Decisions left unmade", "Registration", "Why", "GroupLab", "Session", "Sheet identifier" })
                {
                    Assert.Contains(expected, rest, StringComparison.Ordinal);
                }

                // Nothing on paper the screen does not say: every figure's lines and every card's verdict and evidence are on the screen as written.
                var screen = window.StatisticsText.Concat(window.JudgementCards.SelectMany(c => c)).Concat(window.ZeroText).ToHashSet();
                foreach (var figure in report.Figures.Where(f => f.Label != "Center from aim"))
                {
                    Assert.Contains(figure.Value, screen);
                    Assert.All(figure.Details, d => Assert.Contains(d, screen));
                }

                Assert.All(report.Cards.SelectMany(c => c.Evidence.Prepend(c.Verdict)), line => Assert.Contains(line, screen));
                Assert.Contains(report.Zero.Verdict, screen);

                // Every card's verdict and evidence is on page 1 whole, so the stringing power statement goes wherever a negative does.
                Assert.All(report.Cards.SelectMany(c => c.Evidence.Prepend(c.Verdict)), line => Assert.Contains(Flat(line), Flat(one), StringComparison.Ordinal));
                Assert.Contains(report.Cards.Single(c => c.Title == "Shape").Evidence, e => e.EndsWith("No evidence is not evidence of none.", StringComparison.Ordinal) || e.Contains("beyond what chance gives", StringComparison.Ordinal));

                Assert.All(Strings(report), text => Assert.True(ReportWriter.Printable(text), text));

                var withBoth = report.Figures.Where(f => f.Label != "Center from aim").ToList();
                if (withExclusion)
                {
                    Assert.All(withBoth, f => Assert.Contains(f.Details, d => d.StartsWith("without exclusions: ", StringComparison.Ordinal)));
                    Assert.Contains("Pulled shot", Assert.Single(report.Exclusions), StringComparison.Ordinal);
                    Assert.Equal("excluded: pulled shot", Assert.Single(report.Shots, r => r.Struck).Cells[^1]);
                    Assert.Single(report.Plot.Shots, s => s.Excluded);
                    Assert.Contains("without exclusions", one, StringComparison.Ordinal);
                }
                else
                {
                    Assert.All(withBoth, f => Assert.DoesNotContain(f.Details, d => d.Contains("without exclusions", StringComparison.Ordinal)));
                    Assert.Empty(report.Exclusions);
                    Assert.DoesNotContain(report.Shots, r => r.Struck);
                    Assert.Contains("None. Every shot counts.", rest, StringComparison.Ordinal);
                }

                Assert.All(report.Shots, r => Assert.NotEqual("", r.Cells[1]));
                Assert.Equal(report.Shots.Count, report.Plot.Shots.Count);

                string pdf = Path.Combine(Path.GetDirectoryName(path)!, $"report-{withExclusion}.pdf");
                window.WriteReport(pdf);
                byte[] bytes = File.ReadAllBytes(pdf);
                Assert.Equal("%PDF-1.7", System.Text.Encoding.ASCII.GetString(bytes, 0, 8));
                Assert.StartsWith("Report saved to ", window.StatusText, StringComparison.Ordinal);

                // A copy beside the screen renders, for looking at; out/ is not committed.
                string looks = Path.Combine(Entry109Tests.Repository(), "out", "reports");
                Directory.CreateDirectory(looks);
                File.Copy(pdf, Path.Combine(looks, withExclusion ? "report-with-exclusion.pdf" : "report.pdf"), overwrite: true);
            }

            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Entry 112 section 3: the target library lists the built-in sheets, read only, and the person's own after them. Duplicate makes an own
    /// sheet of any sheet, which renames and deletes after asking; the print screen lists both and prints an own sheet as it does a built-in one;
    /// the designer saves into the own sheets. A sheet a session used can be deleted, the question says so, and the session still opens, because
    /// it keeps its own copy of the definition.
    /// </summary>
    [AvaloniaFact]
    public void TheLibraryKeepsOwnSheetsBesideTheBuiltInOnesAndAsksBeforeDeleting()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string folder = Path.GetDirectoryName(path)!;
        try
        {
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            long session = window.CurrentSession!.Value;

            window.ShowLibrary();
            Settle();
            Assert.True(window.ShowingLibrary);
            var rows = window.LibraryRows;
            Assert.Equal("# Centrefire load development", rows[0]);
            Assert.Contains("GroupLab 5x5 Load Development, Letter", rows);
            Assert.Contains("# " + OwnSheets.Family, rows);

            // A built-in sheet is read only: it can be printed and duplicated, and nothing else.
            window.ChooseLibrarySheet("GroupLab 5x5 Load Development, Letter");
            Assert.Contains(window.LibraryDetailText, t => t.StartsWith("Built in and read only", StringComparison.Ordinal));
            var offered = window.LibraryDetail.GetLogicalDescendants().OfType<Button>().Select(b => b.Content as string).ToList();
            Assert.Contains("Duplicate", offered);
            Assert.DoesNotContain("Rename", offered);
            Assert.DoesNotContain(offered, o => o?.StartsWith("Delete", StringComparison.Ordinal) == true);

            Named(window.LibraryDetail, "Duplicate").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            var copy = window.LibrarySelected!;
            Assert.Equal(OwnSheets.Family, copy.Family);
            Assert.Equal("GroupLab 5x5 Load Development, Letter copy", copy.Definition.Name);
            Assert.True(File.Exists(Path.Combine(window.OwnSheets.Folder, copy.File)));
            Assert.Contains("GroupLab 5x5 Load Development, Letter copy", window.LibraryRows);

            window.LibraryDetail.GetLogicalDescendants().OfType<TextBox>().Single().Text = "My 5x5";
            Named(window.LibraryDetail, "Rename").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Equal("My 5x5", window.LibrarySelected!.Definition.Name);
            Assert.Equal(copy.File, window.LibrarySelected.File);

            // The print screen lists both, and the own sheet prints through the same path as a built-in one.
            var print = window.PrintFromLibrary();
            Assert.Contains(print.Sheets, s => s.Family == OwnSheets.Family && s.Definition.Name == "My 5x5");
            Assert.Contains(print.Sheets, s => s.Family != OwnSheets.Family);
            Assert.True(print.SavePdf(Path.Combine(folder, "own.pdf")));
            print.Close();

            // The designer keeps what it makes among the own sheets.
            var designer = new PrintWindow(window.OwnSheets);
            designer.Show();
            designer.SetDesign("letter", 5, 5, "1.50", 254, 3, false);
            var designed = Assert.IsType<GroupLab.Core.Rendering.LibrarySheet>(designer.SaveDesign());
            Assert.Contains(designer.Sheets, s => s.File == designed.File && s.Family == OwnSheets.Family);
            Assert.StartsWith("Saved as ", designer.StatusText, StringComparison.Ordinal);
            designer.Close();

            // Deleting asks first, says how many sessions used the sheet, and they keep their copy.
            window.ChooseLibrarySheet("My 5x5");
            Named(window.LibraryDetail, "Delete\u2026").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Contains(window.LibraryDetailText, t => t == "Delete My 5x5? One session was analyzed against it. It keeps its own copy of the sheet, so it stays readable.");
            Named(window.LibraryDetail, "Keep it").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Contains("My 5x5", window.LibraryRows);
            window.ChooseLibrarySheet("My 5x5");
            Named(window.LibraryDetail, "Delete\u2026").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Named(window.LibraryDetail, "Delete").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.DoesNotContain("My 5x5", window.LibraryRows);
            Assert.False(File.Exists(Path.Combine(window.OwnSheets.Folder, copy.File)));

            window.OpenSession(session);
            Settle();
            Assert.True(window.Analysing);
            Assert.NotEmpty(window.Plot.Discs);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    /// <summary>
    /// Entry 112 section 4: with records that lack what the solver needs, the zero block and the Ballistics screen both say which fields; kept
    /// on the records, the fields give a dope table in the person's units and clicks, with its wind column and the sentence about aerodynamic
    /// jump, equal to the solver's own rows; and the zero correction carried to a second distance keeps its refusal where the offset cannot be
    /// told from zero, naming the factor the solver carried by.
    /// </summary>
    [AvaloniaFact]
    public void TheSolverCarriesTheZeroAndWorksOutADopeTable()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Book = RecordBook.Empty.With(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa)).With(new Load("H4350 41.5", null));
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.Session.SetEquipment(window.Book.FindRifle("Tikka T3x"), null, "H4350 41.5");
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            Assert.Contains(window.ZeroText, t => t.StartsWith("Carrying it needs the rifle's sight height, the rifle's zero distance, the load's muzzle velocity", StringComparison.Ordinal));

            window.ShowBallistics();
            Settle();
            Assert.True(window.ShowingBallistics);
            Assert.StartsWith("The solver needs the rifle's sight height", window.DopeRows.Single(), StringComparison.Ordinal);

            window.SetBallisticFields("Tikka T3x", "H4350 41.5", "1.75", "100", "2710", "0.326", model: 2, reference: 1, weight: "140");
            window.KeepBallistics();
            Settle();
            var rifle = window.Book.FindRifle("Tikka T3x")!;
            var load = window.Book.FindLoad("H4350 41.5")!;
            Assert.Equal((1.75, 100.0), (rifle.SightHeightInches, rifle.ZeroDistanceYards));
            Assert.Equal((2710.0, 0.326, (GroupLab.Core.Ballistics.DragModel?)GroupLab.Core.Ballistics.DragModel.G7, (GroupLab.Core.Ballistics.ReferenceAtmosphere?)GroupLab.Core.Ballistics.ReferenceAtmosphere.Icao, 140.0),
                (load.MuzzleVelocityFps, load.BallisticCoefficient, load.DragModel, load.BcReference, load.BulletWeightGrains));

            var rows = window.DopeRows;
            Assert.Equal("range, yd | drop, in | elevation, MOA | clicks | 10 mph wind, in | windage, MOA | clicks", rows[0]);
            var solver = GroupLab.Core.Ballistics.SolverUse.Dope(GroupLab.Core.Ballistics.SolverUse.Input(rifle, load, new GroupLab.Core.Ballistics.AirInput())!, 600, 100);
            for (int i = 1; i <= 6; i++)
            {
                var cells = rows[i].Split(" | ");
                Assert.Equal((100 * i).ToString(System.Globalization.CultureInfo.InvariantCulture), cells[0]);
                Assert.Equal(solver.Points[i].DropInches.ToString("F3", System.Globalization.CultureInfo.InvariantCulture), cells[1]);
            }

            Assert.EndsWith("clicks up", rows[6].Split(" | ")[3], StringComparison.Ordinal);
            Assert.Contains(rows, r => r == "Aerodynamic jump is not modeled.");
            Assert.Contains(rows, r => r.StartsWith("No spin drift", StringComparison.Ordinal));

            // Carried to 300 yd: this sheet's centre cannot be told from zero on either axis, so nothing is carried, and it says so.
            window.ShowBallistics(false);
            window.CarryTo("300");
            Settle();
            var zero = window.ZeroText.ToList();
            Assert.Contains(zero, t => t.StartsWith("Windage: not distinguishable from zero at ", StringComparison.Ordinal) && t.EndsWith("so there is nothing to carry to 300 yd.", StringComparison.Ordinal));
            Assert.Contains(zero, t => t.StartsWith("Elevation: not distinguishable from zero at ", StringComparison.Ordinal));
            Assert.Contains(zero, t => t.Contains("Elevation carries along the solver's path, by ", StringComparison.Ordinal) && t.EndsWith("Aerodynamic jump is not modeled.", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
