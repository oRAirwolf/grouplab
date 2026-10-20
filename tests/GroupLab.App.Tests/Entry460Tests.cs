using Avalonia.Headless.XUnit;
using GroupLab.App;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 460 section 2 (question 98, Alan's option B): several pictures of the same single-page design dropped
/// together, their codes read and no set sheet number on any, ask once, "Pool these 3 pictures as one group?", with Alan's words; pooled,
/// they are one tab pooled as pictures with no codes are, each read and its holes found; kept, a tab each, as before.
/// </summary>
public class Entry460Tests
{
    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static TargetDefinition TwoMoaPage() =>
        GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF9-LTR.gltd.json")).Definition!;

    [Fact]
    public void CopiesOfOneSinglePageDesignAreSortedTogetherAndASetIsNot()
    {
        var page = TwoMoaPage();
        var set = Entry423Tests.TwoMoaSet();
        Assert.True(CopiesQuestion.SinglePage(page));
        Assert.False(CopiesQuestion.SinglePage(set));
        var sorting = DropSorting.Of([
            new DroppedPicture("a.png", "a", page, "page", null), new DroppedPicture("b.png", "b", page, "page", null),
            new DroppedPicture("c.png", "c", set, "set", 0), new DroppedPicture("d.png", "d", set, "set", 1),
            new DroppedPicture("e.png", "e", page, "page", null), new DroppedPicture("f.png", "f", null, null, null),
        ]);
        var copies = Assert.Single(sorting.Copies);
        Assert.Equal(["a.png", "b.png", "e.png"], copies.Select(p => p.Path));
        Assert.Equal(["c.png", "d.png"], Assert.Single(sorting.Sets).Select(p => p.Path));
        Assert.Empty(sorting.Alone);
        Assert.Equal("f.png", Assert.Single(sorting.Unnamed).Path);

        // One copy alone is a picture alone, as before.
        var one = DropSorting.Of([new DroppedPicture("a.png", "a", page, "page", null), new DroppedPicture("f.png", "f", null, null, null)]);
        Assert.Empty(one.Copies);
        Assert.Equal("a.png", Assert.Single(one.Alone).Path);
    }

    [Fact]
    public void TheQuestionIsAlansWords()
    {
        Assert.Equal("Pool these 3 pictures as one group?", CopiesQuestion.Title(3));
        Assert.Equal("They are three copies of the same page, GroupLab 3x3 2 MOA, Letter. Pool them if they are one group shot across the pages. "
            + "Keep a tab each if they are separate loads or separate strings.", CopiesQuestion.Body(3, TwoMoaPage().Name));
        Assert.StartsWith("They are 13 copies", CopiesQuestion.Body(13, "a page"), StringComparison.Ordinal);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThreeCopiesDroppedTogetherAskOnceAndFollowTheAnswer(bool pool)
    {
        var page = TwoMoaPage();
        var (window, folder) = Entry423Tests.Window(detect: pool);
        try
        {
            window.OpenDropped([.. Enumerable.Range(0, 3).Select(k => Entry423Tests.Sheet(page, folder, 0, $"copy-{k}.png", seed: k))]);
            await Entry423Tests.Settle(window, () => window.ProblemOpen);
            Assert.Equal(CopiesQuestion.Title(3), window.ProblemTitle);
            Assert.Equal([CopiesQuestion.Pool, CopiesQuestion.Keep], window.ProblemChoices);
            window.PressProblemChoice(pool ? CopiesQuestion.Pool : CopiesQuestion.Keep);
            await Entry423Tests.Settle(window);

            if (pool)
            {
                Assert.Single(window.Targets);
                var group = Assert.IsType<SetGroup>(window.CurrentSet);
                Assert.Null(group.Definition);
                Assert.Equal(page.Name, group.CopiesOf?.Name);
                Assert.Equal(3, group.Members.Count);
                Assert.Equal("GroupLab 3x3 2 MOA, Letter, 3 copies pooled", window.CurrentTarget.Name);
                // Their codes read, so each was read and its holes found: none waits for a scale.
                Assert.DoesNotContain("no scale yet", window.StatusText, StringComparison.Ordinal);
                Assert.Contains("3 pictures pooled.", window.StatusText, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(3, window.Targets.Count);
                Assert.All(window.Targets, t => Assert.Null(t.Set));
                Assert.Contains("a tab each", window.StatusText, StringComparison.Ordinal);
            }
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}
