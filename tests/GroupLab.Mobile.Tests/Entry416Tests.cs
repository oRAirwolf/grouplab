using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 416 section 3, board "A on the phone": the velocity card has a Conditions row with its one-line summary; tapping
/// it opens the sheet, whose Use these puts the values on the session, changes the card at once and saves them with the session, and whose
/// Remember carries them to the next sheet of the same day. Fill from Ballistics is greyed with its reason on the phone.
/// </summary>
public class Entry416Tests
{
    private static readonly TestPhone ThePhone = new();

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static string? Id(Control control) => Avalonia.Automation.AutomationProperties.GetAutomationId(control);

    private static void Press(Control within, string id)
    {
        within.GetLogicalDescendants().OfType<Button>().First(b => Id(b) == id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    [AvaloniaFact]
    public void TheConditionsSheetChangesTheCardAndIsKept()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var window = new Window { Width = 320, Height = 900, FontSize = 16 };
        window.Show();
        string copy = Path.Combine(ThePhone.CacheFolder, "chosen-416.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var store = PhoneAnalysis.Store();
        var before = store.LoadBook();
        var memory = Phone.Settings.LoadConditionsMemory();
        try
        {
            Phone.Settings.SaveConditionsMemory(true, null, null);
            store.SaveBook(RecordBook.Empty.With(new Load("Phone load", null) { BallisticCoefficient = 0.243, DragModel = DragModel.G7, BulletWeightGrains = 175 }));
            var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
            Assert.Null(result.Failure);
            var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
            window.Content = view;
            Settle();
            var figures = view.GetLogicalDescendants().OfType<FiguresView>().Single();
            Press(figures, "velocity-type");
            var page = (Control)view.Content!;
            int shots = result.State.Shots.Count(s => s.IsShot && s.Exclusion is null);
            page.GetLogicalDescendants().OfType<TextBox>().Single(b => Id(b) == "chrono-readings").Text =
                string.Join(", ", Enumerable.Range(0, shots).Select(i => 2700 + ((i * 7) % 23) - 11));
            Press(page, "chrono-read");
            Press(page, "chrono-keep-paired");
            var standard = figures.Velocity!;
            Assert.True(standard.HasResult, standard.Sentence);

            // The row says a standard day and opens the sheet.
            Assert.Contains(figures.GetLogicalDescendants().OfType<Button>(), b => Id(b) == "velocity-conditions");
            Press(figures, "velocity-conditions");
            var open = Assert.IsType<FiguresView.ConditionsSheet>(figures.ConditionsOpen);
            Assert.False(window.GetLogicalDescendants().OfType<Button>().Single(b => Id(b) == "conditions-fill").IsEnabled);
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == FiguresView.ConditionsSheet.FillReason);

            // Use these: on the session, the card changed, saved with the session, and remembered for today.
            open.Temperature.Text = "90";
            open.Where.Text = "6000";
            Press(window, "conditions-use");
            Assert.Equal(new SessionConditions(90, 6000), figures.ConditionsUsed is { } used ? new SessionConditions(used.Air.TemperatureF, used.Air.AltitudeFt) : null);
            Assert.NotEqual(standard.PredictedSdInches!.Value, figures.Velocity!.PredictedSdInches!.Value, 6);
            Assert.Equal("this sheet, and the rest a standard day", figures.ConditionsUsed!.From);
            var saved = store.Get(view.SessionId!.Value)!;
            Assert.Equal(new SessionConditions(90, 6000), MarkingFile.Read(saved.MarkingJson).State.Conditions);
            Assert.Equal(new SessionConditions(90, 6000, FromLastSheet: true), Phone.Settings.ConditionsForNewSheet(DateOnly.FromDateTime(DateTime.Now)));

            // Section 3.3: at Android's largest text size, at the narrowest width, every field and button of the sheet is inside the window.
            GroupLab.App.Theme.TextSize.Follow(AndroidText.Largest);
            Settle();
            figures.OpenConditions();
            Settle();
            Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Settle();
            foreach (var control in window.GetLogicalDescendants().OfType<Control>().Where(c => c is TextBox or Button or CheckBox && Id(c)?.StartsWith("conditions-", StringComparison.Ordinal) == true))
            {
                var at = control.TranslatePoint(new Avalonia.Point(control.Bounds.Width, 0), window);
                Assert.True(at is { } p && p.X <= window.Width + 0.5, $"{Id(control)} reaches {at?.X} in a window {window.Width} wide");
            }
        }
        finally
        {
            GroupLab.App.Theme.TextSize.Follow(size => size);
            Phone.Settings.SaveConditionsMemory(memory.Remember, memory.Day, memory.Conditions);
            store.SaveBook(before);
            window.Close();
        }
    }
}
