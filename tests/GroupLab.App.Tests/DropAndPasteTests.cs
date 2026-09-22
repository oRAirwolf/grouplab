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
    /// Entry 140 section 1.4 in the way: a sheet with edits nobody has saved asks before it is replaced, and a drop or a paste is a replacing
    /// like any other. The tests answer that question rather than working round it, because the asking is the point.
    /// </summary>
    private static void Discard(MainWindow window)
    {
        Assert.True(window.HasUnsavedWork, "the fixture sheet had nothing unsaved, so nothing would ask before replacing it");
        window.AnswerDiscard();
        Settle();
    }

    [AvaloniaFact]
    public void ADroppedImageOpensExactlyAsOpenDoes()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string second = Path.Combine(Path.GetDirectoryName(path)!, "second.png");
        File.Copy(path, second, overwrite: true);
        try
        {
            window.OpenDropped([second]);
            Discard(window);

            Assert.Equal(second, window.Session.State.ImagePath);
            Assert.DoesNotContain("not an image", window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Several dropped at once: the first opens and the rest are counted, rather than a dialog in the way of a quick gesture.</summary>
    [AvaloniaFact]
    public void SeveralDroppedOpenTheFirstAndSayHowManyWereNot()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string a = Path.Combine(Path.GetDirectoryName(path)!, "a.png");
        string b = Path.Combine(Path.GetDirectoryName(path)!, "b.png");
        File.Copy(path, a, overwrite: true);
        File.Copy(path, b, overwrite: true);
        try
        {
            window.OpenDropped([a, b]);
            Discard(window);

            Assert.Equal(a, window.Session.State.ImagePath);
            Assert.Contains("1 other file was dropped with it and not opened", window.StatusText, StringComparison.Ordinal);
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
            Settle();

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
            Discard(window);

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
            Discard(window);

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
            Settle();

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
    public void AFileThatIsNotReallyAnImageIsRefusedInWords()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string liar = Path.Combine(Path.GetDirectoryName(path)!, "not-really.png");
        File.WriteAllText(liar, "this is not a PNG");
        try
        {
            string was = window.Session.State.ImagePath!;
            window.OpenDropped([liar]);
            Discard(window);

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
    /// A drop is a replacing like any other, so entry 140 section 1.4's question comes first. Cancel leaves the sheet exactly as it was,
    /// which is the property that makes a careless drop safe.
    /// </summary>
    [AvaloniaFact]
    public void ADropOntoUnsavedWorkAsksFirstAndCancelKeepsTheSheet()
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
            Settle();

            // It has not opened anything yet: the question is on screen.
            Assert.Equal(was, window.Session.State.ImagePath);

            window.AnswerCancel();
            Settle();
            Assert.Equal(was, window.Session.State.ImagePath);
            Assert.Equal(shots, window.Session.State.Shots.Count);
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
