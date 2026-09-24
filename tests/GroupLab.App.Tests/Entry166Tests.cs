using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 166, the first macOS tester's answers: Command Z did nothing, because every shortcut read the Control key and a
/// Mac's Command key arrives as Meta; undo gave no sign of what it would undo; and pinch zoom had never been built on any platform.
/// </summary>
public class Entry166Tests
{
    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    private static RawInputModifiers Command => (RawInputModifiers)CommandKey.Modifier;

    [Fact]
    public void TheCommandKeyIsMetaOnAMacAndControlElsewhere()
    {
        Assert.Equal(KeyModifiers.Meta, CommandKey.For(mac: true));
        Assert.Equal(KeyModifiers.Control, CommandKey.For(mac: false));
        Assert.Equal("⌘Z", CommandKey.Label("Z", mac: true));
        Assert.Equal("Ctrl+Z", CommandKey.Label("Z", mac: false));
        Assert.Equal("⇧⌘Z", CommandKey.RedoLabelFor(mac: true));
        Assert.Equal("Ctrl+Y", CommandKey.RedoLabelFor(mac: false));
    }

    [AvaloniaFact]
    public void TheCommandKeyUndoesAndRedoesAndTheButtonSaysWhat()
    {
        var window = NewWindow();
        window.Show();
        var session = window.Session;
        session.Load(MarkingState.Empty with { ImagePath = @"C:\a-target.png", Bulls = [new BullAim(0, "6", new PointD(50, 50))] });
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.UndoButtonState.Enabled);
        Assert.StartsWith("Nothing to undo", window.UndoButtonState.Tip);

        int id = session.AddShot(new PointD(51, 50), 0);
        session.MoveShot(id, new PointD(53, 50));
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.UndoButtonState.Enabled);
        Assert.Equal($"Undo: move shot 6 ({CommandKey.Label("Z")})", window.UndoButtonState.Tip);

        window.KeyPressQwerty(PhysicalKey.Z, Command);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new PointD(51, 50), session.State.Find(id)!.Image);

        // Shift Command Z is redo, and never read as undo.
        window.KeyPressQwerty(PhysicalKey.Z, Command | RawInputModifiers.Shift);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new PointD(53, 50), session.State.Find(id)!.Image);
        window.Close();
    }

    [Fact]
    public void AScrollZoomsOrPansAsThePlatformExpects()
    {
        // Command with any scroll zooms, everywhere: a Windows precision touchpad's pinch arrives as Control with a wheel.
        Assert.Equal(WheelAction.Zoom, SheetGestures.ForWheel(new Vector(0, 0.3), command: true, mac: false));
        Assert.Equal(WheelAction.Zoom, SheetGestures.ForWheel(new Vector(0, 0.3), command: true, mac: true));

        // On a Mac a plain scroll pans, trackpad or wheel.
        Assert.Equal(WheelAction.Pan, SheetGestures.ForWheel(new Vector(0, 1), command: false, mac: true));
        Assert.Equal(WheelAction.Pan, SheetGestures.ForWheel(new Vector(0.4, -0.2), command: false, mac: true));

        // Elsewhere a wheel notch zooms, as it always has, and a touchpad's fractional drag pans.
        Assert.Equal(WheelAction.Zoom, SheetGestures.ForWheel(new Vector(0, -1), command: false, mac: false));
        Assert.Equal(WheelAction.Pan, SheetGestures.ForWheel(new Vector(0, 0.25), command: false, mac: false));
        Assert.Equal(WheelAction.Pan, SheetGestures.ForWheel(new Vector(1, 0), command: false, mac: false));

        Assert.Equal(1.2, SheetGestures.ZoomFactor(1), 12);
        Assert.Equal(1 / 1.2, SheetGestures.ZoomFactor(-1), 12);
        Assert.True(SheetGestures.MagnifyFactor(0.1) > 1 && SheetGestures.MagnifyFactor(-0.1) < 1);
    }

    [AvaloniaFact]
    public void TheSheetZoomsAboutThePointerAndPansWithTheFingers()
    {
        var window = NewWindow();
        window.Show();
        var canvas = window.Canvas;
        window.Session.Load(MarkingState.Empty with { ImagePath = @"C:\a-target.png" });
        Dispatcher.UIThread.RunJobs();
        var about = new Point(200, 150);
        var under = canvas.ToImage(about);

        // A trackpad pinch zooms, and the point under the fingers stays where it was.
        canvas.Magnify(0.2, about);
        var after = canvas.ToControl(under);
        Assert.Equal(about.X, after.X, 6);
        Assert.Equal(about.Y, after.Y, 6);

        // Command with a scroll zooms on every platform; a fractional scroll with no key pans on every platform.
        Assert.Equal(WheelAction.Zoom, canvas.Scroll(new Vector(0, 0.5), CommandKey.Modifier, about));
        var before = canvas.ToControl(under);
        Assert.Equal(WheelAction.Pan, canvas.Scroll(new Vector(0, 0.5), KeyModifiers.None, about));
        var panned = canvas.ToControl(under);
        Assert.Equal(before.X, panned.X, 6);
        Assert.Equal(before.Y + (0.5 * SheetGestures.PixelsPerUnit), panned.Y, 6);
        window.Close();
    }
}
