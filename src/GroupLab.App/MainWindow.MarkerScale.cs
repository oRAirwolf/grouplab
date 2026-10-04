using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Library;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.ScaleMarkers;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 365: a target GroupLab does not know, marked by hand, takes its scale from markers in the photo by itself:
/// corner brackets, scale bars, a measured board's stickers (C above all, which needs nothing placed) or a bank card. A card is blanked out
/// first, and the picture GroupLab keeps, shows, saves and sends from then on is a copy without it (section D).
/// </summary>
public partial class MainWindow
{
    /// <summary>A picture with a card in it that could not be blanked: it is never sent (entry 365 section D).</summary>
    private string? cardNotBlanked;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> CardChecked = new(StringComparer.Ordinal);

    /// <summary>
    /// Entry 365 section D, without exception: a picture is never sent with a bank card in it, whatever was or was not done with it on the
    /// screen. Checked once a picture, at a reduced size, and a picture that cannot be read is taken to have none (it cannot be sent either).
    /// </summary>
    internal static bool HasCard(string path) => CardChecked.GetOrAdd(path, p =>
    {
        try
        {
            var (g, _) = GroupLab.Cli.Imaging.ImageLoader.Load(p, 4);
            bool found = ScaleMarkerFinder.Card(g) is not null;
            if (found)
            {
                DiagnosticLog.Info("markers.card.held");
            }

            return found;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or OpenCvSharp.OpenCVException or UnauthorizedAccessException)
        {
            return false;
        }
    });

    /// <summary>Looks for markers off the screen's thread, where the picture has no scale yet.</summary>
    private void ScaleFromMarkers()
    {
        if (grey is not { } g || valueImage is not { } v || session.State.ImagePath is not { } path || session.State.Scale is not null)
        {
            return;
        }

        var printer = settingsStore.LoadChosenPrinter();
        var boards = settingsStore.LoadBoards();
        int? orientation = session.State.ExifOrientation;
        Task.Run(() =>
        {
            var (finding, said, sighting) = ScaleMarkerFinder.Read(g, null, printer, boards, v);
            string? blanked = sighting.Blanked is { } outline ? Blanked(path, outline, g.Width, g.Height) : null;
            return (finding, said, blanked, card: sighting.Card is not null);
        }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
        {
            if (!t.IsCompletedSuccessfully || session.State.ImagePath != path || session.State.Scale is not null)
            {
                return;
            }

            var (finding, said, blanked, card) = t.Result;
            if (card && blanked is null)
            {
                cardNotBlanked = path;
                finding = null;
            }

            if (blanked is not null)
            {
                session.Open(blanked, orientation);
                using (var stream = File.OpenRead(blanked))
                {
                    canvas.SetImage(new Bitmap(stream), v);
                }

                PictureOpened(blanked);
            }

            if (finding is not null)
            {
                double u = finding.UncertaintyAt(null);
                session.SetScale(MarkerReference.Upright(finding.Plane.ImageToInches, new PointD(g.Width / 2.0, g.Height / 2.0), finding.Says(u)));
                status.Text = finding.Says(u) + " Mark the point of aim, then tap each impact.";
            }
            else if (said is not null)
            {
                status.Text = said;
            }

            if (blanked is not null)
            {
                status.Text += " " + ScaleMarkerWords.CardBlanked;
            }

            DiagnosticLog.Info("markers.scale", ("found", finding is not null), ("card", blanked is not null));
        }), TaskScheduler.Default);
    }

    /// <summary>The picture with the card blanked, written where GroupLab keeps its own copies, as the stored pixels are with no turn.</summary>
    private static string? Blanked(string path, IReadOnlyList<PointD> outline, int width, int height)
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GroupLab", "blanked");
            Directory.CreateDirectory(folder);
            string into = Path.Combine(folder, Path.GetFileNameWithoutExtension(path) + "-card-blanked.png");
            File.WriteAllBytes(into, ScaleMarkerFinder.BlankedFile(path, [.. outline.Select(p => new PointD(p.X / width, p.Y / height))], 1));
            return into;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "markers.blank", e);
            return null;
        }
    }
}
