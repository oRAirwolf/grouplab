using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.StoreTargets;
using GroupLab.Tests.Support;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 348, concept A on the computer: Targets, "Add a store-bought target", opens five guided steps on the same
/// screen. A photograph of a stand-in poster has its corners found, the scale comes from the source picked with what entry 344 measured for it
/// and never "Most accurate", the straightened target's bulls can be removed and added, and the file saved holds the fingerprint, name, size
/// and bulls, never the photograph. In both themes, at the smallest window and with large text, every control is inside the window and named.
/// </summary>
[Collection("StoreTargetLibrary")]
public class Entry348Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Wait(FingerprintStepsView steps)
    {
        Assert.True(steps.Work.Wait(TimeSpan.FromSeconds(120)), "the step's work did not finish");
        Settle();
    }

    private static double Apart(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    [AvaloniaFact]
    public void TheFiveStepsMakeAReferenceFileFromAPhoto()
    {
        var (window, path, _) = Entry109Tests.Sheet(1280, 720);
        string store = window.SettingsStore.Path;
        string folder = Temp.Folder("fingerprint-steps");
        Directory.CreateDirectory(folder);
        try
        {
            string photo = Path.Combine(folder, "poster.jpg");
            var truth = StandInPoster.Photograph(photo);
            var steps = window.AddStoreTarget();
            Settle();
            Assert.True(window.ShowingLibrary);
            Assert.Equal(FingerprintStep.Photo, steps.Session.Step);
            Assert.Contains(FingerprintWords.Question(FingerprintStep.Photo), steps.Words);

            // Next before a photo says what it needs, and stays.
            steps.GoOn();
            Settle();
            Assert.Contains(FingerprintWords.NeedPhoto, steps.Words);

            steps.UsePhoto(photo);
            Settle();
            Assert.True(steps.Session.CornersFound);
            for (int i = 0; i < 4; i++)
            {
                Assert.True(Apart(steps.Session.Corners[i], truth[i]) < 20, $"corner {i} is {Apart(steps.Session.Corners[i], truth[i]):0} pixels off");
            }

            steps.GoOn();
            Wait(steps);
            Assert.Equal(FingerprintStep.Scale, steps.Session.Step);
            foreach (var source in new[] { ScaleSource.PrintedSize, ScaleSource.GroupLabSheet, ScaleSource.TwoPoints })
            {
                Assert.Contains(FingerprintWords.Choice(source), steps.Words);
            }

            // Entry 348 section 2's correction: no choice is called the most accurate; the line under it says what entry 344 measured.
            Assert.DoesNotContain(steps.Words, w => w.Contains("Most accurate", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(FingerprintWords.Measured(ScaleSource.PrintedSize), steps.Words);
            steps.ChooseSource(ScaleSource.TwoPoints);
            Settle();
            Assert.Contains(FingerprintWords.Measured(ScaleSource.TwoPoints), steps.Words);
            Assert.DoesNotContain(FingerprintWords.Measured(ScaleSource.PrintedSize), steps.Words);
            steps.ChooseSource(ScaleSource.PrintedSize);
            Settle();

            steps.GoOn();
            Settle();
            Assert.Contains(FingerprintWords.NeedSize, steps.Words);
            steps.FieldNamed("Width in inches").Text = "12";
            steps.FieldNamed("Height in inches").Text = "18";
            steps.GoOn();
            Wait(steps);
            Assert.True(steps.Session.Step == FingerprintStep.Straighten, string.Join(" | ", steps.Words));
            Assert.True(steps.Picture.CornersMove);

            steps.GoOn();
            Wait(steps);
            Assert.Equal(FingerprintStep.Bulls, steps.Session.Step);
            Assert.Equal(12, steps.Session.Target!.WidthInches, 1);
            Assert.Equal(18, steps.Session.Target.HeightInches, 1);
            Assert.Contains(steps.Session.Bulls, b => Apart(b, StandInPoster.Bulls[0]) < 0.3);
            Assert.Equal(FingerprintWords.Next(FingerprintStep.Bulls, phone: false), steps.NextButton.Content);

            // A click on a ring removes it; a click away from the rings adds one; Remove beside a row removes that one.
            int found = steps.Session.Bulls.Count;
            var first = steps.Session.Bulls[0];
            steps.Touch(first);
            Settle();
            Assert.Equal(found - 1, steps.Session.Bulls.Count);
            Assert.DoesNotContain(steps.Session.Bulls, b => Apart(b, first) < 0.01);
            steps.Touch(first);
            Settle();
            Assert.Equal(found, steps.Session.Bulls.Count);
            var remove = steps.GetLogicalDescendants().OfType<Button>().First(b => AutomationProperties.GetName(b) == "Remove bull 1");
            var named = steps.Session.Bulls[0];
            remove.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.Equal(found - 1, steps.Session.Bulls.Count);
            Assert.DoesNotContain(steps.Session.Bulls, b => Apart(b, named) < 0.01);
            steps.Touch(named);
            Settle();
            Assert.Equal(found, steps.Session.Bulls.Count);

            steps.GoOn();
            Wait(steps);
            Assert.Equal(FingerprintStep.Send, steps.Session.Step);
            Assert.Null(steps.Session.FamilySaid);
            Assert.Contains(FingerprintWords.Never, steps.Words);
            Assert.Equal(FingerprintWords.Next(FingerprintStep.Send, phone: false), steps.NextButton.Content);
            Assert.Equal("Save reference file", steps.NextButton.Content);
            steps.FieldNamed(FingerprintWords.NameBox).Text = "Stand-in poster 12 by 18 in";
            string file = Path.Combine(folder, FingerprintWords.FileName("Stand-in poster 12 by 18 in") + ".glref");
            Assert.True(steps.SaveTo(file));
            Settle();
            var written = TargetReference.Read(File.ReadAllText(file));
            Assert.Equal("Stand-in poster 12 by 18 in", written.Target.Name);
            Assert.Equal("12 by 18 in", written.Target.Size);
            Assert.Equal(ScaleSource.PrintedSize, written.Source);
            Assert.Equal(found, written.Fingerprint.Bulls.Count);
            Assert.True(new FileInfo(file).Length < 200 * 1024, "the file is larger than a fingerprint");
            Assert.True(new FileInfo(file).Length < new FileInfo(photo).Length / 4, "the file is as large as the photograph");
            Assert.Contains(steps.Words, w => w.StartsWith("Saved stand-in-poster-12-by-18-in.glref", StringComparison.Ordinal));

            // Back to Targets puts the list back.
            for (int i = 0; i < 5; i++)
            {
                steps.GoBack();
                Settle();
            }

            Assert.Null(window.FingerprintSteps);
            window.Close();
        }
        finally
        {
            Temp.Delete(folder);
            Temp.DeleteFile(store);
            Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Two points a known distance apart set the size, the corners taking out the angle.</summary>
    [AvaloniaFact]
    public void TwoPointsAndADistanceSetTheScale()
    {
        var (window, path, _) = Entry109Tests.Sheet(1280, 720);
        string store = window.SettingsStore.Path;
        string folder = Temp.Folder("fingerprint-points");
        Directory.CreateDirectory(folder);
        try
        {
            string photo = Path.Combine(folder, "poster.jpg");
            var truth = StandInPoster.Photograph(photo);
            var steps = window.AddStoreTarget();
            steps.UsePhoto(photo);
            steps.GoOn();
            Wait(steps);
            steps.ChooseSource(ScaleSource.TwoPoints);
            Settle();
            steps.Touch(truth[0]);
            steps.Touch(truth[1]);
            Settle();
            Assert.Equal(2, steps.Session.Placed);
            Assert.Contains(FingerprintWords.PointsSaid(2, touch: false), steps.Words);
            steps.FieldNamed("Distance between the two ends, inches").Text = "12";
            steps.GoOn();
            Wait(steps);
            Assert.True(steps.Session.Step == FingerprintStep.Straighten, string.Join(" | ", steps.Words));
            steps.GoOn();
            Wait(steps);
            Assert.True(steps.Session.Step == FingerprintStep.Bulls, string.Join(" | ", steps.Words));
            Assert.Equal(ScaleSource.TwoPoints, steps.Session.Target!.Source);
            Assert.InRange(steps.Session.Target.WidthInches, 11.5, 12.5);
            Assert.InRange(steps.Session.Target.HeightInches, 17, 19);
            window.Close();
        }
        finally
        {
            Temp.Delete(folder);
            Temp.DeleteFile(store);
            Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Every step in both themes, at the smallest window and with large text: inside the window, every control named.</summary>
    [AvaloniaFact]
    public void EveryStepFitsAndIsNamedInBothThemesWithLargeText()
    {
        var (window, path, _) = Entry109Tests.Sheet(1060, 720);
        string store = window.SettingsStore.Path;
        string folder = Temp.Folder("fingerprint-fit");
        Directory.CreateDirectory(folder);
        try
        {
            string photo = Path.Combine(folder, "poster.jpg");
            StandInPoster.Photograph(photo);
            window.FontSize = 18;
            foreach (var theme in new[] { ThemeChoice.Dark, ThemeChoice.Light })
            {
                window.SetTheme(theme);
                var steps = window.AddStoreTarget();
                steps.UsePhoto(photo);
                Settle();
                for (int step = 0; step < 5; step++)
                {
                    Settle();
                    foreach (var control in steps.GetVisualDescendants().OfType<Control>().Where(c => c is Button or TextBox or RadioButton && c.IsEffectivelyVisible))
                    {
                        string named = AutomationProperties.GetName(control) is { Length: > 0 } n ? n : control is ContentControl { Content: string s } ? s : "";
                        Assert.False(string.IsNullOrWhiteSpace(named), $"step {step + 1}: a {control.GetType().Name} has no name");
                        var at = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), window);
                        Assert.True(at is { } p && p.X <= window.Width + 0.5, $"step {step + 1}: {named} runs past the window's right edge");
                    }

                    if (step == 1)
                    {
                        steps.FieldNamed("Width in inches").Text = "12";
                        steps.FieldNamed("Height in inches").Text = "18";
                    }

                    if (step < 4)
                    {
                        steps.GoOn();
                        Wait(steps);
                    }
                }

                Assert.True(steps.Session.Step == FingerprintStep.Send, string.Join(" | ", steps.Words));
                for (int i = 0; i < 5; i++)
                {
                    steps.GoBack();
                }

                Settle();
            }

            window.Close();
        }
        finally
        {
            Temp.Delete(folder);
            Temp.DeleteFile(store);
            Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
