using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>One bull of the thumbnail: its index on the marking and its centre and outer radius on the page, in inches.</summary>
internal sealed record ThumbnailBull(int Index, PointD Centre, double RadiusInches);

/// <summary>One shot of the thumbnail: its id, where it is on the page in inches, and whether it is excluded.</summary>
internal sealed record ThumbnailShot(int Id, PointD At, bool Excluded);

/// <summary>
/// The sheet small, NOTES-FROM-PLANNING.md entry 113 section 1, the concept's top-left panel of the analysis: the page drawn from its
/// definition, not from the photograph, as DESIGN.md section 18's archived view is, with every shot on it where it landed. A click on a bull
/// selects the shots on it, on the plot, the table and the sheet alike.
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

    /// <summary>The shots on the bull under a point, or none when the point is not on a bull.</summary>
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
        return bull is null ? [] : [.. ShotsOn(bull)];
    }

    /// <summary>The shots nearest the bull of all the bulls, which is the thumbnail's sense of which shots a bull holds.</summary>
    private IEnumerable<int> ShotsOn(ThumbnailBull bull) => Shots
        .Where(s => Bulls.MinBy(b => Math.Pow(b.Centre.X - s.At.X, 2) + Math.Pow(b.Centre.Y - s.At.Y, 2)) == bull)
        .Select(s => s.Id);

    /// <summary>Picks the shots on one bull as a click there does, for the headless tests.</summary>
    internal IReadOnlyList<int> ClickBull(int index)
    {
        var bull = Bulls.First(b => b.Index == index);
        var picked = Pick(ToScreen(bull.Centre));
        ShotsClicked?.Invoke(this, picked);
        return picked;
    }

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

        foreach (var shot in Shots)
        {
            bool selected = Selected.Contains(shot.Id);
            var at = ToScreen(shot.At);
            if (shot.Excluded)
            {
                Marks.Ring(context, selected ? Marks.Selected : Marks.Excluded, at, 2.5, Tokens.MarkCoreWidth, Marks.Dashed);
            }
            else
            {
                Marks.Dot(context, selected ? Marks.Selected : Marks.Impact, at, selected ? 3.2 : 2.4);
            }
        }
    }
}
