using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 268, for a head that has no native one (entry 290 section 2 item 6, iOS): a pure black screen for a device
/// left on for testing. An OLED screen switches its black pixels off, so nothing can burn in. It behaves as Android's idle activity does: a tap
/// shows, for a few seconds, one dim line saying what it is and a Close button; Close hands the screen back.
/// </summary>
internal sealed class IdleScreen : UserControl
{
    private readonly StackPanel words;

    public IdleScreen(Action close)
    {
        ArgumentNullException.ThrowIfNull(close);
        var closeButton = new Button { Content = "Close", MinHeight = Screens.Touch, MinWidth = 96, HorizontalAlignment = HorizontalAlignment.Center };
        closeButton.Click += (_, _) => close();
        words = new StackPanel
        {
            Spacing = 12,
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = "GroupLab idle screen, used for testing", Foreground = new SolidColorBrush(Color.FromRgb(90, 90, 90)), TextWrapping = TextWrapping.Wrap },
                closeButton,
            },
        };
        Background = Brushes.Black;
        Content = new Grid { Background = Brushes.Black, Children = { words } };
        Tapped += (_, _) => ShowWords();
    }

    /// <summary>Whether the line and Close are showing.</summary>
    internal bool Speaking => words.IsVisible;

    private void ShowWords()
    {
        words.IsVisible = true;
        DispatcherTimer.RunOnce(() => words.IsVisible = false, TimeSpan.FromSeconds(4));
    }
}
