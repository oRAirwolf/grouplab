using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App.Theme;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 411 section 2, Alan: "For android, follow the system". The phone's text follows the system's text size: the
/// body, a heading, a field and a button all grow at Android's largest size, a larger size grows less than a smaller one as Android's own
/// conversion has it, and the sizes go back when the setting does. The screens at that size are held by <see cref="QualitySweepTests"/>
/// and <see cref="MobileSweepTests"/>.
/// </summary>
public class Entry411Tests
{
    private static (Window Window, TextBlock Body, TextBlock Heading, TextBox Field, Button Button) Shown()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        var body = new TextBlock { Text = "body" };
        var heading = new TextBlock { Text = "heading", Classes = { PhoneStyles.Title } };
        var field = new TextBox { Text = "12" };
        var button = new Button { Content = "Press" };
        var window = new Window { Width = 360, Height = 640, Content = new Shell() };
        window.Show();
        ((Shell)window.Content).ShowInPage(new StackPanel { Children = { body, heading, field, button } });
        Dispatcher.UIThread.RunJobs();
        return (window, body, heading, field, button);
    }

    [AvaloniaFact]
    public void TheTextGrowsWithTheSystemsTextSizeAndComesBack()
    {
        var (window, body, heading, field, button) = Shown();
        try
        {
            double[] plain = [body.FontSize, heading.FontSize, field.FontSize, button.FontSize];
            Assert.Equal(new[] { Tokens.TitleSize, Tokens.LeadValueSize, Tokens.TitleSize, Tokens.TitleSize }, plain);

            TextSize.Follow(AndroidText.Largest);
            Dispatcher.UIThread.RunJobs();
            double[] largest = [body.FontSize, heading.FontSize, field.FontSize, button.FontSize];
            Assert.Equal(AndroidText.Largest(Tokens.TitleSize), body.FontSize, 2);
            Assert.All(plain.Zip(largest), p => Assert.True(p.Second > p.First * 1.2, $"{p.First} grew only to {p.Second}"));

            // Android's conversion: the heading grows by less than the body does.
            Assert.True(heading.FontSize / Tokens.LeadValueSize < body.FontSize / Tokens.TitleSize);

            TextSize.Follow(size => size);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(plain, new[] { body.FontSize, heading.FontSize, field.FontSize, button.FontSize });
        }
        finally
        {
            TextSize.Follow(size => size);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void WithNothingFollowedASizeIsItself()
    {
        Assert.Equal(Tokens.TitleSize, TextSize.Of(Tokens.TitleSize));
        Assert.Equal(Tokens.LeadValueSize, TextSize.Of(Tokens.LeadValueSize));
    }
}
