using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 item 2A on the phone: the result screen's plot keeps its own height and grows by its key, which sits in a
/// compact box directly under the plot instead of folding into a Key button over it; two columns where the screen is wide enough.
/// </summary>
public class Entry465PhoneTests
{
    private static readonly TestPhone ThePhone = new();

    [AvaloniaFact]
    public void TheKeySitsUnderThePlotAtEveryWidth()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        string copy = Path.Combine(((TestPhone)Phone.Platform!).CacheFolder, "chosen-465.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        foreach (int width in new[] { 320, 412, 900 })
        {
            var view = new ResultView(result, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => { });
            var window = new Window { Width = width, Height = 900, Content = view };
            window.Show();
            try
            {
                Dispatcher.UIThread.RunJobs();
                Dispatcher.UIThread.RunJobs();
                var plot = view.Plot;
                var (data, key, collapsed) = plot.KeyLayout();
                Assert.False(collapsed, $"{width}: the key folded into its button");
                Assert.Equal(360, data.Height, 1);
                Assert.True(key.Top >= data.Bottom, $"{width}: the key {key} is not under the plot {data}");
                Assert.True(new Rect(plot.Bounds.Size).Contains(key), $"{width}: the key {key} runs out of the plot's control {plot.Bounds}");
                Assert.Equal(key, plot.KeyDrawn);
                Assert.Equal(width >= 900 ? 2 : 1, plot.KeyColumns);
            }
            finally
            {
                window.Close();
            }
        }
    }

    /// <summary>Item 7B on the phone: two loads keep their pictures and say what the difference means under them; three are as before.</summary>
    [AvaloniaFact]
    public void TwoLoadsOnComparesSayWhatTheDifferenceMeans()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        string copy = Path.Combine(((TestPhone)Phone.Platform!).CacheFolder, "chosen-465-compare.png");
        File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
        var result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        Assert.Null(result.Failure);
        var store = PhoneAnalysis.Store();
        var saved = store.Get(result.SessionId!.Value)!;
        var records = new[] { "H4350 41.5", "H4350 42.0", "H4350 42.5" }
            .Select((load, i) => store.Get(store.Save(saved with { Id = 0, CreatedUtc = $"2026-10-21T0{i}:00:00Z", Load = load }))!).ToList();
        foreach (int loads in new[] { 2, 3 })
        {
            var window = new Window { Width = 412, Height = 915 };
            window.Show();
            try
            {
                var page = new ComparePage([.. records.Take(loads)], UnitSettings.Imperial, () => { });
                window.Content = page;
                Dispatcher.UIThread.RunJobs();
                var card = page.Groups!;
                Assert.All(card.Plots, p => Assert.Equal(150, p.Width));
                if (loads == 3)
                {
                    Assert.Null(card.Meaning);
                    continue;
                }

                Assert.NotNull(card.Meaning);
                Assert.False(card.MeaningBeside);
                var said = card.Meaning!.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
                Assert.Contains(LoadGroups.MeaningHeading, said);
                Assert.Contains("H4350 41.5", said);
                Assert.Contains("H4350 42.0", said);
                Assert.Contains("Mean radius", said);
                Assert.Contains(said, t => t.StartsWith("To tell a 10 percent difference in spread apart", StringComparison.Ordinal));
            }
            finally
            {
                window.Close();
            }
        }
    }
}
