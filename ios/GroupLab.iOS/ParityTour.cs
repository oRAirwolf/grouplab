using System.Collections.Immutable;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Mobile;
using OpenCvSharp;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 6: every feature docs/PHONE-PARITY.md said was "not yet" on iOS only because nothing had opened
/// it on the simulator, opened through the shared screens' own buttons, rows and taps from the sample's result and the places along the
/// bottom, checked for what makes it that feature, and photographed. Each check is named by the feature's key on the Features page, so the
/// table can be held to the run. The screens are the Android ones; what this proves is that each opens and works on iOS.
/// </summary>
internal static class ParityTour
{
    private static int picture = 70;

    /// <summary>Runs the tour from the result on screen; <paramref name="sample"/> is the committed scan the result came from.</summary>
    internal static async Task<List<SelfTestCheck>> Run(string sample)
    {
        var checks = new List<SelfTestCheck>();
        if (await SelfTest.OnUi(() => SelfTest.Find<ResultView>()) is not { } result)
        {
            checks.Add(new SelfTestCheck("parity tour") { Detail = "no result was on screen to start from" });
            return checks;
        }

        // From the result: each row opens its own page, which is photographed and closed again.
        checks.Add(await Page(result, "every-hole", "Fix holes", () => Has<FixHolesPage>()));
        checks.Add(await Page(result, "aimed-bulls", "Bulls you fired at", () => Has<BullsPage>()));
        checks.Add(await Page(result, "shots-table", "Shots", () => Has<ShotsPage>()));
        checks.Add(await Page(result, "zero-from", "Zero from this group", () => Has<ZeroFromPage>()));
        checks.Add(await Page(result, "shots-to-zero", "Zero from this group", () => Has<ShotsToZeroPage>(), then: "Open in Shots Needed to Zero"));
        checks.Add(await Page(result, "fudd-buster", GroupLab.Core.Statistics.FuddBusterWords.Title, () => Has<FuddBusterPage>()));
        checks.Add(await Why(result));
        checks.Add(await UnitTapped(result));
        checks.Add(await Explained(result));
        checks.Add(await Shared(result, "csv", "Share the shots as CSV"));
        checks.Add(await Shared(result, "share-session", "Share this session"));
        checks.Add(await PoolSet(result));

        // From the places along the bottom.
        checks.Add(await Compare());
        checks.Add(await Optic());
        checks.Add(await LibrarySheet("zero-grids", "GroupLab Zeroing Grid, MOA at 100 yd", null));
        checks.Add(await LibrarySheet("e-bull", "GroupLab 5x5 Load Development, E Bull, Letter", null));
        checks.Add(await LibrarySheet("c-bull", "GroupLab 5x5 Load Development, C Bull, Letter", null));
        checks.Add(await LibrarySheet("two-moa", "GroupLab 3x3 2 MOA, Letter", null));
        checks.Add(await LibrarySheet("large-sheets", "GroupLab Large Format 1.5 in Bulls, 2x2 Letter Sheets", "Share as one large page with cut lines, for a plotter"));
        checks.Add(await Setting("scope-unit", UnitSettings.ScopeUnitLabel, () => Radios().Any(r => r.IsChecked == true && Words(r) == "MOA")));
        checks.Add(await Setting("send-targets", "Sending targets", null));
        checks.Add(await Setting("error-reports", "Error reports", null));
        checks.Add(await Setting("survey", "Hardware survey", null));
        checks.Add(Region());

        // A target GroupLab did not print, marked by hand: its aim points, and Marking A behind "+ Aim point".
        checks.AddRange(await HandMarked(result, sample));
        checks.Add(await Task.Run(() => Angle(sample)));
        return checks;
    }

    private static bool Has<T>()
        where T : Control => SelfTest.Find<T>() is { IsEffectivelyVisible: true };

    private static string? Words(Control control) => control switch
    {
        // A radio button is a button too.
        Button { Content: TextBlock t } => t.Text,
        TextBlock t => t.Text,
        _ => null,
    };

    private static IEnumerable<RadioButton> Radios() => Shell.Current!.GetVisualDescendants().OfType<RadioButton>();

    /// <summary>The button whose words, or first line on a row, are <paramref name="words"/>, within <paramref name="within"/>.</summary>
    private static Button? ButtonOf(Control within, string words) =>
        within.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b =>
            b.IsEffectivelyVisible && b.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault()?.Text == words);

    private static void Press(Button button) => button.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));

    private static void Tap(Control control) => control.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));

    /// <summary>Waits for <paramref name="shown"/>, photographs the screen under the feature's key, and fills the check.</summary>
    private static async Task<SelfTestCheck> Seen(SelfTestCheck check, Func<bool> shown, string what)
    {
        bool there = await SelfTest.WaitFor(shown, TimeSpan.FromSeconds(15));
        await Task.Delay(TimeSpan.FromSeconds(1));
        bool taken = await SelfTest.Photographed($"{picture++:00}-{check.Name}");
        check.Passed = there;
        check.Detail = $"{what} {(there ? "opened" : "did not open")}, {(taken ? "photographed" : "not photographed")}";
        return check;
    }

    private static async Task<SelfTestCheck> Guard(string key, Func<SelfTestCheck, Task<SelfTestCheck>> body)
    {
        var check = new SelfTestCheck(key);
        try
        {
            return await body(check);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Passed = false;
            check.Detail = SelfTestChecks.Describe(e);
            return check;
        }
    }

    /// <summary>A row of the result pressed, and a button on the page it opens where <paramref name="then"/> names one; the result comes back.</summary>
    private static Task<SelfTestCheck> Page(ResultView result, string key, string row, Func<bool> shown, string? then = null) => Guard(key, async check =>
    {
        object? home = await SelfTest.OnUi(() => result.Content);
        string? missing = await SelfTest.OnUi(() =>
        {
            if (ButtonOf(result, row) is not { } button)
            {
                return row;
            }

            Press(button);
            return null;
        });
        if (missing is null && then is not null)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            missing = await SelfTest.OnUi(() =>
            {
                if (ButtonOf(result, then) is not { } button)
                {
                    return then;
                }

                Press(button);
                return null;
            });
        }

        if (missing is not null)
        {
            check.Detail = $"\"{missing}\" was not found";
            await SelfTest.OnUi(() => result.Content = home);
            return check;
        }

        await Seen(check, shown, then ?? row);
        await SelfTest.OnUi(() => result.Content = home);
        return check;
    });

    /// <summary>A figure's name tapped: the explanation sheet opens with Close, which closes it.</summary>
    private static Task<SelfTestCheck> Why(ResultView result) => Guard("why", async check =>
    {
        string? name = await SelfTest.OnUi(() =>
        {
            var label = result.GetVisualDescendants().OfType<FigureRow>().SelectMany(r => r.Children.OfType<TextBlock>()).FirstOrDefault();
            if (label is null)
            {
                return null;
            }

            label.BringIntoView();
            Tap(label);
            return label.Text;
        });
        if (name is null)
        {
            check.Detail = "no figure's name was found to tap";
            return check;
        }

        await Seen(check, () => ButtonOf(result, "Close") is not null, $"the explanation of {name}");
        await SelfTest.OnUi(() =>
        {
            if (ButtonOf(result, "Close") is { } close)
            {
                Press(close);
            }
        });
        return check;
    });

    /// <summary>A number tapped switches its unit, and says so.</summary>
    private static Task<SelfTestCheck> UnitTapped(ResultView result) => Guard("unit-tap", async check =>
    {
        var (before, after) = await SelfTest.OnUi(() =>
        {
            var number = result.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.IsEffectivelyVisible && t.Classes.Contains(UnitTap.Value));
            if (number is null)
            {
                return ((string?)null, (string?)null);
            }

            number.BringIntoView();
            string? was = number.Text;
            Tap(number);
            return (was, number.Text);
        });
        await Task.Delay(TimeSpan.FromSeconds(1));
        bool taken = await SelfTest.Photographed($"{picture++:00}-{check.Name}");
        check.Passed = before is not null && after is not null && before != after;
        check.Detail = before is null ? "no number that switches units was found" : $"\"{before}\" became \"{after}\" when tapped, {(taken ? "photographed" : "not photographed")}";
        return check;
    });

    /// <summary>A secondary line naming a glossary word, tapped, opens its explanation.</summary>
    private static Task<SelfTestCheck> Explained(ResultView result) => Guard("explain-words", async check =>
    {
        var flyout = await SelfTest.OnUi(() =>
        {
            var line = Shell.Current!.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.IsEffectivelyVisible && FlyoutBase.GetAttachedFlyout(t) is not null);
            if (line is null)
            {
                return null;
            }

            line.BringIntoView();
            Tap(line);
            return FlyoutBase.GetAttachedFlyout(line);
        });
        if (flyout is null)
        {
            check.Detail = "no line naming a glossary word was on screen";
            return check;
        }

        await Seen(check, () => flyout.IsOpen, "the word's explanation");
        await SelfTest.OnUi(flyout.Hide);
        return check;
    });

    /// <summary>A result's export to iOS's share sheet, which opens and is closed again.</summary>
    private static Task<SelfTestCheck> Shared(ResultView result, string key, string words) => Guard(key, async check =>
    {
        var before = await SelfTest.OnUi(IosPhone.Top);
        bool pressed = await SelfTest.OnUi(() =>
        {
            if (ButtonOf(result, words) is not { } button)
            {
                return false;
            }

            Press(button);
            return true;
        });
        if (!pressed)
        {
            check.Detail = $"\"{words}\" was not found";
            return check;
        }

        await Seen(check, () => IosPhone.Top() is UIKit.UIActivityViewController, words + " (the share sheet)");
        await SelfTest.OnUi(() =>
        {
            if (IosPhone.Top() is UIKit.UIActivityViewController sheet)
            {
                sheet.DismissViewController(false, null);
            }
        });
        await SelfTest.WaitFor(() => ReferenceEquals(IosPhone.Top(), before), TimeSpan.FromSeconds(10));
        return check;
    });

    /// <summary>A set's page, the sheets of the set pooled so far and those still to read, for a set of three sheets.</summary>
    private static Task<SelfTestCheck> PoolSet(ResultView result) => Guard("pool-set", async check =>
    {
        string path = Path.Combine(Phone.Platform.FilesFolder, "targets", "GL-CF9-T.gltd.json");
        var definition = GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(path).Definition;
        if (definition is null)
        {
            check.Detail = "the set's sheet could not be read";
            return check;
        }

        object? home = await SelfTest.OnUi(() => result.Content);
        await SelfTest.OnUi(() => result.Content = new SetPage(definition, DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            UnitSettings.Imperial, () => { }, () => { }));
        await Seen(check, () => Has<SetPage>(), "the set of three sheets");
        await SelfTest.OnUi(() => result.Content = home);
        return check;
    });

    /// <summary>Sessions, Compare loads, two ticked, Compare: one figure at a time.</summary>
    private static Task<SelfTestCheck> Compare() => Guard("compare", async check =>
    {
        await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Sessions));
        await Task.Delay(TimeSpan.FromSeconds(1));
        string? missing = await SelfTest.OnUi(() =>
        {
            if (ButtonOf(Shell.Current!, "Compare loads") is not { } compare)
            {
                return "Compare loads";
            }

            Press(compare);
            foreach (var box in Shell.Current!.GetLogicalDescendants().OfType<CheckBox>().Take(2))
            {
                box.IsChecked = true;
            }

            if (ButtonOf(Shell.Current!, "Compare") is not { IsEnabled: true } go)
            {
                return "an enabled Compare";
            }

            Press(go);
            return null;
        });
        if (missing is not null)
        {
            check.Detail = $"\"{missing}\" was not found";
            return check;
        }

        return await Seen(check, () => Has<ComparePage>(), "two sessions compared");
    });

    /// <summary>Targets, Made for your optic: 100 yards at 10 power makes a sheet to print or share.</summary>
    private static Task<SelfTestCheck> Optic() => Guard("optic", async check =>
    {
        await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Targets));
        await Task.Delay(TimeSpan.FromSeconds(1));
        bool pressed = await SelfTest.OnUi(() =>
        {
            var boxes = Shell.Current!.GetLogicalDescendants().OfType<TextBox>().ToList();
            if (boxes.Count < 2 || ButtonOf(Shell.Current!, "Make the sheet") is not { } make)
            {
                return false;
            }

            boxes[1].Text = "10";
            Press(make);
            make.BringIntoView();
            return true;
        });
        if (!pressed)
        {
            check.Detail = "Made for your optic's form was not found";
            return check;
        }

        return await Seen(check, () => Shell.Current!.GetLogicalDescendants().OfType<Button>()
            .Any(b => Words(b) is { } w && w.StartsWith("Print or share the sheet", StringComparison.Ordinal)), "a sheet made for 100 yards at 10 power");
    });

    /// <summary>A sheet of the Targets library opened: its name as the title, and <paramref name="also"/> where it has that button too.</summary>
    private static Task<SelfTestCheck> LibrarySheet(string key, string name, string? also) => Guard(key, async check =>
    {
        await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Targets));
        await Task.Delay(TimeSpan.FromSeconds(1));
        var targets = await SelfTest.OnUi(() => SelfTest.Find<TargetsPage>());
        object? home = await SelfTest.OnUi(() => targets?.Content);
        bool pressed = await SelfTest.OnUi(() =>
        {
            if (targets is null || ButtonOf(targets, name) is not { } row)
            {
                return false;
            }

            Press(row);
            return true;
        });
        if (!pressed)
        {
            check.Detail = $"\"{name}\" is not in the library";
            return check;
        }

        await Seen(check, () => targets!.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == name && t.Classes.Contains(PhoneStyles.Title)), name);
        if (also is not null)
        {
            bool has = await SelfTest.OnUi(() => ButtonOf(targets!, also) is not null);
            check.Passed &= has;
            check.Detail += has ? $", with \"{also}\"" : $", but without \"{also}\"";
        }

        await SelfTest.OnUi(() => targets!.Content = home);
        return check;
    });

    /// <summary>A section of Settings brought into view and photographed; <paramref name="holds"/> checks it where given.</summary>
    private static Task<SelfTestCheck> Setting(string key, string heading, Func<bool>? holds) => Guard(key, async check =>
    {
        await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Settings));
        await Task.Delay(TimeSpan.FromSeconds(1));
        bool found = await SelfTest.OnUi(() =>
        {
            var line = Shell.Current!.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Text == heading);

            // The heading with the screen's height beneath it, so the section is shown from its heading down.
            line?.BringIntoView(new Avalonia.Rect(0, 0, 1, 560));
            return line is not null;
        });
        await Seen(check, () => found && (holds?.Invoke() ?? true), $"Settings, {heading},");
        return check;
    });

    /// <summary>The phone follows the region iOS says: the same region to GroupLab's settings, and Letter paper where it is American.</summary>
    private static SelfTestCheck Region()
    {
        var check = new SelfTestCheck("phone-region");
        string? ios = IosPhone.Region();
        string? used = AppSettingsStore.Region();
        check.Passed = ios is not null && ios == used;
        check.Detail = $"iOS says the region is {ios ?? "nothing"}, and GroupLab uses {used ?? "nothing"}, so its paper is {(AppSettingsStore.LetterRegion(used) ? "Letter" : "A4")}";
        return check;
    }

    /// <summary>
    /// A target GroupLab did not print: the sample's own marking taken as though marked by hand, with a length for its scale and three aim
    /// points, shows each aim point's own figures and "+ Aim point", which opens Marking A to go on with it.
    /// </summary>
    private static async Task<List<SelfTestCheck>> HandMarked(ResultView result, string sample)
    {
        var aims = await Guard("aim-points", async check =>
        {
            var state = await SelfTest.OnUi(() => typeof(ResultView).GetField("session", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(result) is MarkingSession s ? s.State : null);
            if (state is null || !File.Exists(sample))
            {
                check.Detail = "the result's marking could not be read";
                return check;
            }

            var bulls = state.Bulls.Where(b => b.Scoring).Take(3).Select(b => b with { Declared = null }).ToImmutableList();
            var kept = bulls.Select(b => b.Index).ToHashSet();
            var byHand = state with
            {
                ImagePath = sample,
                Scale = new LengthReference(new GroupLab.Core.Imaging.PointD(0, 0), new GroupLab.Core.Imaging.PointD(600, 0), 1.0),
                Bulls = bulls,
                Shots = state.Shots.Where(s => s.Bull is { } b && kept.Contains(b)).ToImmutableList(),
                SetSheet = null,
            };
            // The tour ends in Settings; the hand-marked result is shown where every result is, on Capture.
            await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Capture));
            await Task.Delay(TimeSpan.FromSeconds(1));
            var capture = await SelfTest.OnUi(() => SelfTest.Find<CapturePage>());
            if (capture is null)
            {
                check.Detail = "the Capture screen was not found";
                return check;
            }

            await SelfTest.OnUi(() => capture.ShowResult(new ResultView(new PhoneResult(byHand, null, null, null),
                new ShotSetup(state.Calibre, state.ShotDistanceInches), UnitSettings.Imperial, () => { })));
            return await Seen(check, () => Shell.Current!.GetLogicalDescendants().OfType<Button>().Any(b => Words(b) is { } w && w.StartsWith("Aim ", StringComparison.Ordinal))
                && ButtonOf(Shell.Current!, "+ Aim point") is not null, "three aim points marked by hand, each a chip,");
        });
        var marking = await Guard("other-targets", async check =>
        {
            bool pressed = await SelfTest.OnUi(() =>
            {
                if (ButtonOf(Shell.Current!, "+ Aim point") is not { } add)
                {
                    return false;
                }

                Press(add);
                return true;
            });
            if (!pressed)
            {
                check.Detail = "\"+ Aim point\" was not found";
                return check;
            }

            return await Seen(check, () => Has<MarkingAPage>(), "Marking A, going on from the aim points,");
        });
        return [aims, marking];
    }

    /// <summary>
    /// A photograph at an angle: the sample warped as a phone held off square would see it, one side of the sheet a tenth narrower than the
    /// other, read through the phone's own analysis, which must still find every shot, one on each bull.
    /// </summary>
    private static SelfTestCheck Angle(string sample)
    {
        var check = new SelfTestCheck("angle");
        try
        {
            if (!File.Exists(sample))
            {
                check.Skipped = true;
                check.Detail = "the sample scan was not put in the application's files";
                return check;
            }

            string angled = Path.Combine(Phone.Platform.CacheFolder, "self-test-angled.png");
            using (var flat = Cv2.ImRead(sample, ImreadModes.Color))
            {
                float w = flat.Width, h = flat.Height, inset = w * 0.05f;
                var from = new[] { new Point2f(0, 0), new Point2f(w, 0), new Point2f(w, h), new Point2f(0, h) };
                var to = new[] { new Point2f(inset, h * 0.03f), new Point2f(w - inset, h * 0.03f), new Point2f(w, h), new Point2f(0, h) };
                using var warp = Cv2.GetPerspectiveTransform(from, to);
                using var tilted = new Mat();
                Cv2.WarpPerspective(flat, tilted, warp, flat.Size(), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(255));
                Cv2.ImWrite(angled, tilted);
            }

            var setup = new ShotSetup(Calibre.Parse(SelfTestChecks.SampleCalibre, out _), SelfTestChecks.SampleDistanceInches);
            var read = PhoneAnalysis.Run(angled, setup, UnitSettings.Imperial, null, CancellationToken.None);
            File.Delete(angled);
            var shots = read.State.Shots.Where(s => s.IsShot).ToList();
            int bulls = shots.Where(s => s.Bull is not null).Select(s => s.Bull).Distinct().Count();
            check.Numbers["shots"] = shots.Count;
            check.Numbers["bulls"] = bulls;
            check.Passed = read.Failure is null && shots.Count == SelfTestChecks.SampleShots && bulls == SelfTestChecks.SampleShots;
            check.Detail = read.Failure ?? $"the sample tilted as a photograph at an angle: {shots.Count} shots on {bulls} bulls, read as {read.Definition?.Name}";
            if (read.SessionId is { } id)
            {
                PhoneAnalysis.Store().Delete(id);
            }

            PhoneAnalysis.Discard(read.Image);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = SelfTestChecks.Describe(e);
        }

        return check;
    }
}
