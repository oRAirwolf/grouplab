using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 item 6A on the phone: the tour's card, under or over each outlined control, has a small pointer on
/// its edge toward the outline, and wherever it points the card is clear of the outline, at the ordinary text size and the largest.
/// </summary>
public class Entry465PhoneDemoTests
{
    private static async Task Until(Func<bool> done, int seconds = 20)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!done() && clock.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(100);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EveryCardPointsAtItsOutlineFromOutsideIt(bool largest)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        Phone.Settings.SaveDemoStep(0);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        if (largest)
        {
            AndroidText.LargestUntilClosed(window);
        }

        window.Show();
        Dispatcher.UIThread.RunJobs();
        var found = new List<string>();
        int pointed = 0;
        try
        {
            await shell.Demo.Start();
            for (int i = 0; i < PhoneDemo.Steps.Count; i++)
            {
                var card = shell.Demo.Card;
                await Until(() => shell.Demo.Spotlit is not null && !double.IsNaN(Canvas.GetLeft(card)) && card.Bounds.Width > 0);
                await Task.Delay(200);
                Dispatcher.UIThread.RunJobs();
                var pointer = shell.Demo.Pointer;
                if (shell.Demo.Spotlit is not { } part || !pointer.IsVisible)
                {
                    await shell.Demo.Next();
                    continue;
                }

                pointed++;
                var layer = shell.Demo.Layer;
                var box = new Rect(Canvas.GetLeft(card), Canvas.GetTop(card), card.Bounds.Width, card.Bounds.Height);
                var ring = new Rect(part.TranslatePoint(default, layer) ?? default, part.Bounds.Size).Inflate(4);
                var tip = new Point(Canvas.GetLeft(pointer) + pointer.Width / 2, Canvas.GetTop(pointer) + pointer.Height / 2);
                bool onTop = Math.Abs(tip.Y - box.Y) < 1, onBottom = Math.Abs(tip.Y - box.Bottom) < 1;
                if (!onTop && !onBottom || tip.X < box.X || tip.X > box.Right)
                {
                    found.Add($"step {i + 1}: the pointer at {tip} is not on the card's top or bottom edge, {box}");
                }
                else if (onTop ? ring.Bottom > box.Y + 0.5 : ring.Y < box.Bottom - 0.5)
                {
                    found.Add($"step {i + 1}: the card at {box} points at its outline {ring} from over it");
                }

                await shell.Demo.Next();
            }
        }
        finally
        {
            shell.Demo.End(keep: false);
            window.Close();
        }

        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
        Assert.True(pointed >= PhoneDemo.Steps.Count / 2, $"only {pointed} of {PhoneDemo.Steps.Count} cards had a pointer");
    }
}
