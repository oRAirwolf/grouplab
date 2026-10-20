using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 137: opening an image by dropping it on the window or pasting it.
/// <para>
/// <b>No test here touches a real clipboard.</b> Reading one would take whatever happened to be on the machine at that moment, and writing
/// one would take something away from the person running the tests. The clipboard is behind <see cref="IOutsideWorld"/> for exactly the
/// reason the browser is, and these drive the recorder.
/// </para>
/// <para>
/// <b>What is held is that a dropped or pasted file behaves as an opened one</b>, and that the two failures people will actually hit, a file
/// GroupLab cannot read and a clipboard with nothing on it, say so in words rather than throwing.
/// </para>
/// </summary>
public class DropAndPasteTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    /// <summary>
    /// Entry 423 section 0: a drop or a paste onto a tab holding a target opens beside it, in a new tab, and leaves the tab dropped on exactly
    /// as it was; the fixture sheet holds one, so every drop here lands in a new tab.
    /// </summary>
    private static async Task Opened(MainWindow window, int tabs)
    {
        for (int i = 0; i < 400 && !window.Dropping.IsCompleted; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(25);
        }

        // A drop waiting on a question nobody answers fails here rather than hanging the run.
        Assert.True(window.Dropping.IsCompleted, "the drop did not finish in ten seconds" + (window.ProblemOpen ? ": a question is open, " + window.ProblemTitle : ""));
        await window.Dropping;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(tabs + 1, window.Targets.Count);
    }

    [AvaloniaFact]
    public async Task ADroppedImageOpensExactlyAsOpenDoes()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string second = Path.Combine(Path.GetDirectoryName(path)!, "second.png");
        File.Copy(path, second, overwrite: true);
        try
        {
            window.OpenDropped([second]);
            await Opened(window, 1);

            Assert.Equal(second, window.Session.State.ImagePath);
            Assert.DoesNotContain("not an image", window.StatusText, StringComparison.Ordinal);
            Assert.Contains(MainWindow.DroppedBeside, window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Several dropped at once: entry 137 opened the first and counted the rest; entry 420 section 2 opened each in a tab of its own; entry 423
    /// reads their codes first. Two sheets that are not of one set still open a tab each, beside the target dropped on, and the same picture
    /// under two names is one picture. Entry 460 section 2: the two are copies of one single-page design, so they are asked about once, and
    /// Keep a tab each opens a tab each.
    /// </summary>
    [AvaloniaFact]
    public async Task SeveralDroppedOpenOneTabEach()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string a = Path.Combine(Path.GetDirectoryName(path)!, "a.png");
        string b = Path.Combine(Path.GetDirectoryName(path)!, "b.png");
        string same = Path.Combine(Path.GetDirectoryName(path)!, "same-as-a.png");
        File.Copy(path, a, overwrite: true);
        File.Copy(path, same, overwrite: true);
        using (var mat = OpenCvSharp.Cv2.ImRead(path, OpenCvSharp.ImreadModes.Unchanged))
        {
            mat.Set(0, 0, mat.Get<byte>(0, 0) == 0 ? (byte)1 : (byte)0);
            OpenCvSharp.Cv2.ImWrite(b, mat);
        }

        try
        {
            int tabs = window.Targets.Count;
            window.OpenDropped([a, b, same]);
            for (int i = 0; i < 400 && !window.ProblemOpen; i++)
            {
                Dispatcher.UIThread.RunJobs();
                await Task.Delay(25);
            }

            Assert.Equal(GroupLab.Core.Marking.CopiesQuestion.Title(2), window.ProblemTitle);
            window.PressProblemChoice(GroupLab.Core.Marking.CopiesQuestion.Keep);
            await Opened(window, tabs + 1);
            Assert.Equal(b, window.Session.State.ImagePath);
            Assert.Contains("a.png, b.png: a tab each.", window.StatusText, StringComparison.Ordinal);
            Assert.Contains("1 was the same picture as another", window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>A drop of nothing GroupLab can open says what it does open, rather than failing silently.</summary>
    [AvaloniaFact]
    public void ADropOfSomethingElseSaysWhatIsAccepted()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.OpenDropped([]);
            window.OpenFinished();

            Assert.Contains("JPEG and PNG images", window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Only the types Open accepts are taken from a drag; the list is one list so the routes cannot drift apart.</summary>
    [Theory]
    [InlineData("target.png", true)]
    [InlineData("target.JPG", true)]
    [InlineData("target.jpeg", true)]
    [InlineData("target.tif", false)]
    [InlineData("marking.gltm.json", false)]
    [InlineData("notes.txt", false)]
    public void OnlyWhatOpenAcceptsIsTakenFromADrag(string name, bool accepted) =>
        Assert.Equal(accepted, MainWindow.LooksLikeAnImage(name));

    /// <summary>Pasting a file path copied in a file manager opens it, exactly as dropping it would.</summary>
    [AvaloniaFact]
    public async Task PastingAFileOpensIt()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string copy = Path.Combine(Path.GetDirectoryName(path)!, "pasted-file.png");
        File.Copy(path, copy, overwrite: true);
        try
        {
            Outside.Forget();
            Outside.Clipboard = new ClipboardContents([copy], null, null);

            await window.PasteImage();
            await Opened(window, 1);

            Assert.Equal(copy, window.Session.State.ImagePath);
            Assert.Contains(Outside.Asked, a => a.What == "clipboard");
        }
        finally
        {
            Outside.Forget();
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Pasting image data, which is the case with no file behind it: a screenshot, or an image copied from a browser. It is written into
    /// GroupLab's own folder and never beside the person's files, and GroupLab says what that costs.
    /// </summary>
    [AvaloniaFact]
    public async Task PastingImageDataWritesItIntoGroupLabsOwnFolderAndSaysWhatItLacks()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Outside.Forget();
            Outside.Clipboard = new ClipboardContents([], await File.ReadAllBytesAsync(path), ".png");

            await window.PasteImage();
            Assert.Equal(2, window.Targets.Count);

            string opened = Assert.IsType<string>(window.Session.State.ImagePath);
            Assert.Equal(MainWindow.PastedFolder, Path.GetDirectoryName(opened));
            Assert.StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GroupLab"),
                opened, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no file name and no resolution of its own", window.StatusText, StringComparison.Ordinal);
            GroupLab.Tests.Support.Temp.DeleteFile(opened);
        }
        finally
        {
            Outside.Forget();
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>An empty clipboard is the commonest case of all, and it says what to do rather than nothing.</summary>
    [AvaloniaFact]
    public async Task PastingWithNothingOnTheClipboardSaysWhatToCopy()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string was = window.Session.State.ImagePath!;
        try
        {
            Outside.Forget();
            Outside.Clipboard = ClipboardContents.Nothing;

            await window.PasteImage();
            window.OpenFinished();

            Assert.Equal(was, window.Session.State.ImagePath);
            Assert.Contains("There is no image on the clipboard", window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            Outside.Forget();
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// A file that is named like an image and is not one. Before this, every route in threw out of the open call; now all three say the same
    /// sentence and the sheet that was open stays open.
    /// </summary>
    [AvaloniaFact]
    public async Task AFileThatIsNotReallyAnImageIsRefusedInWords()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string liar = Path.Combine(Path.GetDirectoryName(path)!, "not-really.png");
        File.WriteAllText(liar, "this is not a PNG");
        try
        {
            string was = window.Session.State.ImagePath!;
            window.OpenDropped([liar]);
            await window.Dropping;
            Dispatcher.UIThread.RunJobs();

            // The new tab it would have opened in goes again, and the target it was dropped on is showing as it was.
            Assert.Single(window.Targets);
            Assert.Equal(was, window.Session.State.ImagePath);
            Assert.Contains("could not be opened as an image", window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>
    /// Entry 140 section 1.4 asked before a drop replaced unsaved work. Entry 423 section 0 goes further: a drop never replaces a target, so
    /// nothing is asked; the picture opens beside it and the sheet is exactly as it was, in its own tab.
    /// </summary>
    [AvaloniaFact]
    public async Task ADropOntoUnsavedWorkOpensBesideItAndKeepsTheSheet()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string second = Path.Combine(Path.GetDirectoryName(path)!, "kept.png");
        File.Copy(path, second, overwrite: true);
        try
        {
            string was = window.Session.State.ImagePath!;
            int shots = window.Session.State.Shots.Count;
            Assert.True(window.HasUnsavedWork);

            window.OpenDropped([second]);
            Assert.False(window.AskingAboutUnsavedWork);
            await Opened(window, 1);
            Assert.Equal(second, window.Session.State.ImagePath);

            Assert.True(window.ShowTarget(0));
            Settle();
            Assert.Equal(was, window.Session.State.ImagePath);
            Assert.Equal(shots, window.Session.State.Shots.Count);
            Assert.False(window.AskingAboutUnsavedWork);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Entry 137 section 5: both ways in are offered where somebody would look for them.</summary>
    [AvaloniaFact]
    public void BothWaysInAreOfferedOnScreen()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Assert.Contains("Paste an image (Ctrl+V)", window.MenuItems);
            Assert.Contains(window.EmptyCanvasText, t => t.Contains("drop an image here", StringComparison.Ordinal) && t.Contains("Ctrl+V", StringComparison.Ordinal));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
