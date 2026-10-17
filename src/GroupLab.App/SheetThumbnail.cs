using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>One bull of the thumbnail: its index on the marking and its centre and outer radius on the page, in inches.</summary>
internal sealed record ThumbnailBull(int Index, PointD Centre, double RadiusInches);

/// <summary>
/// One shot of the thumbnail: its id, where it is on the page in inches, whether it is excluded, the bull it was given (its index on the
/// sheet, the thumbnail's own numbering) and, in a pooled set, which sheet it came from, counting from 0.
/// </summary>
internal sealed record ThumbnailShot(int Id, PointD At, bool Excluded, int? Bull = null, int Sheet = 0);

/// <summary>
/// The sheet small, NOTES-FROM-PLANNING.md entry 113 section 1, the concept's top-left panel of the analysis: the page drawn from its
/// definition, not from the photograph, as DESIGN.md section 18's archived view is, with every shot on it where it landed. A click on a bull
/// selects the shots on it, on the plot, the table and the sheet alike.
/// Issue 35: a pooled set's sheets lie over one another on the one page, each sheet's shots in its own color as the plot draws them, and a
/// bull's shots are the shots given to it on every sheet, never a shot that only landed nearer it.
/// </summary>
internal sealed class SheetThumbnail : Control
{
    public Bitmap? Artwork { get; set; }

    public double PageWidthInches { get; set; } = 8.5;

    public double PageHeightInches { get; set; } = 11;

    public IReadOnlyList<ThumbnailBull> Bulls { get; set; } = [];

    public IReadOnlyList<ThumbnailShot> Shots { get; set; } = [];

    public IReadOnlySet<int> Selected { get; set; } = new HashSet<int>();

    /// <summary>Raised when a click on a bull picks the shots on it.</summary>
    public event EventHandler<IReadOnlyList<int>>? ShotsClicked;

    public SheetThumbnail()
    {
        ClipToBounds = true;
        Height = 240;
        PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && Pick(e.GetPosition(this)) is { Count: > 0 } picked)
            {
                ShotsClicked?.Invoke(this, picked);
                e.Handled = true;
            }
        };
    }

    /// <summary>The page's rectangle in the control, as large as fits, centred.</summary>
    private (Rect Page, double Scale) Frame()
    {
        double scale = Math.Min(Bounds.Width / PageWidthInches, Bounds.Height / PageHeightInches);
        double width = PageWidthInches * scale, height = PageHeightInches * scale;
        return (new Rect((Bounds.Width - width) / 2, (Bounds.Height - height) / 2, width, height), scale);
    }

    private Point ToScreen(PointD inches)
    {
        var (page, scale) = Frame();
        return new Point(page.X + (inches.X * scale), page.Y + (inches.Y * scale));
    }

    /// <summary>How near a shot's dot a click off every bull has to be to pick that shot, pixels.</summary>
    private const double ShotReach = 6;

    /// <summary>The shots on the bull under a point; off every bull, the shot whose dot is under it; or none.</summary>
    internal IReadOnlyList<int> Pick(Point at)
    {
        var (page, scale) = Frame();
        if (scale <= 0)
        {
            return [];
        }

        var inches = new PointD((at.X - page.X) / scale, (at.Y - page.Y) / scale);
        var bull = Bulls
            .Select(b => (Bull: b, Distance: Math.Sqrt(Math.Pow(b.Centre.X - inches.X, 2) + Math.Pow(b.Centre.Y - inches.Y, 2))))
            .Where(b => b.Distance <= Math.Max(b.Bull.RadiusInches, 0.3))
            .OrderBy(b => b.Distance)
            .Select(b => b.Bull)
            .FirstOrDefault();
        if (bull is not null)
        {
            return [.. ShotsOn(bull)];
        }

        // Issue 35: a shot wide of every bull can still be picked on its own dot.
        var shot = Shots
            .Select(s => (Shot: s, Distance: Math.Sqrt(Math.Pow(ToScreen(s.At).X - at.X, 2) + Math.Pow(ToScreen(s.At).Y - at.Y, 2))))
            .Where(s => s.Distance <= ShotReach)
            .OrderBy(s => s.Distance)
            .Select(s => s.Shot)
            .FirstOrDefault();
        return shot is null ? [] : [shot.Id];
    }

    /// <summary>
    /// The shots given to a bull, on every sheet of a pooled set. Issue 35: this was the shots nearest the bull, so a shot wide of its own bull
    /// was picked with its neighbour's; a shot whose bull is not known still goes by nearness.
    /// </summary>
    private IEnumerable<int> ShotsOn(ThumbnailBull bull) => Shots
        .Where(s => s.Bull is { } own ? own == bull.Index : Bulls.MinBy(b => Math.Pow(b.Centre.X - s.At.X, 2) + Math.Pow(b.Centre.Y - s.At.Y, 2)) == bull)
        .Select(s => s.Id);

    /// <summary>The sheets the shots come from, in order, where they come from more than one: a pooled set.</summary>
    internal IReadOnlyList<int> PooledSheets => Shots.Select(s => s.Sheet).Distinct().Order().ToList() is { Count: > 1 } sheets ? sheets : [];

    /// <summary>Picks the shots on one bull as a click there does, for the headless tests.</summary>
    internal IReadOnlyList<int> ClickBull(int index)
    {
        var bull = Bulls.First(b => b.Index == index);
        var picked = Pick(ToScreen(bull.Centre));
        ShotsClicked?.Invoke(this, picked);
        return picked;
    }

    /// <summary>Picks what a click on one shot's dot picks, for the headless tests.</summary>
    internal IReadOnlyList<int> ClickShot(int id)
    {
        var picked = Pick(ToScreen(Shots.First(s => s.Id == id).At));
        ShotsClicked?.Invoke(this, picked);
        return picked;
    }

    /// <summary>Where a point on the page in inches is drawn in the control, for the headless tests.</summary>
    internal Point Drawn(PointD inches) => ToScreen(inches);

    public override void Render(DrawingContext context)
    {
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));
        var (page, _) = Frame();
        if (page.Width <= 0)
        {
            return;
        }

        context.FillRectangle(new SolidColorBrush(Tokens.Paper), page);
        if (Artwork is { } artwork)
        {
            context.DrawImage(artwork, new Rect(artwork.Size), page);
        }

        // Issue 35: a pooled set's shots in their sheet's color, the colors the plot gives them.
        var sheets = PooledSheets.ToList();
        var colours = Tokens.Loads(ActualThemeVariant);
        foreach (var shot in Shots)
        {
            var at = ToScreen(shot.At);
            if (shot.Excluded)
            {
                Marks.Ring(context, Marks.Excluded, at, 2.5, Tokens.MarkCoreWidth, Marks.Dashed);
            }
            else
            {
                Marks.Dot(context, sheets.Count > 0 ? new SolidColorBrush(colours[sheets.IndexOf(shot.Sheet) % colours.Count]) : Marks.Impact, at, 2.4);
            }
        }

        // Entry 425: at this size a change of color barely shows, so the selected shots are ringed several times a shot's size, with the
        // crosshair, after every other shot.
        foreach (var shot in Shots.Where(s => Selected.Contains(s.Id)))
        {
            Marks.Selection(context, ActualThemeVariant, ToScreen(shot.At), Tokens.SelectionThumbnailRing, 3.5);
        }
    }
}
