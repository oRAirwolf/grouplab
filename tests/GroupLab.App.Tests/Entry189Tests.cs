using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 189, Unholy's feedback. Section 6: typing "6.5" and choosing "6.5 Creedmoor" from the suggestions puts it in
/// the box and sets it, one action, where it used to leave "6.5" in the box and need Set pressed twice.
/// <para>
/// A mouse click on a suggestion cannot be sent faithfully here: the headless window hosts the suggestions inside itself, and a press on
/// one closes them before the list sees it, which a desktop's separate popup does not do. So the click is sent as what its release hands
/// on, and the keyboard path, Down then Enter, is sent as keys.
/// </para>
/// </summary>
public class Entry189Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static (MainWindow Window, string Path, AutoCompleteBox Box) Typed(string text)
    {
        var (window, path, _) = Entry109Tests.Sheet();
        var box = window.CalibreBox;
        box.Focus();
        Settle();
        window.KeyTextInput(text);
        Settle();
        Assert.Equal(text, box.Text);
        Assert.True(box.IsDropDownOpen, "the suggestions did not open");
        return (window, path, box);
    }

    private static void SetInOneAction(MainWindow window, AutoCompleteBox box)
    {
        Assert.Equal(0.264, window.Session.State.Calibre?.DiameterInches ?? 0, 3);
        Assert.Equal(window.Session.State.Calibre!.Name, box.Text);
        Assert.NotEqual("6.5", box.Text);
        Assert.False(box.IsDropDownOpen);
    }

    [AvaloniaFact]
    public void ChoosingACaliberSuggestionSetsItInOneAction()
    {
        var (window, path, box) = Typed("6.5");
        try
        {
            // A click on a suggestion ends in the list's release, which hands the suggestion under it to the window.
            string creedmoor = box.ItemsSource!.Cast<string>().First(i => i.StartsWith("6.5 Creedmoor", StringComparison.Ordinal));
            window.ChooseCalibreSuggestion(creedmoor);
            Settle();
            SetInOneAction(window, box);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void EnterOnAHighlightedSuggestionSetsIt()
    {
        var (window, path, box) = Typed("6.5");
        try
        {
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Settle();
            Assert.StartsWith("6.5 Creedmoor", window.CalibreHighlighted, StringComparison.Ordinal);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Settle();
            SetInOneAction(window, box);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Taps two points with the length tool, as a person does, and enters the distance between them.</summary>
    private static void SetLengthByHand(MainWindow window, PointD from, PointD to, string inches)
    {
        window.KeyPress(Key.L, RawInputModifiers.None, PhysicalKey.L, "l");
        window.KeyRelease(Key.L, RawInputModifiers.None, PhysicalKey.L, "l");
        Settle();
        var canvas = window.Canvas;
        foreach (var point in new[] { from, to })
        {
            var at = canvas.TranslatePoint(canvas.ToControl(point), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Settle();
        }

        var use = window.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Content as string == "Use this length");
        Assert.True(use is not null, "the length was not asked for: " + window.StatusText);
        ((Panel)use!.Parent!).Children.OfType<TextBox>().Single().Text = inches;
        use.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    /// <summary>Section 5, Unholy: a scale set by hand could not be set again without closing GroupLab. It can, and the figures follow it.</summary>
    [AvaloniaFact]
    public void AScaleSetByHandCanBeSetAgain()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry189-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "not-a-grouplab-sheet.png");
        using (var mat = new Mat(600, 800, MatType.CV_8UC1, new Scalar(200)))
        {
            Cv2.Circle(mat, new OpenCvSharp.Point(400, 300), 120, new Scalar(30), 3);
            Cv2.ImWrite(path, mat);
        }

        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        try
        {
            window.Show();
            window.OpenImage(path);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Settle();

            window.Session.AddShot(new PointD(380, 290));
            window.Session.AddShot(new PointD(420, 310));
            window.Session.AddShot(new PointD(400, 330));
            double Spread()
            {
                // The widest pair of marks, in inches on the target, through the scale in use.
                var scale = window.Session.State.Scale!;
                var on = window.Session.State.Shots.Select(s => scale.ToTarget(s.Image)).ToList();
                return on.SelectMany(a => on.Select(b => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)))).Max();
            }

            SetLengthByHand(window, new PointD(200, 300), new PointD(600, 300), "2");
            var first = Assert.IsType<LengthReference>(window.Session.State.Scale);
            Assert.Equal(2, first.Inches, 6);
            double atTwo = Spread();

            // The same two marks again, because the number typed was wrong: what a person does to correct it.
            SetLengthByHand(window, new PointD(200, 300), new PointD(600, 300), "3");
            var second = Assert.IsType<LengthReference>(window.Session.State.Scale);
            Assert.Equal(3, second.Inches, 6);
            Assert.Equal(atTwo * 1.5, Spread(), 9);

            // And somewhere else entirely.
            SetLengthByHand(window, new PointD(250, 350), new PointD(550, 350), "4");
            var third = Assert.IsType<LengthReference>(window.Session.State.Scale);
            Assert.Equal(4, third.Inches, 6);
            Assert.Equal(new PointD(250, 350), third.A);

            // Or given a new size without tapping it out again, from the length tool.
            window.KeyPress(Key.L, RawInputModifiers.None, PhysicalKey.L, "l");
            window.KeyRelease(Key.L, RawInputModifiers.None, PhysicalKey.L, "l");
            Settle();
            window.GetLogicalDescendants().OfType<Button>().First(b => b.Content as string == "Change the length of the scale in use").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            var use = window.GetLogicalDescendants().OfType<Button>().First(b => b.Content as string == "Use this length");
            ((Panel)use.Parent!).Children.OfType<TextBox>().Single().Text = "5";
            use.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            var fourth = Assert.IsType<LengthReference>(window.Session.State.Scale);
            Assert.Equal((third.A, third.B, 5.0), (fourth.A, fourth.B, fourth.Inches));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    /// <summary>
    /// Section 3: with a distance each size leads with its angle and has the size on the paper beneath; without one, the size on the paper
    /// and a way to give the distance; and the setting puts the size on the paper first for a one-distance shooter.
    /// </summary>
    [AvaloniaFact]
    public void ASizeIsAnAngleFirstWhereTheDistanceIsKnown()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            (string Name, string Value, string? Tip) Mean() => window.KeptFigures.Single(k => k.Name == "Mean radius");
            Assert.EndsWith(" in", Mean().Value, StringComparison.Ordinal);
            Assert.Empty(window.KeptBeneath);
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), b => b.Text?.Contains("needs the shot distance", StringComparison.Ordinal) == true);

            window.Session.SetShotDistance(25.4 * 36);
            Settle();
            Assert.EndsWith(" MOA", Mean().Value, StringComparison.Ordinal);
            Assert.Contains(window.KeptBeneath, b => b.EndsWith(" in on the paper at 25.4 yd", StringComparison.Ordinal));
            Assert.DoesNotContain(window.GetLogicalDescendants().OfType<TextBlock>(), b => b.Text?.Contains("needs the shot distance", StringComparison.Ordinal) == true);
            Assert.All(new[] { "Extreme spread", "CEP 50", "CEP 90", "Center from aim" }, name =>
                Assert.Contains("MOA", window.KeptFigures.Single(k => k.Name == name).Value, StringComparison.Ordinal));

            window.ShowSettings();
            Settle();
            window.GetLogicalDescendants().OfType<CheckBox>().Single(c => MainWindow.WordsOf(c).StartsWith("Show a group's size on the paper first", StringComparison.Ordinal)).IsChecked = true;
            Settle();
            window.ShowSettings(false);
            Settle();
            Assert.EndsWith(" in", Mean().Value, StringComparison.Ordinal);
            Assert.Contains(window.KeptBeneath, b => b.EndsWith(" MOA", StringComparison.Ordinal));
            Assert.True(window.SettingsStore.LoadSizeOnPaperFirst());
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 192: Unholy's five "crashes" were one error, Set pressed while the caliber list was open, which set the
    /// box's text from inside the box's own update and threw in Avalonia. Typing "6.5" and pressing Set now sets it, first time, with no error.
    /// </summary>
    [AvaloniaFact]
    public void SetWithTheListOpenSetsTheCaliberFirstTime()
    {
        var (window, path, box) = Typed("6.5");
        try
        {
            // A suggestion highlighted, as the pointer passing over the list or an arrow key does, and then Set rather than the suggestion.
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Settle();
            box.Text = "6.5";
            Settle();
            var set = ((Panel)box.Parent!).Children.OfType<Button>().First(b => b.Content as string == "Set");
            set.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.NotNull(window.Session.State.Calibre);
            Assert.Equal(window.Session.State.Calibre!.Name, box.Text);
            Assert.False(box.IsDropDownOpen);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
