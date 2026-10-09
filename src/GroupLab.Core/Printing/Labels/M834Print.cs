using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Printing.Labels;

/// <summary>Where one page of a direct print to the M834 has got to, for the screen and the log.</summary>
/// <param name="Said">After <see cref="M834Stage.Printed"/>: whether the printer said the page had printed before the limit ran out.</param>
public sealed record M834Progress(M834Stage Stage, int Page, int Pages, int Blocks = 0, int Bytes = 0, bool Said = false, int LimitSeconds = 0);

/// <summary>The steps of one page: being sent, sent, being printed, printed or given up on.</summary>
public enum M834Stage
{
    Sending,
    Sent,
    Printing,
    Printed,
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 389 section 1: the Phomemo M834's direct print, in one place for the phone and the computer, so the bytes
/// the computer sends down a paired Bluetooth serial port are the phone's bytes for the same page. Each page is drawn for the M834's 300 dpi
/// head at true size and encoded in its LZO1X blocks (<see cref="PhomemoLzoEncoder"/>), with the roll's 15.5 mm feed after it
/// (<see cref="TearBar"/>, entry 382); it is sent in the profile's chunks and pacing (<see cref="PrinterJob"/>), and the link is held open
/// until the printer answers <c>1A 0F 0C</c> (<see cref="PrinterFinish"/>, entry 381).
/// <para>
/// Entry 391: the M834 feeds short, 99.2 to 99.5 percent along the feed by four ways of measuring over two prints, so every page is drawn
/// <see cref="FeedStretch"/> longer down the page (<see cref="PrintHead.FeedStretch"/>). Across is the head's own dot pitch and is not
/// touched. A saved printer check is still not applied to the dots: it corrects photographs of the sheet, chosen in Settings under Printers.
/// </para>
/// </summary>
public static class M834Print
{
    public const string ProfileId = "phomemo-m834";

    /// <summary>What the M834 calls itself over Bluetooth, which is how it is found among paired devices.</summary>
    public const string NameHint = "M834";

    public static PrinterProfile Profile => PrinterProfiles.All.Single(p => p.Id == ProfileId);

    /// <summary>
    /// Entry 391: how long the M834 prints a length along the feed, as a fraction of what GroupLab draws: 99.30 percent, the mean of five
    /// readings over request 81's two prints (calipers 99.19 and 99.34, rulers 99.19 twice, the second print's scan 99.49).
    /// </summary>
    public const double MeasuredFeed = 0.9930;

    /// <summary>Rows drawn for each row of the page, so the printed page comes out true along the feed: 1 / <see cref="MeasuredFeed"/>.</summary>
    public const double FeedStretch = 1.0 / MeasuredFeed;

    /// <summary>The head a page for the M834 is drawn on: the profile's, stretched along the feed.</summary>
    public static PrintHead Head => Profile.Head with { FeedStretch = FeedStretch };

    /// <summary>One page of the sheet as the M834 takes it.</summary>
    public static byte[] Encode(Scene page, PaperForm paper)
    {
        ArgumentNullException.ThrowIfNull(page);
        var profile = Profile;
        var dots = ThermalRaster.Render(page, Head);
        var job = new LabelJob(dots.Image, page.Width / (10.0 * Scene.UnitsPerDmm), page.Height / (10.0 * Scene.UnitsPerDmm), FeedAfterMm: TearBar.FeedAfterMm(paper));
        return PrinterEncoders.For(profile).Encode(job, profile);
    }

    /// <summary>
    /// Sends every page in turn and waits after each for the printer to say it has printed; true when it said so for every page.
    /// <paramref name="progress"/> hears each step. Cancelling throws.
    /// </summary>
    public static async Task<bool> SendAsync(IPrinterLink link, IReadOnlyList<byte[]> pages, Action<M834Progress> progress,
        Func<TimeSpan, CancellationToken, Task> pause, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(progress);
        var profile = Profile;
        bool finished = true;
        for (int k = 0; k < pages.Count; k++)
        {
            progress(new M834Progress(M834Stage.Sending, k + 1, pages.Count));
            int blocks = await PrinterJob.SendAsync(link, profile, pages[k], pause, token).ConfigureAwait(false);
            progress(new M834Progress(M834Stage.Sent, k + 1, pages.Count, blocks, pages[k].Length));

            // Entry 381: the page is in the platform's buffer, not the printer; the link stays open until the printer says it has printed.
            progress(new M834Progress(M834Stage.Printing, k + 1, pages.Count));
            var limit = PrinterFinish.Limit(pages[k].Length);
            bool said = await PrinterFinish.WaitAsync(link, PrinterFinish.M834Printed, limit, pause, _ => { }, token).ConfigureAwait(false);
            finished &= said;
            progress(new M834Progress(M834Stage.Printed, k + 1, pages.Count, blocks, pages[k].Length, said, (int)limit.TotalSeconds));
        }

        return finished;
    }

    /// <summary>What the screen says while a page is on its way.</summary>
    public static string Saying(M834Progress at)
    {
        ArgumentNullException.ThrowIfNull(at);
        return at.Stage is M834Stage.Sending or M834Stage.Sent
            ? (at.Pages == 1 ? "Sending the page to the M834…" : $"Sending page {at.Page} of {at.Pages} to the M834…")
            : (at.Pages == 1 ? "The M834 is printing the page… (Cancel stops it)" : $"The M834 is printing page {at.Page} of {at.Pages}… (Cancel stops it)");
    }

    /// <summary>What the screen says when the print has ended; <paramref name="diagnostics"/> is where this platform sends its log from.</summary>
    public static string Done(int pages, bool finished, string diagnostics)
    {
        string measure = pages == 1 ? "Measure its ruler line: it should be true to size." : "Measure a ruler line: it should be true to size.";
        return finished
            ? (pages == 1 ? "The M834 printed the page. " : $"The M834 printed {pages} pages. ") + measure
            : $"The page went to the M834, but the printer never said it had finished. If the print stopped short, {diagnostics}. " + measure;
    }
}
