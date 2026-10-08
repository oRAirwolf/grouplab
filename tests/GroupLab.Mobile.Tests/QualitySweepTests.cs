using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 388 section 2: every screenshot a scenario takes carries the layout's faults beside it, so the emulator's
/// sweep at each width and theme is a quality sweep. Each of the four faults is found on a page made to have it, and a page without them
/// says nothing.
/// </summary>
public class QualitySweepTests
{
    private static (Window Window, Shell Shell) Open(double width)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = width, Height = 800, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, shell);
    }

    private static List<(string Kind, string? Text)> Found(Shell shell)
    {
        shell.UpdateLayout();
        var said = Scenario.Quality(TopLevel.GetTopLevel(shell)!, "test");
        return [.. said["findings"]!.AsArray().Select(f => (f!["kind"]!.GetValue<string>(), f["text"]?.GetValue<string>()))];
    }

    [AvaloniaFact]
    public void EachFaultIsFoundOnAPageMadeToHaveIt()
    {
        var (window, shell) = Open(360);
        try
        {
            var both = new Grid();
            both.Children.Add(new TextBlock { Text = "first words" });
            both.Children.Add(new TextBlock { Text = "second words" });
            shell.ShowInPage(new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new Button { Content = "x", Width = 30, Height = 30 },
                    new TextBlock { Text = "a sentence far too long for its box", Width = 60, TextTrimming = TextTrimming.CharacterEllipsis },
                    new Border { Width = 600, HorizontalAlignment = HorizontalAlignment.Left, Child = new TextBlock { Text = "reaches past the side" } },
                    both,
                },
            });
            Dispatcher.UIThread.RunJobs();

            var found = Found(shell);
            Assert.Contains(("small", "x"), found);
            Assert.Contains(found, f => f.Kind == "cut" && f.Text == "a sentence far too long for its box");
            Assert.Contains(("off", "reaches past the side"), found);
            Assert.Contains(found, f => f.Kind == "overlap" && f.Text is "first words" or "second words");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void APageWithoutThemSaysNothing()
    {
        var (window, shell) = Open(360);
        try
        {
            shell.ShowInPage(new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new Button { Content = "Big enough", MinWidth = 48, MinHeight = 48 },
                    new TextBlock { Text = "A sentence that wraps onto as many lines as it needs, and is never cut short.", TextWrapping = TextWrapping.Wrap },
                },
            });
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(Found(shell));
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The sweep's screens at the small phone's width, as the emulator's quality sweep runs them: nothing under a finger's size, nothing cut
    /// short, nothing past the side and no text over text. The first emulator sweep found the Shots page's switch 32 units tall, fixed here.
    /// </summary>
    [AvaloniaFact]
    public async Task TheSweepsScreensAtASmallPhonesWidthHaveNoFaults()
    {
        var (window, _) = Open(360);
        window.Height = 640;
        Directory.CreateDirectory(Scenario.Folder);
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), Path.Combine(Scenario.Folder, "sample.png"), overwrite: true);
        try
        {
            await Scenario.Run(File.ReadAllText(Repo.PathTo("scripts", "scenarios", "phone-sweep.json")));
            var faults = Directory.EnumerateFiles(Scenario.Results, "*.quality.json")
                .SelectMany(f => System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(f))!["findings"]!.AsArray()
                    .Select(x => $"{Path.GetFileName(f)}: {x!["kind"]} {x["type"]} {x["id"] ?? x["text"]} {x["width"]} x {x["height"]} {x["detail"]}"))
                .ToList();
            Assert.True(Directory.EnumerateFiles(Scenario.Results, "*.quality.json").Count() >= 11, "fewer quality files than the sweep takes screenshots");
            Assert.True(faults.Count == 0, string.Join(Environment.NewLine, faults));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(Path.Combine(Scenario.Folder, "sample.png"));
        }
    }
}
