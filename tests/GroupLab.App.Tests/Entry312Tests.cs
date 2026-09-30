using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 312 section 5 on the desktop: the caliber box selects all of its text when it takes the focus, so typing
/// replaces it, and a clear button inside it empties it.
/// </summary>
public class Entry312Tests
{
    [AvaloniaFact]
    public void TheCaliberBoxSelectsAllOnFocusAndHasAClearButton()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var box = window.CalibreBox;
            box.Text = "6.5 Creedmoor, 0.264 in";
            Dispatcher.UIThread.RunJobs();
            box.Focus();
            Dispatcher.UIThread.RunJobs();
            var field = box.GetVisualDescendants().OfType<TextBox>().First();
            Assert.Equal(0, Math.Min(field.SelectionStart, field.SelectionEnd));
            Assert.Equal(box.Text.Length, Math.Max(field.SelectionStart, field.SelectionEnd));

            var clear = Assert.IsType<Button>(box.InnerRightContent);
            Assert.True(clear.IsVisible);
            clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("", box.Text);
            Assert.False(clear.IsVisible);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
