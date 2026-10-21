using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 item 6A: the tour's card is placed beside the outlined part, on whichever side has room, never over
/// the part, with a small pointer toward it, and at the bottom of the window where no side has room. The rule is a pure function, held
/// here case by case and over a sweep of parts, and the whole tour is walked headless at two window sizes to hold the screen to it.
/// </summary>
public class Entry465DemoTourTests
{
    private static readonly Size Window = new(1280, 720);
    private static readonly Size Card = new(360, 180);
    private const double Margin = 12;

    private static Rect CardAt(DemoCardPlace place) => new(place.At, Card);

    private static bool Inside(Rect card, Size room) =>
        card.X >= Margin - 0.01 && card.Y >= Margin - 0.01 && card.Right <= room.Width - Margin + 0.01 && card.Bottom <= room.Height - Margin + 0.01;

    private static bool Overlaps(Rect a, Rect b) => a.Intersect(b) is { Width: > 0.01, Height: > 0.01 };

    [Fact]
    public void ACardGoesBesideOnTheSideWithMostRoomAndPointsAtThePart()
    {
        // A panel on the left, taller than the card: beside it, on the right, where the room is.
        var left = new Rect(60, 200, 120, 300);
        var place = DemoCardPlacement.Place(Window, left, Card, Margin);
        Assert.Equal(DemoCardSide.Right, place.Side);
        Assert.Equal(left.Right + DemoCardPlacement.Gap, place.At.X);
        Assert.Equal(left.Center.Y - Card.Height / 2, place.At.Y);
        Assert.Equal(new Point(place.At.X, left.Center.Y), place.Pointer);

        // A column on the right: the left.
        var right = new Rect(1040, 120, 220, 400);
        place = DemoCardPlacement.Place(Window, right, Card, Margin);
        Assert.Equal(DemoCardSide.Left, place.Side);
        Assert.Equal(right.X - DemoCardPlacement.Gap, CardAt(place).Right, 3);
        Assert.Equal(CardAt(place).Right, place.Pointer!.Value.X);

        // A wide bar along the top: below, centred on it and kept inside the window.
        var top = new Rect(0, 40, 1280, 40);
        place = DemoCardPlacement.Place(Window, top, Card, Margin);
        Assert.Equal(DemoCardSide.Below, place.Side);
        Assert.Equal(top.Bottom + DemoCardPlacement.Gap, place.At.Y);
        Assert.Equal((Window.Width - Card.Width) / 2, place.At.X);
        Assert.Equal(new Point(Window.Width / 2, place.At.Y), place.Pointer);

        // The line along the bottom: above.
        var status = new Rect(60, 690, 900, 20);
        place = DemoCardPlacement.Place(Window, status, Card, Margin);
        Assert.Equal(DemoCardSide.Above, place.Side);
        Assert.Equal(status.Y - DemoCardPlacement.Gap, CardAt(place).Bottom, 3);
        Assert.Equal(CardAt(place).Bottom, place.Pointer!.Value.Y);

        // A button low on the right, less tall than the card: above it, though the left has more room, so the card lies along it.
        var button = new Rect(1100, 600, 120, 30);
        place = DemoCardPlacement.Place(Window, button, Card, Margin);
        Assert.Equal(DemoCardSide.Above, place.Side);
        Assert.Equal(Window.Width - Margin - Card.Width, place.At.X);

        // A small part in a corner: below it, inside the window, and the pointer on the card's edge clear of its rounded corner.
        var corner = new Rect(4, 4, 30, 30);
        place = DemoCardPlacement.Place(Window, corner, Card, Margin);
        var card = CardAt(place);
        Assert.Equal(DemoCardSide.Below, place.Side);
        Assert.True(Inside(card, Window));
        Assert.False(Overlaps(card, corner));
        Assert.NotNull(place.Pointer);
        Assert.Equal(card.Y, place.Pointer!.Value.Y);
        Assert.InRange(place.Pointer!.Value.X, card.X + DemoCardPlacement.PointerReach, card.Right - DemoCardPlacement.PointerReach);
    }

    [Fact]
    public void WhereNoSideHasRoomTheCardGoesToTheBottomCoveringLeastOfThePart()
    {
        // The plot of the analysis at 1280 by 720: no side takes a card 360 wide and 180 high.
        var plot = new Rect(352, 81, 557, 500);
        var place = DemoCardPlacement.Place(Window, plot, Card, Margin);
        var card = CardAt(place);
        Assert.Equal(DemoCardSide.Bottom, place.Side);
        Assert.Equal(Window.Height - Margin, card.Bottom, 3);
        Assert.True(Inside(card, Window));
        // At the right it covers a sliver of the plot; at the left a strip, and in the middle far more.
        Assert.Equal(Window.Width - Margin - Card.Width, card.X);
        Assert.NotNull(place.Pointer);
        Assert.Equal(card.Y, place.Pointer!.Value.Y);

        // A part filling the window: the bottom, in the middle, pointing up at the part's middle above it.
        place = DemoCardPlacement.Place(Window, new Rect(0, 0, 1280, 720), Card, Margin);
        Assert.Equal(DemoCardSide.Bottom, place.Side);
        Assert.Equal((Window.Width - Card.Width) / 2, place.At.X);
        Assert.Equal(new Point(Window.Width / 2, place.At.Y), place.Pointer);

        // A part scrolled out of the window is as none: the card stays inside the window.
        place = DemoCardPlacement.Place(Window, new Rect(40, 2100, 300, 40), Card, Margin);
        Assert.Equal(DemoCardSide.Bottom, place.Side);
        Assert.True(Inside(CardAt(place), Window));
        Assert.Null(place.Pointer);

        // No part on the screen: the middle of the bottom.
        place = DemoCardPlacement.Place(Window, null, Card, Margin);
        Assert.Equal(DemoCardSide.Bottom, place.Side);
        Assert.Equal((Window.Width - Card.Width) / 2, place.At.X);
        Assert.Null(place.Pointer);
    }

    /// <summary>Over a sweep of parts of every size and place: a card beside is never over its part and always inside the window.</summary>
    [Fact]
    public void ACardBesideItsPartIsNeverOverItAndAlwaysInsideTheWindow()
    {
        int beside = 0, bottom = 0;
        foreach (var room in new[] { new Size(1060, 720), new Size(1280, 720), new Size(1920, 1080) })
        {
            for (double w = 20; w < room.Width; w += 137)
            {
                for (double h = 16; h < room.Height; h += 91)
                {
                    for (double x = 0; x + w <= room.Width; x += 173)
                    {
                        for (double y = 0; y + h <= room.Height; y += 119)
                        {
                            var part = new Rect(x, y, w, h);
                            var place = DemoCardPlacement.Place(room, part, Card, Margin);
                            var card = CardAt(place);
                            Assert.True(Inside(card, room), $"{part} in {room}: the card at {card} is outside the window");
                            if (place.Side == DemoCardSide.Bottom)
                            {
                                bottom++;
                                continue;
                            }

                            beside++;
                            Assert.False(Overlaps(card, part), $"{part} in {room}: the card at {card} ({place.Side}) is over it");
                            Assert.NotNull(place.Pointer);
                            var tip = place.Pointer!.Value;
                            Assert.True(tip.X >= card.X - 0.01 && tip.X <= card.Right + 0.01 && tip.Y >= card.Y - 0.01 && tip.Y <= card.Bottom + 0.01,
                                $"{part} in {room}: the pointer at {tip} is off the card's edge");
                        }
                    }
                }
            }
        }

        Assert.True(beside > bottom, $"{beside} beside and {bottom} at the bottom");
    }

    private static void Wait(Task task)
    {
        var until = DateTime.UtcNow.AddMinutes(1);
        while (!task.IsCompleted && DateTime.UtcNow < until)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(5);
        }

        Dispatcher.UIThread.RunJobs();
        Assert.True(task.IsCompleted, "the demo did not finish its step within a minute");
        task.GetAwaiter().GetResult();
    }

    /// <summary>The whole tour on screen: at each step the card is inside the window, and beside its outline it is never over it.</summary>
    [AvaloniaTheory]
    [InlineData(1280, 720)]
    [InlineData(1920, 1080)]
    public void EveryStepOfTheTourKeepsItsCardOffTheOutlinedPart(int width, int height)
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry465-{Guid.NewGuid():N}");
        var window = new MainWindow(new AppSettingsStore(Path.Combine(folder, "settings.json"))) { Width = width, Height = height };
        var found = new List<string>();
        int beside = 0;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Wait(window.StartDemo());
            for (int i = 0; i < MainWindow.DemoSteps.Count; i++)
            {
                for (int pass = 0; pass < 3; pass++)
                {
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                }

                var step = MainWindow.DemoSteps[i];
                var card = new Rect(window.DemoCard.TranslatePoint(default, window) ?? default, window.DemoCard.Bounds.Size);
                if (card.X < 0 || card.Y < 0 || card.Right > window.Bounds.Width + 1 || card.Bottom > window.Bounds.Height + 1)
                {
                    found.Add($"step {i + 1}, {step.Title}: the card at {card} is outside the window");
                }

                if (window.DemoSpotlit is { } part && window.DemoCardSideShown != DemoCardSide.Bottom)
                {
                    beside++;
                    var ring = new Rect(part.TranslatePoint(default, window) ?? default, part.Bounds.Size).Inflate(4);
                    if (Overlaps(card, ring))
                    {
                        found.Add($"step {i + 1}, {step.Title}: the card at {card} is over its outline {ring}");
                    }

                    if (!window.DemoPointer.IsVisible)
                    {
                        found.Add($"step {i + 1}, {step.Title}: the card is beside its outline with no pointer");
                    }
                }

                Wait(window.DemoNext());
            }
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }

        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
        Assert.True(beside >= MainWindow.DemoSteps.Count / 2, $"only {beside} of {MainWindow.DemoSteps.Count} steps had a card beside its outline");
    }
}
