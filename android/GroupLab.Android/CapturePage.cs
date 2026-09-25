using System.Globalization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A4: the first place along the bottom. The caliber and the distance, remembered from the last
/// target, then take a picture with the capture screen or choose one already on the phone; either is analyzed at the working size, the
/// result shown, and saved in Sessions. The caliber comes first because it is the one answer that changes what GroupLab finds (entry 131
/// section 6.3), as on the desktop.
/// </summary>
public sealed class CapturePage : UserControl
{
    private readonly TextBlock status = Screens.Line("");
    private readonly AutoCompleteBox calibre = new()
    {
        ItemsSource = CartridgeTable.Suggest(""),
        FilterMode = AutoCompleteFilterMode.Contains,
        PlaceholderText = "Caliber, e.g. 6.5 Creedmoor or .308",
        MinHeight = Screens.Touch,
    };

    private readonly TextBox distance = new() { MinHeight = Screens.Touch, PlaceholderText = "Distance" };
    private readonly Control start;

    public CapturePage()
    {
        var units = App.Settings.LoadUnits();
        var (typed, inches) = App.Settings.LoadShotSetup();
        calibre.Text = typed ?? "";
        distance.PlaceholderText = $"Distance in {UnitSettings.Symbol(units.Distance)}";
        distance.Text = inches is { } d ? UnitSettings.DistanceFromInches(d, units.Distance).ToString("0.#", CultureInfo.CurrentCulture) : "";
        start = Screens.Page(new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Screens.Heading("Capture"),
                Screens.Line("Photograph a GroupLab target and GroupLab finds the holes and measures the group. Hold the phone square over the sheet; the words at the top of the camera say what to change, and it takes the picture itself when everything is right."),
                calibre,
                distance,
                Screens.Choice("Take a picture", Camera),
                Screens.Choice("Choose a photograph", () => _ = Choose()),
                status,
            },
        });
        Content = start;
    }

    /// <summary>The caliber and distance as typed, remembered for the next target; null where the caliber cannot be read, with why.</summary>
    private ShotSetup? Setup()
    {
        var units = App.Settings.LoadUnits();
        var chosen = Calibre.Parse(calibre.Text, out string? why);
        if (why is not null)
        {
            status.Text = why;
            return null;
        }

        double? inches = double.TryParse(distance.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) && value > 0
            ? UnitSettings.DistanceToInches(value, units.Distance)
            : null;
        App.Settings.SaveShotSetup(calibre.Text, inches);
        return new ShotSetup(chosen, inches);
    }

    private void Camera()
    {
        if (Setup() is not { } setup)
        {
            return;
        }

        if (!MainActivity.CameraAllowed())
        {
            status.Text = "GroupLab needs the camera to take the picture. Allow it, then press Take a picture again.";
            return;
        }

        Content = new CameraView(path => _ = Analyze(path, setup), () => Content = start);
    }

    private async Task Choose()
    {
        if (Setup() is not { } setup || TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a photograph of a target",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
        });
        if (files.Count == 0)
        {
            return;
        }

        // The picker hands a content address, not a path; the picture is copied into the cache, analyzed, and the copy deleted.
        string copy = Path.Combine(global::Android.App.Application.Context.CacheDir!.AbsolutePath, "chosen" + Path.GetExtension(files[0].Name));
        await using (var from = await files[0].OpenReadAsync())
        await using (var to = File.Create(copy))
        {
            await from.CopyToAsync(to);
        }

        await Analyze(copy, setup);
    }

    private async Task Analyze(string photo, ShotSetup setup)
    {
        Content = Screens.Words("Reading the sheet", "Finding the sheet's markers and the holes. This takes a few seconds.");
        var units = App.Settings.LoadUnits();
        PhoneResult result;
        try
        {
            result = await Task.Run(() => PhoneAnalysis.Run(photo, setup, units, App.Survey, CancellationToken.None));
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or OpenCvSharp.OpenCVException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "phone.detect", e);
            result = new PhoneResult(MarkingState.Empty, null, "The picture could not be analyzed: " + e.Message, null);
        }
        finally
        {
            // The photograph itself is never kept: the session keeps its working copy.
            File.Delete(photo);
        }

        Dispatcher.UIThread.Post(() => Content = new ResultView(result, setup, units, () => Content = start));
    }
}
