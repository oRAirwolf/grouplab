using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.StoreTargets;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 340 section 2 on the phone: "Which target is this?", asked only when the picture is of a store-bought target
/// sold at more than one size with the same printing and cannot tell which. Each member is a row with GroupLab's drawing of its outline,
/// its name and its printed size, the last answer for the family first; "Not sure" places the bull and leaves the scale to be measured.
/// The answer chosen is kept for next time. The desktop asks the same question in a small window.
/// </summary>
internal sealed class WhichTargetPage : UserControl
{
    /// <param name="answered">The member chosen, or null for "Not sure".</param>
    public WhichTargetPage(StoreTargetRecognition seen, Action<StoreTargetMatch?> answered)
    {
        ArgumentNullException.ThrowIfNull(seen);
        ArgumentNullException.ThrowIfNull(answered);
        string? family = FamilyQuestion.Family(seen);
        Answers = FamilyQuestion.Answers(seen, family is null ? null : Phone.Settings.LoadFamilyAnswer(family));
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title(FamilyQuestion.Title));
        column.Children.Add(Screens.Line(FamilyQuestion.Why));
        foreach (var answer in Answers)
        {
            var words = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center, Children = { Screens.Line(answer.Name), Screens.Quiet(answer.Printed) } };
            var outline = new TargetOutline(answer) { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            var content = new DockPanel();
            DockPanel.SetDock(outline, Dock.Left);
            content.Children.Add(outline);
            content.Children.Add(words);
            var button = new Button { Content = content, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, MinHeight = Screens.Touch }
                .Id("which-target-" + answer.Match.Target.Id);
            button.Classes.Add(PhoneStyles.Row);
            button.Click += (_, _) =>
            {
                if (family is not null)
                {
                    Phone.Settings.SaveFamilyAnswer(family, answer.Match.Target.Id);
                }

                DiagnosticLog.Info("storetarget.answer", ("answer", answer.Match.Target.Id));
                answered(answer.Match);
            };
            column.Children.Add(button);
        }

        column.Children.Add(Screens.Choice(FamilyQuestion.NotSure, () =>
        {
            DiagnosticLog.Info("storetarget.answer", ("answer", "not sure"));
            answered(null);
        }).Id("which-target-not-sure"));
        Content = Screens.Page(column);
    }

    /// <summary>The answers in the order shown, for a test.</summary>
    internal IReadOnlyList<FamilyAnswer> Answers { get; }
}
