using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 3.3: the shot distance's unit is a dropdown beside the number, yards or metres.
/// <para>
/// It was a label showing whatever the Settings said, so somebody who thinks in metres but shoots at a hundred yard range had to change a
/// global setting to type one number, or convert it in their head at the bench. The unit chosen here decides how the number in the box is
/// read and written, and the distance itself is kept in inches whichever is chosen, so nothing stored moves with it.
/// </para>
/// </summary>
public class Entry131Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void TheShotDistanceUnitIsAChoiceAndNotALabel()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();

            var choice = window.GetLogicalDescendants().OfType<ComboBox>().Single(c => c.Name == "ShotDistanceUnit");

            Assert.Equal(["yd", "m"], choice.ItemsSource!.Cast<string>());
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    /// <summary>
    /// Typing a number with metres chosen stores the metres, and the marking keeps inches. A hundred metres is 3937 in, not the 3600 in a
    /// hundred yards would be, and reading it back in yards says 109.4.
    /// </summary>
    [AvaloniaFact]
    public void TheNumberIsReadInTheUnitBesideIt()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();
            var choice = window.GetLogicalDescendants().OfType<ComboBox>().Single(c => c.Name == "ShotDistanceUnit");
            var box = window.GetLogicalDescendants().OfType<TextBox>().First(t => t.Width == 90);

            choice.SelectedIndex = (int)DistanceUnit.Metre;
            box.Text = "100";
            Settle();
            window.SetShotDistanceFromBox();
            Settle();

            Assert.NotNull(window.Session.State.ShotDistanceInches);
            Assert.Equal(100 / 0.0254, window.Session.State.ShotDistanceInches!.Value, 3);

            // And choosing yards says the same distance in yards rather than reading the number again as yards.
            choice.SelectedIndex = (int)DistanceUnit.Yard;
            Settle();
            Assert.Equal(100 / 0.0254, window.Session.State.ShotDistanceInches!.Value, 3);
            Assert.Equal("109.361", box.Text);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 131 section 6.3: Accept is held on a sheet of bulls until the calibre question is answered. It is the one
    /// question whose answer changes what GroupLab finds rather than how it shows it, and leaving it blank cost five holes on one of the range
    /// scans and the shot at the edge of the scan on another, with nothing on the screen ever saying so.
    /// </summary>
    [AvaloniaFact]
    public void AcceptIsHeldUntilTheCalibreIsAnswered()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Settle();

            window.Analyse();
            Settle();

            Assert.False(window.Analysing, "Accept went ahead with the calibre unanswered");
            Assert.Contains("Say what you were shooting", window.ProblemText, StringComparison.Ordinal);

            // Answering it, either way, lets it through. Clearing the box is an answer: a photograph of something that is not a GroupLab
            // sheet has no calibre, and the gate is there to stop Accept on a sheet nobody was asked about.
            window.CalibreAnswered();
            window.Analyse();
            Settle();

            Assert.True(window.Analysing);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 131 section 6.2: the zero block can show where the group landed against where it was aimed.
    /// <para>
    /// The numbers above it already say the correction. What this adds is the comparison a number cannot make: how far the centre sits from
    /// the aim against how well that centre is known. So the test asks for both halves of that, and for the honest ending where the
    /// uncertainty covers the aim.
    /// </para>
    /// </summary>
    [AvaloniaFact]
    public void TheZeroBlockShowsWhereTheGroupLanded()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetShotDistance(3600);
            Settle();

            string? says = window.ZeroPictureSays;
            Assert.NotNull(says);
            Assert.Contains("The group's centre is", says, StringComparison.Ordinal);
            Assert.Matches(@"0\.\d+ in (right|left)", says!);
            Assert.Matches(@"0\.\d+ in (low|high)", says!);

            // This sheet's group sits close to its aim, so the honest ending is the one that says so rather than an instruction to dial.
            Assert.Contains("cannot be told from chance", says!, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

}
