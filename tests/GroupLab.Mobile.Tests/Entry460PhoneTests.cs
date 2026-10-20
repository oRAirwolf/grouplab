using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 460 section 2 on the phone: pictures picked together that are all copies of one single-page design are asked
/// about once, in the computer's words; "Pool as one group" opens them as one group, "Keep a tab each" shows them as before.
/// </summary>
public class Entry460PhoneTests
{
    private static TargetDefinition Load(string file) => GltdJsonReader.ReadFile(Repo.PathTo("targets", file)).Definition!;

    private static PhoneResult Read(TargetDefinition? definition, int? sheet) =>
        new(MarkingState.Empty with { SetSheet = sheet }, definition, null, null);

    [Fact]
    public void CopiesOfOneSinglePageDesignAreCopiesAndASetIsNot()
    {
        var set = Load("GL-CF9-T.gltd.json");
        var page = Load("GL-CF9-LTR.gltd.json");

        Assert.Same(page, CapturePage.Copies([Read(page, null), Read(page, null), Read(page, null)]));
        Assert.Null(CapturePage.Copies([Read(page, null)]));
        Assert.Null(CapturePage.Copies([Read(page, null), Read(null, null)]));
        Assert.Null(CapturePage.Copies([Read(set, 0), Read(set, 1)]));
        Assert.Null(CapturePage.Copies([Read(page, null), Read(set, 0)]));
    }

    [AvaloniaTheory]
    [InlineData("copies-pool", true)]
    [InlineData("copies-keep", false)]
    public async Task TheQuestionIsTheComputersAndFollowsTheAnswer(string id, bool pooled)
    {
        var host = new ContentControl { Content = "before" };
        var window = new Window { Width = 402, Height = 874, Content = host };
        window.Show();
        try
        {
            var asked = CapturePage.PoolCopies(host, 3, "GroupLab 3x3 2 MOA, Letter");
            Dispatcher.UIThread.RunJobs();
            var words = host.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains(CopiesQuestion.Title(3), words);
            Assert.Contains(CopiesQuestion.Body(3, "GroupLab 3x3 2 MOA, Letter"), words);
            host.GetLogicalDescendants().OfType<Button>().Single(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b) == id)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(pooled, await asked);
            Assert.Equal("before", host.Content);
        }
        finally
        {
            window.Close();
        }
    }
}
