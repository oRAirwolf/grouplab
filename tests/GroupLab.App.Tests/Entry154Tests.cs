using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 154: a word a shooter may not know explains itself wherever it is a label or a heading, from the one list the
/// website reads, and from the keyboard as well as the pointer.
/// </summary>
public class Entry154Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static List<string> Unexplained(Window window)
    {
        var failures = new List<string>();
        foreach (var block in window.GetLogicalDescendants().OfType<TextBlock>())
        {
            if (!new[] { AppStyles.Label, AppStyles.Section, AppStyles.Dim }.Any(block.Classes.Contains) || Glossary.Find(block.Text ?? "") is not { } term)
            {
                continue;
            }

            if (!block.Classes.Contains(TermHelp.Class))
            {
                failures.Add($"\"{block.Text}\" names {term.Term} with nothing explaining it");
            }
            else if (ToolTip.GetTip(block) as string != term.Plain)
            {
                failures.Add($"\"{block.Text}\" explains {term.Term} in words that are not the glossary's");
            }
            else if (!block.Focusable)
            {
                failures.Add($"\"{block.Text}\" cannot be reached from the keyboard");
            }
        }

        return failures;
    }

    /// <summary>Sections 3 and 5.1 and 5.3: on the analysis, its Advanced section, the marking screen and the settings.</summary>
    [AvaloniaFact]
    public void EveryLabelThatNamesATermExplainsItWithTheGlossarysWords()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetShotDistance(3600);
            Settle();
            var failures = Unexplained(window);
            window.CalibreAnswered();
            window.Analyse();
            window.AdvancedPanel.IsExpanded = true;
            Settle();
            failures.AddRange(Unexplained(window));
            window.ShowSettings();
            Settle();
            failures.AddRange(Unexplained(window));
            Assert.True(failures.Count == 0, string.Join("\n", failures.Distinct()));

            // Something was explained, and on the analysis the kept figures are among them.
            window.ShowSettings(false);
            Settle();
            var explained = window.GetLogicalDescendants().OfType<TextBlock>().Where(b => b.Classes.Contains(TermHelp.Class)).Select(b => b.Tag as string).ToHashSet();
            foreach (string term in new[] { "mean-radius", "extreme-spread", "cep", "moa", "mil", "sigma" })
            {
                Assert.Contains(term, explained);
            }
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Section 3.2: the explanation is reached by keyboard: focus shows the plain sentence, Enter opens the whole entry.</summary>
    [AvaloniaFact]
    public void TheKeyboardReachesAnExplanation()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            Settle();
            var label = window.GetLogicalDescendants().OfType<TextBlock>().First(b => b.Classes.Contains(TermHelp.Class) && (b.Tag as string) == "mean-radius" && Entry109Tests.Shown(b));
            Assert.True(label.Focus(NavigationMethod.Tab));
            Settle();
            Assert.True(ToolTip.GetIsOpen(label));

            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Settle();
            var flyout = Assert.IsType<Flyout>(FlyoutBase.GetAttachedFlyout(label));
            Assert.True(flyout.IsOpen);
            var entry = Assert.IsType<StackPanel>(flyout.Content);
            Assert.Contains(entry.Children.OfType<TextBlock>(), t => t.Text == Glossary.ByTerm("mean-radius")!.Plain);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
