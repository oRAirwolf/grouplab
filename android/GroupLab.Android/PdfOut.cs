using Android.Content;
using Android.OS;
using Android.Print;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.4: a sheet's PDF sent to Android's own print dialog, where the person picks the printer and
/// sees the pages first, or offered to the share sheet, to print from another application or send to a computer. Nothing prints without
/// the dialog, and the PDF is the desktop's, byte for byte.
/// </summary>
internal static class PdfOut
{
    private static string Shared => Path.Combine(global::Android.App.Application.Context.CacheDir!.AbsolutePath, "shared");

    /// <summary>Opens the print dialog for the PDF, sized to the sheet's paper where Android knows it. Returns why not, or null.</summary>
    public static string? Print(byte[] pdf, string name, PageSize paper)
    {
        if (MainActivity.Current is not { } activity || activity.GetSystemService(Context.PrintService) is not PrintManager printer)
        {
            return "This phone offers no printing. Share the PDF instead, and print it from a computer.";
        }

        var attributes = new PrintAttributes.Builder();
        if (paper switch { PageSize.Letter => PrintAttributes.MediaSize.NaLetter, PageSize.A4 => PrintAttributes.MediaSize.IsoA4, PageSize.Tabloid => PrintAttributes.MediaSize.NaTabloid, PageSize.A3 => PrintAttributes.MediaSize.IsoA3, _ => null } is { } size)
        {
            attributes.SetMediaSize(size);
        }

        printer.Print(name, new Adapter(pdf, name), attributes.Build());
        DiagnosticLog.Info("print.dialog", ("paper", paper.ToString()), ("bytes", pdf.Length));
        return null;
    }

    /// <summary>Writes the PDF where the share sheet may read it and opens the sheet. Returns why not, or null.</summary>
    public static string? Share(byte[] pdf, string name)
    {
        if (MainActivity.Current is not { } activity)
        {
            return "The share sheet could not be opened.";
        }

        // Only the newest shared file is kept, as for a session file.
        if (Directory.Exists(Shared))
        {
            Directory.Delete(Shared, recursive: true);
        }

        Directory.CreateDirectory(Shared);
        string path = Path.Combine(Shared, string.Concat(name.Split(Path.GetInvalidFileNameChars())) + ".pdf");
        File.WriteAllBytes(path, pdf);
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(activity, SessionFiles.Authority, new Java.IO.File(path));
        var send = new Intent(Intent.ActionSend);
        send.SetType("application/pdf");
        send.PutExtra(Intent.ExtraStream, uri);
        send.AddFlags(ActivityFlags.GrantReadUriPermission);
        activity.StartActivity(Intent.CreateChooser(send, "Share the PDF"));
        DiagnosticLog.Info("print.share", ("bytes", pdf.Length));
        return null;
    }

    /// <summary>The PDF as it is: one layout, every page written as rendered.</summary>
    private sealed class Adapter(byte[] pdf, string name) : PrintDocumentAdapter
    {
        public override void OnLayout(PrintAttributes? oldAttributes, PrintAttributes? newAttributes, CancellationSignal? cancellationSignal, LayoutResultCallback? callback, Bundle? extras)
        {
            if (cancellationSignal?.IsCanceled == true)
            {
                callback?.OnLayoutCancelled();
                return;
            }

            callback?.OnLayoutFinished(new PrintDocumentInfo.Builder(name).SetContentType(PrintContentType.Document).Build(), true);
        }

        public override void OnWrite(PageRange[]? pages, ParcelFileDescriptor? destination, CancellationSignal? cancellationSignal, WriteResultCallback? callback)
        {
            try
            {
                using var output = new Java.IO.FileOutputStream(destination!.FileDescriptor);
                output.Write(pdf);
                callback?.OnWriteFinished([PageRange.AllPages!]);
            }
            catch (Java.IO.IOException e)
            {
                DiagnosticLog.Warn("print.write", ("error", e.GetType().Name));
                callback?.OnWriteFailed(e.Message);
            }
        }
    }
}
