using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using GroupLab.Mobile;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 313 section 1.5: on the iPad mini reading stayed on "Reading the sheet's codes" and Cancel did nothing. Here
/// a large picture, the committed 600 dpi scan of 32 megapixels, is shared into Capture, Cancel is pressed while its codes are being read,
/// twice, and Capture's start must show again within a second with the picture kept. Cancel is pressed once more after that, when the
/// page it was on has gone, which threw on build 134.
/// </summary>
internal static class SelfTestCancel
{
    /// <summary>The most Cancel may take to bring the start back, in milliseconds.</summary>
    internal const int MostMilliseconds = 1000;

    internal static async Task<SelfTestCheck> Run(string sample, int n)
    {
        var check = new SelfTestCheck("cancel while reading");
        if (!File.Exists(sample))
        {
            check.Skipped = true;
            check.Detail = "the sample scan was not put in the application's files";
            return check;
        }

        try
        {
            await SelfTest.OnUi(() => Shell.Current!.Show(Shell.Place.Capture));
            var handle = new PhotoHandle(null, null, null, new FileInfo(sample).Length, Path.GetExtension(sample).ToLowerInvariant(),
                () => Task.FromResult<Stream?>(File.OpenRead(sample)));
            await SelfTest.OnUi(() => CapturePage.SharedPicture?.Invoke([handle]));

            // Watched closely, on the interface thread, until the line says the codes are being read.
            string? codes = GroupLab.Core.Trace.StageWords.During("S0.identify");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            bool onCodes = false;
            while (watch.Elapsed < TimeSpan.FromSeconds(120) && !onCodes)
            {
                onCodes = await SelfTest.OnUi(() => Shell.Current?.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == codes) == true);
                if (!onCodes)
                {
                    await Task.Delay(10);
                }
            }

            check.Numbers["reachedCodes"] = onCodes ? 1 : 0;
            check.Numbers["msToCodes"] = watch.ElapsedMilliseconds;
            if (!onCodes)
            {
                check.Detail = "the reading never said it was reading the codes";
                return check;
            }

            var pressed = System.Diagnostics.Stopwatch.StartNew();
            var button = await SelfTest.OnUi(() =>
            {
                var cancel = Shell.Current!.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Content is TextBlock { Text: "Cancel" });
                cancel?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                cancel?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return cancel;
            });
            bool back = false;
            while (pressed.ElapsedMilliseconds < 10_000 && !back)
            {
                back = await SelfTest.OnUi(() => SelfTest.Find<CapturePage>() is { AtStart: true });
                if (!back)
                {
                    await Task.Delay(10);
                }
            }

            long took = pressed.ElapsedMilliseconds;
            bool kept = await SelfTest.OnUi(() => SelfTest.Find<CapturePage>()?.KeepingPicture == true);
            string? thrown = null;
            await SelfTest.OnUi(() =>
            {
                try
                {
                    button?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException)
                {
                    thrown = e.GetType().Name;
                }
            });
            check.Numbers["backMs"] = took;
            check.Numbers["kept"] = kept ? 1 : 0;
            check.Passed = button is not null && back && took <= MostMilliseconds && kept && thrown is null;
            check.Detail = button is null ? "no Cancel on the reading's page"
                : $"Cancel pressed while the codes were read, {watch.ElapsedMilliseconds} ms after the picture was shared; "
                  + (back ? $"the start showed again after {took} ms" : "the start never showed again")
                  + (kept ? ", with the picture kept" : ", and the picture was not kept")
                  + (thrown is null ? "" : $"; pressed again it threw {thrown}");
            if (back)
            {
                await SelfTest.Photographed($"{n:00}-cancel-kept");
            }

            // The kept picture forgotten, so the checks after this one start from a clean start.
            await SelfTest.OnUi(() => Shell.Current!.GetVisualDescendants().OfType<Button>()
                .FirstOrDefault(b => b.IsEffectivelyVisible && (b.Content as string ?? (b.Content as TextBlock)?.Text) == "Forget it")?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = SelfTestChecks.Describe(e);
        }

        return check;
    }
}
