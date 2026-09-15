using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 37 section 5: an image published with a stated sheet size in its provenance record is offered that size
/// as its reference rectangle, and an image without one is offered nothing.
/// </summary>
public sealed class StatedSheetSizeTests : IDisposable
{
    private readonly string root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-stated-app-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static string Image(string directory, string name)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, name);
        using var mat = new OpenCvSharp.Mat(60, 80, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(230));
        OpenCvSharp.Cv2.ImWrite(path, mat);
        return path;
    }

    private static Button? Offer(Window window) =>
        window.GetLogicalDescendants().OfType<Button>().SingleOrDefault(b => (b.Content as string)?.StartsWith("Use the stated sheet size", StringComparison.Ordinal) == true);

    [AvaloniaFact]
    public void TheRectangleToolOffersTheSheetSizeTheContributorStated()
    {
        string published = Path.Combine(root, "donated", "2026-09-15_5068047f");
        string image = Image(published, "001_IMG_1580.png");
        var record = new JsonObject
        {
            ["files"] = new JsonArray(new JsonObject { ["storedName"] = "001_IMG_1580.png" }),
            ["statedSheetSize"] = StatedSheetSize.Parse("Action Target PR-BE6 17.5x23”")!.ToJson(),
        };
        File.WriteAllText(Path.Combine(published, PublicationCheck.ProvenanceFile), record.ToJsonString());
        var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.OpenImage(image);
        window.AskRectangle();
        Dispatcher.UIThread.RunJobs();
        var offer = Offer(window);
        Assert.NotNull(offer);
        offer.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["17.5", "23"], window.GetLogicalDescendants().OfType<TextBox>().Select(t => t.Text).Where(t => t is "17.5" or "23"));

        window.OpenImage(Image(Path.Combine(root, "elsewhere"), "photo.png"));
        window.AskRectangle();
        Dispatcher.UIThread.RunJobs();
        Assert.Null(Offer(window));
        window.Close();
    }
}
