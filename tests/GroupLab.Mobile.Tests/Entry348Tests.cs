using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Core.Imaging;
using GroupLab.Core.StoreTargets;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 348, concept A on a phone: Targets, "Add a store-bought target", opens five full-screen steps with a
/// five-part progress bar, Back at the top and one big button at the bottom. A stand-in poster's photo is straightened by its printed size,
/// its bulls are confirmed, and "Save and share the file" hands the reference file, never the photo, to the share sheet. At 320 wide with
/// large text and in both themes nothing runs off the screen and every control has a name; no field sits under the keyboard.
/// </summary>
[Collection("StoreTargetLibrary")]
public class Entry348Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Wait(FingerprintPage page)
    {
        Assert.True(page.Work.Wait(TimeSpan.FromSeconds(120)), "the step's work did not finish");
        Settle();
    }

    private static TestPhone Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        return (TestPhone)Phone.Platform!;
    }

    private static string? IdOf(Control control) => AutomationProperties.GetAutomationId(control);

    private static void Press(Control within, string id)
    {
        within.GetLogicalDescendants().OfType<Button>().First(b => IdOf(b) == id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    private static IEnumerable<string> Words(Control page) => page.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>A page on its way to <paramref name="step"/> with the stand-in poster, its printed size typed and its bulls as found.</summary>
    internal static FingerprintPage At(FingerprintStep step, string photo, Action? leave = null)
    {
        var page = new FingerprintPage(leave ?? (() => { }));
        page.UsePhoto(photo);
        Wait(page);
        while (page.Session.Step < step)
        {
            if (page.Session.Step == FingerprintStep.Scale)
            {
                page.Field("fingerprint-width").Text = "12";
                page.Field("fingerprint-height").Text = "18";
            }

            page.GoOn();
            Wait(page);
        }

        return page;
    }

    [AvaloniaFact]
    public void TargetsOpensTheStepsAndTheFileGoesToTheShareSheet()
    {
        var phone = Started();
        string folder = Temp.Folder("fingerprint-phone");
        Directory.CreateDirectory(folder);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        try
        {
            string photo = Path.Combine(folder, "poster.jpg");
            StandInPoster.Photograph(photo);
            Settle();
            shell.Show(Shell.Place.Targets);
            Settle();
            Press(shell, "targets-add-store");
            var page = shell.GetLogicalDescendants().OfType<FingerprintPage>().Single();
            Assert.Contains(FingerprintWords.Of(FingerprintStep.Photo), Words(page));
            Assert.Contains(page.GetLogicalDescendants().OfType<Button>(), b => IdOf(b) == "fingerprint-camera");
            Assert.Contains(page.GetLogicalDescendants().OfType<Button>(), b => IdOf(b) == "fingerprint-choose");

            page.UsePhoto(photo);
            Wait(page);
            Assert.True(page.Session.CornersFound);
            Press(page, "fingerprint-next");
            Wait(page);
            Assert.Equal(FingerprintStep.Scale, page.Session.Step);
            Assert.Contains(FingerprintWords.Of(FingerprintStep.Scale), Words(page));
            foreach (var source in new[] { ScaleSource.PrintedSize, ScaleSource.GroupLabSheet, ScaleSource.TwoPoints })
            {
                Assert.Contains(FingerprintWords.Choice(source), Words(page));
            }

            // Entry 348 section 2's correction, on the phone too: no "Most accurate", and what entry 344 measured for the source picked.
            Assert.DoesNotContain(Words(page), w => w.Contains("Most accurate", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(FingerprintWords.Measured(ScaleSource.PrintedSize), Words(page));

            page.Field("fingerprint-width").Text = "12";
            page.Field("fingerprint-height").Text = "18";
            Press(page, "fingerprint-next");
            Wait(page);
            Press(page, "fingerprint-next");
            Wait(page);
            Assert.Equal(FingerprintStep.Bulls, page.Session.Step);
            Assert.Contains(page.Session.Bulls, b => Math.Abs(b.X - StandInPoster.Bulls[0].X) < 0.3 && Math.Abs(b.Y - StandInPoster.Bulls[0].Y) < 0.3);
            Assert.Contains(page.GetLogicalDescendants().OfType<Button>(), b => IdOf(b) == "fingerprint-add-bull");
            Assert.Contains(page.GetLogicalDescendants().OfType<Button>(), b => b.Content is TextBlock { Text: "These are right" });

            // A tap on a ring removes it, and a tap where it was puts it back.
            int found = page.Session.Bulls.Count;
            var first = page.Session.Bulls[0];
            page.Touched(first);
            Settle();
            Assert.Equal(found - 1, page.Session.Bulls.Count);
            page.Touched(first);
            Settle();
            Assert.Equal(found, page.Session.Bulls.Count);

            Press(page, "fingerprint-next");
            Wait(page);
            Assert.Equal(FingerprintStep.Send, page.Session.Step);
            Assert.Contains(FingerprintWords.Never, Words(page));
            Assert.Contains(page.GetLogicalDescendants().OfType<Button>(), b => b.Content is TextBlock { Text: "Save and share the file" });
            page.Field("fingerprint-name").Text = "Stand-in poster";
            phone.Asked.Clear();
            Press(page, "fingerprint-next");
            Assert.Contains(("share", "stand-in-poster.glref"), phone.Asked);
            string file = Path.Combine(phone.CacheFolder, "shared", "stand-in-poster.glref");
            var written = TargetReference.Read(File.ReadAllText(file));
            Assert.Equal("12 by 18 in", written.Target.Size);
            Assert.Equal(found, written.Fingerprint.Bulls.Count);
            Assert.True(new FileInfo(file).Length < new FileInfo(photo).Length / 4, "the file is as large as the photo");
            Temp.DeleteFile(file);

            // Back from each step, then from the first back to Targets.
            for (int i = 0; i < 5; i++)
            {
                Press(page, "fingerprint-back");
            }

            Assert.Empty(shell.GetLogicalDescendants().OfType<FingerprintPage>());
        }
        finally
        {
            window.Close();
            Temp.Delete(folder);
        }
    }

    /// <summary>Every step at 320 wide with large text, in both themes: nothing past the edge, nothing unnamed, no word broken.</summary>
    [AvaloniaFact]
    public void EveryStepFitsTheNarrowestPhoneWithLargeTextInBothThemes()
    {
        Started();
        string folder = Temp.Folder("fingerprint-narrow");
        Directory.CreateDirectory(folder);
        try
        {
            string photo = Path.Combine(folder, "poster.jpg");
            StandInPoster.Photograph(photo);
            foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                var window = new Window { Width = 320, Height = 640, FontSize = 22, RequestedThemeVariant = theme };
                window.Show();
                try
                {
                    var page = new FingerprintPage(() => { });
                    window.Content = page;
                    page.UsePhoto(photo);
                    Wait(page);
                    for (int step = 0; step < 5; step++)
                    {
                        Settle();
                        string where = $"{theme} step {step + 1}";
                        foreach (var control in page.GetVisualDescendants().OfType<Control>().Where(c => c is Button or TextBox or RadioButton && c.IsEffectivelyVisible))
                        {
                            string named = AutomationProperties.GetName(control) is { Length: > 0 } n ? n
                                : control is ContentControl { Content: TextBlock { Text: { } t } } ? t : control is ContentControl { Content: string s } ? s : "";
                            Assert.False(string.IsNullOrWhiteSpace(named), $"{where}: a {control.GetType().Name} has no name");
                            var right = control.TranslatePoint(new Point(control.Bounds.Width, 0), window);
                            Assert.True(right is { } p && p.X <= 320.5, $"{where}: {named} runs past the right edge to {right?.X:0}");
                        }

                        foreach (var text in page.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text)))
                        {
                            string words = text.Text!;
                            foreach (var line in text.TextLayout.TextLines.Skip(1))
                            {
                                int start = line.FirstTextSourceIndex;
                                Assert.True(start > 0 && start <= words.Length && (char.IsWhiteSpace(words[start - 1]) || words[start - 1] is '-' or '/' or ','), $"{where}: \"{words}\" is broken inside a word");
                            }
                        }

                        if (step == 1)
                        {
                            page.Field("fingerprint-width").Text = "12";
                            page.Field("fingerprint-height").Text = "18";
                        }

                        if (step < 4)
                        {
                            page.GoOn();
                            Wait(page);
                        }
                    }

                    Assert.Equal(FingerprintStep.Send, page.Session.Step);
                }
                finally
                {
                    window.Close();
                }
            }
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    /// <summary>The keyboard rule (entry 328): the printed size's fields and the name stay above the keyboard on the smallest phone.</summary>
    [AvaloniaFact]
    public void NoFieldOfTheStepsSitsUnderTheKeyboard()
    {
        Started();
        string folder = Temp.Folder("fingerprint-keyboard");
        Directory.CreateDirectory(folder);
        var shell = new Shell();
        var window = new Window { Width = 320, Height = 568, Content = shell };
        window.Show();
        try
        {
            string photo = Path.Combine(folder, "poster.jpg");
            StandInPoster.Photograph(photo);
            Settle();
            int fields = 0;
            foreach (var step in new[] { FingerprintStep.Scale, FingerprintStep.Send })
            {
                shell.ShowInPage(At(step, photo));
                Settle();
                var boxes = shell.GetVisualDescendants().OfType<TextBox>().Where(t => t.IsEffectivelyVisible && t.IsEffectivelyEnabled).ToList();
                foreach (var box in boxes)
                {
                    Entry328Tests.ClearOfTheKeyboard(shell, window, box, 253, $"the fingerprint's {step}, field {boxes.IndexOf(box) + 1} of {boxes.Count}");
                    shell.Keyboard.CloseKeyboard();
                    Settle();
                    fields++;
                }
            }

            Assert.True(fields >= 3, $"only {fields} fields were found");
        }
        finally
        {
            window.Close();
            Temp.Delete(folder);
        }
    }
}
