using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using Button = Avalonia.Controls.Button;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A4: what a photograph came to, on the phone. The group's figures in the person's own units, the
/// desktop's composite plot filled the same way from the same marking, and the photograph with every hole GroupLab found. Entry 291
/// section 2: the photograph is shown upright as the sheet is and is for looking only; the holes are corrected on a page of their own, Fix
/// holes, where nothing moves by accident. Every change is saved at once. Where the sheet could not be read, the reason and what to do
/// next, never a blank screen.
/// </summary>
public sealed class ResultView : UserControl
{
    private readonly MarkingSession session;
    private readonly TargetDefinition? definition;
    private UnitSettings units;
    private readonly CompositePlot plot = new() { Height = 360, HorizontalAlignment = HorizontalAlignment.Stretch };

    /// <summary>Entry 259 screen 1: the tiles, the plot with its chips, and the sections that open, from the shared figures.</summary>
    private readonly FiguresView full;
    private readonly TextBlock saved = Screens.Line("");
    private SheetPicture? picturePane;

    /// <summary>Entry 318 section 1: the marks to check, a card that follows every change.</summary>
    private readonly StackPanel checks = new() { Spacing = 8 };
    private long? sessionId;
    private readonly Action again;

    /// <summary>
    /// Entry 356 section 5: "Which target is this?", a sheet over the choices behind it, each with the line that says what it needs. Marking
    /// it by hand opens Marking A; a store-bought target opens Add a store-bought target; a GroupLab sheet leaves the list of sheets behind it.
    /// </summary>
    private Control WhichTargetIsThis(Control behind, WorkingImage working, ShotSetup setup, UnitSettings units, Action again)
    {
        void Back()
        {
            // The choices behind the sheet come back as the screen, out of the layer they sat in.
            Content = null;
            (behind.Parent as Panel)?.Children.Remove(behind);
            Content = behind;
        }

        var byHand = Screens.Row(GroupLab.Core.Registration.OpeningWords.ByHand.Label, GroupLab.Core.Registration.OpeningWords.ByHand.Says, () =>
            Content = new MarkingAPage(working.Path, working.Metadata.Orientation, setup, units, marked => Content = new ResultView(marked, setup, units, again), Back), explain: false)
            .Id("which-target-by-hand");
        var store = Screens.Row(GroupLab.Core.Registration.OpeningWords.StoreBought.Label, GroupLab.Core.Registration.OpeningWords.StoreBought.Says,
            () => Content = new FingerprintPage(Back), explain: false).Id("which-target-store");
        var sheet = Screens.Row(GroupLab.Core.Registration.OpeningWords.GroupLabSheet.Label, GroupLab.Core.Registration.OpeningWords.GroupLabSheet.Says, Back, explain: false)
            .Id("which-target-grouplab");
        byHand.Classes.Add(PhoneStyles.Primary);
        var body = new StackPanel { Spacing = 10 };
        body.Children.Add(Screens.Dim(GroupLab.Core.Registration.OpeningWords.WhichSays));
        body.Children.Add(byHand);
        body.Children.Add(store);
        body.Children.Add(sheet);
        body.Children.Add(Screens.Quiet(GroupLab.Core.Registration.OpeningWords.WhichFooter));
        return ProblemSheet.Over(behind, GroupLab.Core.Registration.OpeningWords.WhichTitle, body, [byHand, store, sheet], Back);
    }

    /// <summary>
    /// Entry 356 section 7, board "B, final candidate, on a phone", stacked: the stages with "Looks like a GroupLab sheet" ticked, the sheet it
    /// looks like where the markers and the drawing say, Choose the sheet in amber, Take it again, then "Not a GroupLab sheet?" with
    /// Store-bought or hand-drawn, and More choices, which is everything behind the sheet.
    /// </summary>
    private Control SheetProblem(Control behind, PhoneResult result, WorkingImage working, ShotSetup setup, UnitSettings units, Action again)
    {
        void Back()
        {
            Content = null;
            (behind.Parent as Panel)?.Children.Remove(behind);
            Content = behind;
        }

        int of = Math.Max(GroupLab.Core.Registration.OpeningWords.CornerCodes, result.Look?.Codes.Count ?? 0);
        var stages = new StackPanel { Spacing = 6 };
        foreach (var (mark, words, dim) in new[]
        {
            ("✓", GroupLab.Core.Registration.OpeningWords.LooksLikeGroupLab, false),
            ("✕", GroupLab.Core.Registration.OpeningWords.CornerCodesStage(0, of), false),
            ("○", GroupLab.Core.Registration.OpeningWords.WhichSheetThenHoles, true),
        })
        {
            stages.Children.Add(dim ? Screens.Quiet($"{mark}  {words}") : Screens.Line($"{mark}  {words}"));
        }

        var body = new StackPanel { Spacing = 11 };
        body.Children.Add(Screens.Card(stages));
        if (result.LooksLike is { } likely)
        {
            body.Children.Add(Screens.Line(GroupLab.Core.Registration.OpeningWords.LooksLikeName(likely.Name)));
        }

        var choose = ProblemSheet.Choice(GroupLab.Core.Registration.OpeningWords.ChooseSheet, Back, primary: true).Id("problem-choose-sheet");
        var take = ProblemSheet.Choice(GroupLab.Core.Registration.OpeningWords.TakeAgain, () =>
        {
            PhoneAnalysis.Discard(result.Image);
            again();
        }).Id("problem-take-again");
        var notOurs = ProblemSheet.Choice(GroupLab.Core.Registration.OpeningWords.StoreOrDrawnShort, () =>
        {
            // Remembered for this picture and counted; no picture is sent.
            if (result.PictureHash is { } hash)
            {
                Phone.Settings.SaveNotGroupLab(hash);
            }

            GroupLab.App.Diagnostics.DiagnosticLog.Info("phone.not-grouplab", ("count", Phone.Settings.LoadNotGroupLabCount()));
            Back();
            var choicesPage = (Control)Content!;
            Content = null;
            Content = WhichTargetIsThis(choicesPage, working, setup, units, again);
        }).Id("problem-not-grouplab");
        var more = ProblemSheet.Choice(GroupLab.Core.Registration.OpeningWords.MoreChoices, Back).Id("problem-more-choices");
        body.Children.Add(choose);
        body.Children.Add(take);
        body.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(90, 128, 128, 128)) });
        body.Children.Add(Screens.Quiet(GroupLab.Core.Registration.OpeningWords.NotGroupLab));
        body.Children.Add(notOurs);
        body.Children.Add(more);
        return ProblemSheet.Over(behind, GroupLab.Core.Registration.OpeningWords.CodesTitle, body, [choose, take, notOurs, more], Back);
    }

    internal ResultView(PhoneResult result, ShotSetup setup, UnitSettings units, Action again)
    {
        // Entry 273: a tap on any number switches units everywhere; this result shows them again.
        void Follow() => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.units = Phone.Settings.LoadUnits();
            Refresh();
        });
        AttachedToVisualTree += (_, _) => Shell.UnitsChanged += Follow;
        DetachedFromVisualTree += (_, _) => Shell.UnitsChanged -= Follow;

        this.units = units;
        this.again = again;
        definition = result.Definition;
        sessionId = result.SessionId;
        session = new MarkingSession(result.State);
        // Entry 323 section 3: the plot's chips as they were left, the velocity band's among them.
        plot.Shown = Phone.Settings.LoadPlotMarks();
        full = new FiguresView(result.State, units, plot, ShowShotsToZero) { Definition = result.Definition, SessionId = result.SessionId, VelocityAction = VelocityAction };
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title(result.Definition?.Name ?? "The sheet"));
        if (result.Failure is { } failure)
        {
            column.Children.Add(Screens.Line(failure));
            if (result.AskWhichSheet && result.Image is { } working)
            {
                if (result.LooksLike is { } likely)
                {
                    // Entry 281: the sheet the picture looks most like, by its markers and its drawing, to confirm with one press.
                    column.Children.Add(Screens.Line($"It looks like {likely.Name}."));
                    column.Children.Add(Screens.Primary("Yes, measure it as that sheet", () => _ = AsSheet(working, likely, setup, again)).Id("result-as-likely-sheet"));
                }

                // Entry 279 section 2: a target GroupLab did not print is marked by hand, Marking A.
                var failed = Screens.Page(column);
                column.Children.Add(Screens.Choice("Not a GroupLab sheet: mark it by hand", () => Content = new MarkingAPage(working.Path, working.Metadata.Orientation, setup, units,
                    marked => Content = new ResultView(marked, setup, units, again), () => Content = failed)).Id("result-mark-by-hand"));
                column.Children.Add(Screens.Line(result.LooksLike is null ? "Which sheet is it?" : "Or another sheet:"));
                foreach (var sheet in PhoneAnalysis.Library().OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    column.Children.Add(Screens.Choice(sheet.Name, () => _ = AsSheet(working, sheet, setup, again)));
                }
            }

            column.Children.Add(Screens.Choice("Try another picture", () =>
            {
                PhoneAnalysis.Discard(result.Image);
                again();
            }));
            Content = Screens.Page(column);

            // Entry 356 section 5, board "Not an error": a target GroupLab has not been told about asks which it is, calmly, over the choices,
            // marking it by hand first.
            if (result.Opening == GroupLab.Core.Registration.OpeningOutcome.NotGroupLab && result.Image is { } unknown)
            {
                var choicesPage = (Control)Content!;
                Content = null;
                Content = WhichTargetIsThis(choicesPage, unknown, setup, units, again);
            }
            else if (result.Opening == GroupLab.Core.Registration.OpeningOutcome.LooksLikeGroupLab && result.Image is { } sheetLike)
            {
                // Entry 356 section 7, board "B, final candidate, on a phone": it looks like a GroupLab sheet and its codes would not read.
                var choicesPage = (Control)Content!;
                Content = null;
                Content = SheetProblem(choicesPage, result, sheetLike, setup, units, again);
            }

            // Entry 340 section 1: a store-bought target GroupLab knows goes straight to marking it, its bulls placed and its printed size as
            // the scale, after "Which target is this?" where the picture cannot tell its sizes apart. Cancel comes back to the choices above.
            if (result.Recognized is { } seen && result.Image is { } image)
            {
                var choices = Content;
                if (seen.Recognition.AsksWhichSize)
                {
                    Content = new WhichTargetPage(seen.Recognition, match => MarkStoreTarget(result, seen, match, image, setup, units, again, choices));
                }
                else
                {
                    MarkStoreTarget(result, seen, seen.Recognition.Named, image, setup, units, again, choices);
                }
            }

            return;
        }

        // Entry 243 section 3.3: three parts, the numbers, the sheet and what to do next, which a phone shows one under the other in the
        // order it always had, and a big screen in landscape, at least ExpandedWidth wide, side by side: the sheet on the left, the numbers
        // and what to do next beside it. Entry 246 found the tablet in portrait, 924 wide, going side by side too, which the entry did not
        // ask for, and the phone's order changed; both are as they were now.
        var numbers = new StackPanel { Spacing = 12 };
        var picture = new StackPanel { Spacing = 12 };
        var actions = new StackPanel { Spacing = 12 };
        numbers.Children.Add(full);
        // Entry 280 section 2, board MultiAim: on a target GroupLab did not print, a chip per aim point in its own color, its own figures,
        // and "+ Aim point"; the figures above stay the pooled ones.
        if (AimPoints(result, setup, again) is { } aims)
        {
            numbers.Children.Add(aims);
        }

        // Entry 273: the one-time hint card, until a number has been tapped once.
        if (!Phone.Settings.LoadUnitTapped())
        {
            numbers.Children.Add(Screens.Card(Screens.Line(GroupLab.Core.Marking.UnitSwitch.Hint + "."), Screens.Dim(GroupLab.Core.Marking.UnitSwitch.HintMore)));
        }
        // Entry 341 section 2: a store-bought target's printed size is the scale, so the figures carry the warning and the way to check it.
        if (GroupLab.Core.StoreTargets.StoreTargetMatch.InPlay(result.State) && result.State.ImagePath is { } marked && File.Exists(marked))
        {
            numbers.Children.Add(Screens.Card(Screens.Line(GroupLab.Core.StoreTargets.StoreTargetMatch.Warning),
                Screens.Choice(GroupLab.Core.StoreTargets.StoreTargetMatch.CheckScale, () =>
                {
                    var here = Content;
                    Content = new MarkingAPage(marked, session.State.ExifOrientation, setup, units,
                        done => Content = new ResultView(done, setup, units, again), () => Content = here, session.State, sessionId, checkTheScale: true);
                }).Id("result-check-scale")));
        }

        // Entry 271: what the figures are measured in, and on a photograph the ruler that makes them real inches.
        if (PrinterCard.For(result, session, Changed) is { } printer)
        {
            numbers.Children.Add(printer);
        }

        if (result.State.ImagePath is { } path && File.Exists(path))
        {
            var bitmap = new Bitmap(path);
            int turns = ViewRotation.Upright(result.State.Scale, bitmap.PixelSize.Width, bitmap.PixelSize.Height, result.State.ViewQuarterTurns);
            picturePane = new SheetPicture(bitmap, turns, () => session.State.Shots.Where(s => s.IsShot).ToList(),
                definition is null && AimedByHand(result.State) ? shot => AimColour(session.State, shot.Bull) : null,
                () => ReviewQueue.MarksToCheck(session.State).Select(f => f.ShotId).ToHashSet());
            picture.Children.Add(checks);

            // Entry 291 section 2.2: the holes are fixed on their own page, and the result measures again when it comes back.
            picture.Children.Add(Screens.Primary("Fix holes", () =>
            {
                var here = Content;
                Content = new FixHolesPage(session.State, turns, fixedState =>
                {
                    Content = here;
                    if (fixedState is not null)
                    {
                        session.Load(fixedState);
                        Changed();
                    }
                }, units);
            }).Id("result-fix-holes"));
            picture.Children.Add(Screens.Dim("Move, add or remove a hole under a crosshair, with zoom and undo."));
            picture.Children.Add(picturePane);
        }

        // Entry 259 screen 6: a sheet of a set from Made for your optic leads to the set, pooled so far, and the sheets still to read.
        if (session.State.SetSheet is not null && definition?.Tiling is { } set && set.Cols * set.Rows > 1)
        {
            actions.Children.Insert(0, Screens.Row("Your set", "The sheets read so far pooled into one group, and those still to read", ShowSet));
        }

        // Entry 259 screen 5: the hit chance with this group carried in.
        actions.Children.Add(Screens.Row("Ballistics", "The dope, and the chance of a hit with this group", () => Shell.Current?.ShowBallistics(session.State)).Id("result-ballistics"));

        // Entry 259 screen 2: which bulls were fired at, so each shot is measured from its own.
        if (session.State.Bulls.Count(b => b.Scoring) > 1)
        {
            actions.Children.Add(Screens.Row("Bulls you fired at", AimedBulls.Says(session.State.Rule, session.State.Bulls), ShowBulls));
        }

        // Entry 280 section 2, Shots A: every shot's offset and clicks, and which count.
        actions.Children.Add(Screens.Row("Shots", "Each shot's offset and clicks, and which count", () =>
        {
            var result = Content;
            Content = new ShotsPage(session, definition, units, Changed, () =>
            {
                Content = result;
                Refresh();
            });
        }).Id("result-shots"));

        // Entry 280 section 2, board ZeroFrom: the zero from this group, and on to Shots Needed to Zero.
        actions.Children.Add(Screens.Row("Zero from this group", "Where the group sits, the clicks, and how sure", () =>
        {
            var result = Content;
            Content = new ZeroFromPage(session.State, units, ShowShotsToZero, () => Content = result);
        }).Id("result-zero"));

        // Entry 280 section 2, board ShareA: the picture with a results box on it, saved to the gallery or shared.
        if (result.State.ImagePath is { } shared && File.Exists(shared))
        {
            actions.Children.Add(Screens.Row("Share a picture", "The target with its results box and the mean radius circle", () =>
            {
                var result = Content;
                Content = new SharePage(session.State, Title(), Date(), units, () => Content = result);
            }).Id("result-share-picture"));
        }

        // Entry 280 section 2, board Report: one dated page, shared or printed.
        actions.Children.Add(Screens.Row("Report", "One dated page: the picture, the plot, the figures and how sure", () =>
        {
            var result = Content;
            Content = new OnePageReportPage(session.State, Title(), Date(), units, () => Content = result);
        }).Id("result-report"));

        // Entry 279 section 3 and entry 281 section 2: Unholy's "Fudd buster mode", from twenty shots.
        if (FuddBusterPage.Shots(session.State).Count >= GroupLab.Core.Statistics.FuddBuster.LeastShots)
        {
            actions.Children.Add(Screens.Row(GroupLab.Core.Statistics.FuddBusterWords.Title, "Why a few shots mislead, shown with your own", () =>
            {
                var result = Content;
                Content = new FuddBusterPage(session.State, units, () => Content = result);
            }));
        }

        // Entry 281 section 2: the phone follows A, saved by itself, and says so from the start, not only after a change.
        saved.Text = SavedWords(sessionId);
        actions.Children.Add(saved);
        var shareSaid = Screens.Line("");
        actions.Children.Add(Screens.Choice("Share this session", () => shareSaid.Text = SessionFiles.Share(session.State, definition, units) ?? "").Id("result-share-session"));
        actions.Children.Add(Screens.Choice("Share the shots as CSV", () => shareSaid.Text = SessionFiles.ShareCsv(session.State, definition) ?? "").Id("result-share-csv"));
        actions.Children.Add(shareSaid);
        actions.Children.Add(Screens.Choice("Another target", again).Id("result-another-target"));
        Refresh();

        var host = new Grid { Margin = new Thickness(16) };
        var left = new StackPanel { Spacing = 12 };
        var right = new StackPanel { Spacing = 12 };
        Grid.SetColumn(right, 2);
        bool? wide = null;
        void Arrange(Size size)
        {
            // Entry 342, the phone sweep: which way up is the screen's, not the page's. On an iPad upright the keyboard took 400 points off
            // the page's height, the page became wider than tall, and the result changed to its side-by-side layout under the person's
            // finger, losing the field being typed in under the keyboard.
            var screen = TopLevel.GetTopLevel(this)?.Bounds.Size is { Width: > 0 } whole ? whole : size;
            bool now = Shell.Across(size.Width) >= ExpandedWidth && screen.Width > screen.Height && picture.Children.Count > 0;
            if (wide == now)
            {
                return;
            }

            wide = now;
            host.Children.Clear();
            host.ColumnDefinitions.Clear();
            left.Children.Clear();
            right.Children.Clear();
            foreach (var part in new Control[] { numbers, picture, actions })
            {
                column.Children.Remove(part);
            }

            if (now)
            {
                // Entry 298 section 4: on a big screen in landscape the split between the sheet and the numbers can be dragged, and the
                // share it is left at is remembered; a grip drawn in the middle of a finger-wide strip.
                double share = Math.Clamp(Phone.Settings.LoadPaneShare(ResultSplit) ?? 0.6, 0.3, 0.75);
                host.MaxWidth = double.PositiveInfinity;
                host.ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(share, GridUnitType.Star) { MinWidth = 320 },
                    new ColumnDefinition(24, GridUnitType.Pixel),
                    new ColumnDefinition(1 - share, GridUnitType.Star) { MinWidth = 280 },
                };
                left.Children.Add(column);
                left.Children.Add(picture);
                right.Children.Add(numbers);
                right.Children.Add(actions);
                host.Children.Add(left);
                host.Children.Add(right);
                var line = new Border { Width = 4, Height = 56, CornerRadius = new CornerRadius(2), VerticalAlignment = VerticalAlignment.Center, Classes = { GroupLab.App.Theme.AppStyles.Divider } };
                var grip = new GridSplitter { Width = 24, Background = Brushes.Transparent, ResizeDirection = GridResizeDirection.Columns };
                grip.DragCompleted += (_, _) =>
                {
                    double a = host.ColumnDefinitions[0].ActualWidth, b = host.ColumnDefinitions[2].ActualWidth;
                    if (a + b > 0)
                    {
                        Phone.Settings.SavePaneShare(ResultSplit, a / (a + b));
                    }
                };
                Grid.SetColumn(line, 1);
                Grid.SetColumn(grip, 1);
                host.Children.Add(line);
                host.Children.Add(grip);
            }
            else
            {
                host.MaxWidth = 640;
                column.Children.Add(numbers);
                column.Children.Add(picture);
                column.Children.Add(actions);
                host.Children.Add(column);
            }
        }

        SizeChanged += (_, e) => Arrange(e.NewSize);
        Arrange(Bounds.Size);
        // The explanation sheet lies over the page at its bottom edge, and closes with its own button.
        Content = new Grid
        {
            Children =
            {
                new ScrollViewer { Content = host, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled },
                full.Sheet,
            },
        };
    }

    /// <summary>What the shared picture and the report are named by: the sheet, and the sheet's own label where it has one.</summary>
    private string Title() => (definition?.Name ?? "Marked by hand") + (session.State.SheetLabel is { Length: > 0 } label ? ", " + label : "");

    /// <summary>The day the target was shot, as the session keeps it, or today.</summary>
    private string Date() =>
        (sessionId is { } id ? PhoneAnalysis.Store().Get(id)?.ShotDate : null) ?? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Entry 259 screen 6: the set this sheet belongs to, as a checklist; photographing the next sheet goes to the camera.</summary>
    private void ShowSet()
    {
        var result = Content;
        string? date = sessionId is { } id ? PhoneAnalysis.Store().Get(id)?.ShotDate : null;
        Content = new SetPage(definition!, date, units, again, () => Content = result);
    }

    /// <summary>Entry 259 screen 2: the bulls fired at, chosen on the sheet's own layout, and back to the result.</summary>
    private void ShowBulls()
    {
        var result = Content;
        Content = new BullsPage(session, Changed, () => Content = result);
    }

    /// <summary>Entry 259 screen 3: Shots Needed to Zero as its own page, from this result, and back to it.</summary>
    private void ShowShotsToZero()
    {
        var result = Content;
        Content = new ShotsToZeroPage(session.State, units, () => Content = result);
    }

    /// <summary>The width from which the sheet and the numbers sit side by side in landscape, Material's expanded window class, entry 243 section 3.3.</summary>
    internal const double ExpandedWidth = 840;

    /// <summary>Entry 298 section 4: the settings key of the wide result's split between the sheet and the numbers.</summary>
    internal const string ResultSplit = "phone.result";

    /// <summary>Whether the aim points were placed by hand on a target GroupLab did not print, where each has a color.</summary>
    internal static bool AimedByHand(MarkingState state) => ResultWords.AimedByHand(state);

    /// <summary>An aim point's color, the desktop's own (Tokens.BullMarks), by its place among the aim points.</summary>
    internal static IBrush AimColour(MarkingState state, int? bull)
    {
        int at = bull is { } b ? state.Bulls.FindIndex(x => x.Index == b) : -1;
        var marks = GroupLab.App.Theme.Tokens.BullMarks;
        return at < 0 ? Brushes.OrangeRed : new SolidColorBrush(marks[at % marks.Count]);
    }

    private Control? AimPoints(PhoneResult result, ShotSetup setup, Action again)
    {
        var state = session.State;
        if (definition is not null || !AimedByHand(state) || result.State.ImagePath is not { } path)
        {
            return null;
        }

        var figures = GroupAnalysis.ByAimPoint(state);
        var said = Screens.Line("Tap an aim point for its own figures.");
        var chips = new WrapPanel();
        foreach (var (aim, own) in figures)
        {
            var dot = new Avalonia.Controls.Shapes.Ellipse { Width = 12, Height = 12, Fill = AimColour(state, aim.Index), Margin = new Thickness(0, 0, 6, 0) };
            var chip = new Button { Content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { dot, new TextBlock { Text = "Aim " + aim.Label } } }, MinHeight = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
            chip.Click += (_, _) => said.Text = ResultWords.AimPoint(aim, own, units);
            chips.Children.Add(chip);
        }

        var add = Screens.Choice("+ Aim point", () =>
        {
            var here = Content;
            Content = new MarkingAPage(path, session.State.ExifOrientation, setup, units,
                marked => Content = new ResultView(marked, setup, units, again), () => Content = here, session.State, sessionId);
        });
        return Screens.Card(Screens.Heading("Aim points"), chips, said, add,
            Screens.Dim(ResultWords.AimPointsPooled));
    }

    /// <summary>
    /// Entry 340: Marking A on a recognized store-bought target, its bulls placed and, where a size is known, its printed size as the scale;
    /// "Not sure" places the bull and starts at the scale.
    /// </summary>
    private void MarkStoreTarget(PhoneResult result, StoreTargetSeen seen, GroupLab.Core.StoreTargets.StoreTargetMatch? match, WorkingImage image, ShotSetup setup, UnitSettings units, Action again, object? back)
    {
        var marking = new MarkingSession(result.State);
        string said;
        if (match is null)
        {
            foreach (var bull in seen.Recognition.Family.FirstOrDefault()?.Bulls(seen.Width, seen.Height) ?? [])
            {
                marking.AddBull(bull);
            }

            said = GroupLab.Core.StoreTargets.FamilyQuestion.NotSureSaid;
        }
        else
        {
            marking.PlaceStoreTarget(match, seen.Width, seen.Height);
            said = match.Said(marking.State.Bulls.Count);
        }

        Content = new MarkingAPage(image.Path, image.Metadata.Orientation, setup, units, marked => Content = new ResultView(marked, setup, units, again), () => Content = back,
            marking.State, said: said);
    }

    /// <summary>Where the result is kept and that it is safe to close, entry 279 section 3 (Unholy) and entry 281 section 2 (A).</summary>
    private static string SavedWords(long? id) => id is null
        ? "This session could not be saved on the phone."
        : "Saved in Sessions on this phone, and every change as you make it: safe to close.";

    /// <summary>After a change: the figures and the plot again, and the session saved over itself.</summary>
    private void Changed()
    {
        sessionId = PhoneAnalysis.Save(session.State, definition, units, sessionId) ?? sessionId;
        Refresh();
    }

    /// <summary>
    /// Entry 323 section 4: where the velocity card's button goes. The readings and the distance open a page in place of the result and come
    /// back to it; the BC opens Ballistics on its load.
    /// </summary>
    private void VelocityAction(VelocityBlockState state)
    {
        var back = Content;
        void Return()
        {
            Content = back;
            Refresh();
        }

        switch (state)
        {
            case VelocityBlockState.NoReadings:
                Content = VelocityPages.Chronograph(sessionId, session.State, Return);
                break;
            case VelocityBlockState.NoDistance:
                Content = VelocityPages.Distance(units, inches =>
                {
                    session.SetShotDistance(inches);
                    sessionId = PhoneAnalysis.Save(session.State, definition, units, sessionId) ?? sessionId;
                    full.SessionId = sessionId;
                    Return();
                }, Return);
                break;
            case VelocityBlockState.NoBc:
                Shell.Current?.ShowBallistics(session.State, open: "load");
                break;
        }
    }

    private void Refresh()
    {
        var state = session.State;
        var labels = ShotLabels.For(state);
        string Label(int id) => labels.FirstOrDefault(l => l.ShotId == id) is { Text: { } text } ? text : id.ToString(CultureInfo.InvariantCulture);
        string Bull(int index) => state.Bulls.FirstOrDefault(b => b.Index == index)?.Label ?? index.ToString(CultureInfo.InvariantCulture);
        plot.Show(state, definition, units, Label, Bull);
        full.Show(state, units);
        saved.Text = SavedWords(sessionId);
        ShowChecks();
        picturePane?.InvalidateVisual();
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 318 section 1: a mark much bigger than the bullet goes to the person to check. Each one still standing is
    /// ringed in amber on the picture and said here in a sentence, with the review queue's own choices; it stays until one of them is taken
    /// or the shot is moved in Fix holes. Entry 318 section 2: so is a hole Find holes proposed and was not sure of.
    /// </summary>
    private void ShowChecks()
    {
        checks.Children.Clear();
        var state = session.State;
        var flags = ReviewQueue.MarksToCheck(state);
        checks.IsVisible = flags.Count > 0;
        if (flags.Count == 0)
        {
            return;
        }

        var open = ReviewQueue.For(state).Where(i => !i.Resolved && i.Kind is ReviewKind.Joined or ReviewKind.Oversized or ReviewKind.Proposed).ToList();
        var card = new List<Control> { Screens.Heading(flags.Count == 1 ? "1 mark to check" : $"{flags.Count} marks to check") };
        foreach (var flag in flags)
        {
            card.Add(Screens.Line(flag.Sentence));
            if (open.FirstOrDefault(i => i.ShotId == flag.ShotId) is { } item)
            {
                foreach (var choice in item.Choices)
                {
                    card.Add(Screens.Choice(choice.Label, () =>
                    {
                        ReviewQueue.Apply(session, item, choice);
                        Changed();
                    }));
                }
            }
        }

        card.Add(Screens.Dim("Each is ringed in amber on the picture. Moving the shot in Fix holes settles it too."));
        checks.Children.Add(Screens.Card([.. card]));
    }

    private async Task AsSheet(WorkingImage working, TargetDefinition sheet, ShotSetup setup, Action again)
    {
        // Entry 313 section 1: a Cancel that always works, at once, and a time limit, as for a picture whose codes are read.
        var reading = new Reading("phone.detect");
        var (page, line, stop) = Screens.Progress($"Reading the sheet as {sheet.Name}");
        stop.Click += (_, _) =>
        {
            reading.Stop();
            line.Text = "Canceling…";
        };
        Content = page;
        ReadOutcome<PhoneResult> outcome;
        using (Phone.Platform.KeepRunning("Reading the sheet"))
        {
            outcome = await reading.Run(token => PhoneAnalysis.Detect(working, sheet, setup, units, Phone.Survey, token, words => Dispatcher.UIThread.Post(() =>
            {
                if (!reading.Stopping)
                {
                    line.Text = words;
                }
            })), Reading.Limit, late =>
            {
                // A reading stopped part way: nobody will see what it made, so its session and working copy go when it ends.
                if (late?.SessionId is { } id)
                {
                    PhoneAnalysis.Store().Delete(id);
                }

                PhoneAnalysis.Forget(working);
            });
        }

        switch (outcome.End)
        {
            case ReadEnd.Done:
                Content = new ResultView(outcome.Value!, setup, units, again);
                break;
            case ReadEnd.Failed:
                PhoneAnalysis.Forget(working);
                Content = new ResultView(new PhoneResult(MarkingState.Empty, sheet, "The picture could not be analyzed: " + outcome.Error!.Message, null), setup, units, again);
                break;
            case ReadEnd.TimedOut:
                Content = new ResultView(new PhoneResult(MarkingState.Empty, sheet,
                    $"Reading the picture as {sheet.Name} took longer than a minute and was stopped. Take it again closer, square on and in even light.", null), setup, units, again);
                break;
            default:
                // Entry 243 section 3.2: canceled, so the working copy goes when the reading stops, and the person is back where they started.
                GroupLab.App.Diagnostics.DiagnosticLog.Info("phone.detect.cancel", ("ms", (long)outcome.Took.TotalMilliseconds));
                again();
                break;
        }
    }

    /// <summary>
    /// Entry 246, look B: the figures as tiles, in the person's own units with the angle beneath where the distance is known: mean radius,
    /// the one that answers the question, extreme spread, the shots and the group's center from the aim. None where there is no group.
    /// </summary>
    internal static IReadOnlyList<(string Label, string Value, string Under, bool Headline)> Tiles(GroupFigures? figures, UnitSettings units, double? distanceInches)
    {
        if (figures is null || figures.Shots == 0 || figures.MeanRadius is null)
        {
            return [];
        }

        string Angle(double inches) => units.Angle(inches, distanceInches) is { } angle ? $"{angle.ToString("0.00", CultureInfo.CurrentCulture)} {UnitSettings.Symbol(units.Angular)}" : "";
        var shown = new List<(string, string, string, bool)> { ("Mean radius", units.Length(figures.MeanRadius.Value), Angle(figures.MeanRadius.Value), true) };
        if (figures.ExtremeSpread is { } spread)
        {
            shown.Add(("Extreme spread", units.Length(spread.Value), Angle(spread.Value) is { Length: > 0 } a ? a : "center to center", false));
        }

        shown.Add(("Shots", figures.Shots.ToString(CultureInfo.CurrentCulture), "on the sheet", false));
        if (figures.CentreFromAim is { } centre)
        {
            double off = Math.Sqrt((centre.X * centre.X) + (centre.Y * centre.Y));
            shown.Add(("Center from aim", units.Length(off), Angle(off) is { Length: > 0 } a ? a : "from the bulls' centers", false));
        }

        return shown;
    }

    /// <summary>The group in one or two sentences: how many shots, the extreme spread and the mean radius, with the angle where the distance is known.</summary>
    internal static string Figures(GroupFigures? figures, UnitSettings units, double? distanceInches)
    {
        if (figures is null || figures.Shots == 0)
        {
            return "No holes were found on this sheet. Add them with Fix holes below, or take the picture again closer.";
        }

        string Size(double inches) => units.Angle(inches, distanceInches) is { } angle
            ? $"{units.Length(inches)} ({angle.ToString("0.00", CultureInfo.CurrentCulture)} {UnitSettings.Symbol(units.Angular)})"
            : units.Length(inches);
        string said = figures.Shots == 1 ? "1 shot." : $"{figures.Shots} shots.";
        if (figures.ExtremeSpread is { } spread)
        {
            said += $" Extreme spread {Size(spread.Value)}, center to center.";
        }

        if (figures.MeanRadius is { } radius)
        {
            said += $" Mean radius {Size(radius.Value)}.";
        }

        return said + (distanceInches is null ? " Enter the distance on Capture to see the angles." : "");
    }

    /// <summary>
    /// The working image across the whole width and exactly as tall as it needs, turned upright as the sheet is, with a ring on every hole.
    /// Entry 291 section 2: it takes no touches. Entry 281 section 1.7 already scaled it alike across and down and turned it with the
    /// marking's view; entry 291 found it turned twice, by itself and again by the box it sat in, which also stood a screen high and empty.
    /// </summary>
    /// <para>Entry 318 section 1: a shot whose mark is flagged for its size and not yet settled has a second ring around it, in amber.</para>
    internal sealed class SheetPicture(Bitmap image, int turns, Func<IReadOnlyList<MarkedShot>> shots, Func<MarkedShot, IBrush?>? colour = null,
        Func<IReadOnlySet<int>>? flagged = null) : Control
    {
        /// <summary>The quarter turns the picture is shown by.</summary>
        internal int Turns => ViewRotation.Normalise(turns);

        /// <summary>The shots ringed in amber as marks to check, entry 318 section 1.</summary>
        internal IReadOnlySet<int> Flagged => flagged?.Invoke() ?? new HashSet<int>();

        private double PixelWidth => image.PixelSize.Width;

        private double PixelHeight => image.PixelSize.Height;

        /// <summary>The picture's size as it is shown, turned.</summary>
        internal (double Width, double Height) Shown => ViewRotation.DisplaySize(turns, PixelWidth, PixelHeight);

        /// <summary>Screen units per image pixel, the same across and down so the picture keeps its shape.</summary>
        private double Scale => Math.Min(Bounds.Width / Shown.Width, Bounds.Height / Shown.Height);

        protected override Size MeasureOverride(Size available)
        {
            double width = double.IsFinite(available.Width) ? available.Width : Shown.Width;
            return new Size(width, width * Shown.Height / Shown.Width);
        }

        private Point ToScreen(PointD image)
        {
            var shown = ViewRotation.ToDisplay(image, turns, PixelWidth, PixelHeight);
            return new Point(shown.X * Scale, shown.Y * Scale);
        }

        public override void Render(DrawingContext context)
        {
            var (a, b, c, d, e, f) = ViewRotation.Affine(turns, PixelWidth, PixelHeight);
            double k = Scale;
            using (context.PushTransform(new Matrix(a * k, d * k, b * k, e * k, c * k, f * k)))
            {
                context.DrawImage(image, new Rect(image.Size), new Rect(0, 0, PixelWidth, PixelHeight));
            }

            var pen = new Pen(Brushes.OrangeRed, 2);
            // Entry 280 section 2, Shots A: a shot left out is dashed on the picture, still there.
            var leftOut = new Pen(Brushes.OrangeRed, 2, new DashStyle([2, 2], 0));
            var check = new Pen(new SolidColorBrush(GroupLab.App.Theme.Tokens.MarkSelected), 3);
            var marked = Flagged;
            foreach (var shot in shots())
            {
                var ring = colour?.Invoke(shot) is { } brush ? new Pen(brush, 2, shot.Exclusion is null ? null : new DashStyle([2, 2], 0)) : shot.Exclusion is null ? pen : leftOut;
                context.DrawEllipse(null, ring, ToScreen(shot.Image), 9, 9);
                if (marked.Contains(shot.Id))
                {
                    context.DrawEllipse(null, check, ToScreen(shot.Image), 15, 15);
                }
            }
        }
    }
}
