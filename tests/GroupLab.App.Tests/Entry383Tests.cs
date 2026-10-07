using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 383, a tester's report on nightly 174: a marking file held its photo's whole path, which named the user and
/// their folders wherever the file went, and a photo and marking moved together to another folder would not open, with nothing said.
/// </summary>
public sealed class Entry383Tests
{
    private static string Photo(string folder)
    {
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "PXL_20260919_223605180_2.png");
        using var image = new Mat(600, 800, MatType.CV_8UC3, new Scalar(235, 235, 235));
        Cv2.Circle(image, new OpenCvSharp.Point(300, 300), 9, new Scalar(30, 30, 30), -1);
        Cv2.ImWrite(path, image);
        return path;
    }

    private static MainWindow Window()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900, DetectOnOpen = false };
        window.Show();
        return window;
    }

    [AvaloniaFact]
    public void AMarkingHoldsOnlyThePhotosNameAndOpensWhereverItAndThePhotoAreMovedTogether()
    {
        string root = GroupLab.Tests.Support.Temp.Folder("entry383");
        try
        {
            string first = Path.Combine(root, "Users", "someone", "Range photos");
            string photo = Photo(first);
            var window = Window();
            window.OpenImage(photo);
            window.Session.AddShot(new PointD(300, 300), null);
            string marking = Path.Combine(first, "target.grouplab.json");
            File.WriteAllText(marking, MarkingFile.Write(window.Session.State));
            string written = File.ReadAllText(marking);
            Assert.Contains("\"image\": \"PXL_20260919_223605180_2.png\"", written, StringComparison.Ordinal);
            Assert.DoesNotContain("someone", written, StringComparison.Ordinal);
            Assert.DoesNotContain("Range photos", written, StringComparison.Ordinal);

            // The two moved together to another folder open with nothing asked.
            string moved = Path.Combine(root, "elsewhere");
            Directory.CreateDirectory(moved);
            File.Move(photo, Path.Combine(moved, Path.GetFileName(photo)));
            File.Move(marking, Path.Combine(moved, "target.grouplab.json"));
            int asked = 0;
            window.PickMarkingImage = _ => { asked++; return Task.FromResult<string?>(null); };
            window.OpenMarking(Path.Combine(moved, "target.grouplab.json"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, asked);
            Assert.Equal(Path.Combine(moved, Path.GetFileName(photo)), window.Session.State.ImagePath);
            Assert.Single(window.Session.State.Shots);

            // A file from before entry 383, holding a whole path that is no longer there, opens by the photo's name beside it.
            string old = Path.Combine(moved, "old.grouplab.json");
            File.WriteAllText(old, written.Replace("\"image\": \"PXL_20260919_223605180_2.png\"", "\"image\": \"/Users/someone/Range photos/PXL_20260919_223605180_2.png\"", StringComparison.Ordinal));
            window.OpenMarking(old);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, asked);
            Assert.Equal(Path.Combine(moved, Path.GetFileName(photo)), window.Session.State.ImagePath);
            Assert.DoesNotContain("someone", MarkingFile.Write(window.Session.State), StringComparison.Ordinal);

            // A marking alone says plainly that its photo was not found beside it, and asks for it.
            string alone = Path.Combine(root, "alone");
            Directory.CreateDirectory(alone);
            File.Copy(Path.Combine(moved, "target.grouplab.json"), Path.Combine(alone, "target.grouplab.json"));
            string? question = null;
            window.PickMarkingImage = title => { question = title; return Task.FromResult<string?>(Path.Combine(moved, Path.GetFileName(photo))); };
            window.OpenMarking(Path.Combine(alone, "target.grouplab.json"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(MainWindow.PhotoNotFound("PXL_20260919_223605180_2.png"), question);
            Assert.Contains("was not found next to the marking file", question, StringComparison.Ordinal);
            Assert.Equal(Path.Combine(moved, Path.GetFileName(photo)), window.Session.State.ImagePath);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }
}
