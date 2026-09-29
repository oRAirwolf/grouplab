using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Foundation;
using GroupLab.Core.Rendering;
using GroupLab.Mobile;
using UIKit;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 6: what the simulator can prove of the system's own sheets and the pasteboard. A GroupLab
/// PDF handed to Share opens iOS's share sheet, and handed to Print opens its print sheet, each closing again with GroupLab still there;
/// and a picture on the pasteboard, pressed in with Paste a picture on the Capture screen, is read into analysis. Choosing a printer, and
/// what comes out of one, are for a device.
/// </summary>
internal static class SheetsSelfTest
{
    /// <summary>The sample sheet rendered as the Targets screen renders one to print or share.</summary>
    private static (byte[] Pdf, string Name, GroupLab.Core.Gltd.Model.PageSize Paper)? Rendered()
    {
        string path = Path.Combine(Phone.Platform.FilesFolder, "targets", SelfTestChecks.SheetFile);
        if (!File.Exists(path))
        {
            return null;
        }

        var definition = GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(path).Definition;
        if (definition is null)
        {
            return null;
        }

        var rendered = TargetRenderer.Render(definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote));
        return rendered.Pdf is { } pdf ? (pdf, definition.Name, definition.Page.Size) : null;
    }

    /// <summary>
    /// Share or Print with a GroupLab PDF: the sheet iOS presents is photographed and closed, and GroupLab must still be there.
    /// </summary>
    internal static async Task<SelfTestCheck> Sheet(bool print, string picture)
    {
        var check = new SelfTestCheck(print ? "print sheet" : "share sheet");
        try
        {
            var sheet = await Task.Run(Rendered);
            if (sheet is not { } made)
            {
                check.Detail = "the sample sheet could not be rendered as a PDF";
                return check;
            }

            check.Numbers["pdfKb"] = made.Pdf.Length / 1024;
            if (print && !await SelfTest.OnUi(() => UIPrintInteractionController.PrintingAvailable))
            {
                check.Skipped = true;
                check.Detail = "this simulator says printing is not available, so the print sheet is for the TestFlight sitting";
                return check;
            }

            var before = await SelfTest.OnUi(IosPhone.Top);
            string? said = await SelfTest.OnUi(() => print
                ? Phone.Platform.PrintPdf(made.Pdf, made.Name, made.Paper)
                : Phone.Platform.SharePdf(made.Pdf, made.Name));
            bool shown = said is null && await SelfTest.WaitFor(() => IosPhone.Top() is { } top && !ReferenceEquals(top, before), TimeSpan.FromSeconds(20));
            string kind = await SelfTest.OnUi(() => IosPhone.Top()?.GetType().Name ?? "nothing");
            check.Numbers["shown"] = shown ? 1 : 0;
            bool taken = shown && await SelfTest.Photographed(picture);
            await SelfTest.OnUi(() =>
            {
                if (print)
                {
                    UIPrintInteractionController.SharedPrintController.Dismiss(false);
                }
                else if (IosPhone.Top() is UIActivityViewController share)
                {
                    share.DismissViewController(false, null);
                }
            });
            bool closed = await SelfTest.WaitFor(() => ReferenceEquals(IosPhone.Top(), before), TimeSpan.FromSeconds(15));
            bool alive = await SelfTest.OnUi(() => Shell.Current is not null);
            check.Passed = shown && closed && alive;
            check.Detail = said is not null
                ? "GroupLab said: " + said
                : $"a {made.Pdf.Length / 1024} KB GroupLab PDF {(shown ? "opened " + kind : "opened nothing")}, {(taken ? "photographed" : "not photographed")}; "
                    + $"it {(closed ? "closed" : "did not close")}, and GroupLab {(alive ? "is still there" : "is gone")}";
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            check.Detail = SelfTestChecks.Describe(e);
        }

        return check;
    }

    /// <summary>
    /// Paste a picture: the sample put on the pasteboard as a PNG, as another app copies one, then the Capture screen's own button pressed;
    /// passed where it is read into analysis and saved with the sample's shots.
    /// </summary>
    internal static async Task<SelfTestCheck> Paste(string sample, int n)
    {
        var check = new SelfTestCheck("paste a picture");
        if (!File.Exists(sample))
        {
            check.Skipped = true;
            check.Detail = "the sample scan was not put in the application's files";
            return check;
        }

        return await PhotosSelfTest.Analyzed(check, n, () => SelfTest.OnUi<string?>(() =>
        {
            using var data = NSData.FromFile(sample);
            UIPasteboard.General.SetData(data, "public.png");
            var paste = Shell.Current!.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Content is TextBlock { Text: "Paste a picture" });
            if (paste is null)
            {
                return "the Paste a picture button was not found on the Capture screen";
            }

            paste.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            return null;
        }));
    }
}
