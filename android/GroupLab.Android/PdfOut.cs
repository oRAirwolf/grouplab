using Android.App;
using Android.Content;
using Android.OS;
using Android.Print;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Gltd.Model;

using GroupLab.Mobile;

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
        if (paper switch { PageSize.Letter => PrintAttributes.MediaSize.NaLetter, PageSize.A4 => PrintAttributes.MediaSize.IsoA4, PageSize.Tabloid => PrintAttributes.MediaSize.NaTabloid, PageSize.A3 => PrintAttributes.MediaSize.IsoA3, PageSize.A6 => PrintAttributes.MediaSize.IsoA6, PageSize.Label4x6 => PrintAttributes.MediaSize.NaIndex4x6, PageSize.Label100x150 => new PrintAttributes.MediaSize("GROUPLAB_100X150", "100 x 150 mm", 3937, 5906), _ => null } is { } size)
        {
            attributes.SetMediaSize(size);
        }

        printer.Print(name, new Adapter(pdf, name), attributes.Build());
        DiagnosticLog.Info("print.dialog", ("paper", paper.ToString()), ("bytes", pdf.Length));
        return null;
    }

    /// <summary>
    /// Entry 376 item B6: each page drawn by Android's own PDF renderer, on white, <paramref name="width"/> pixels wide, as PNG; none where it
    /// cannot (the caller says so).
    /// </summary>
    public static IReadOnlyList<byte[]> Pages(byte[] pdf, int width)
    {
        string path = Path.Combine(Application.Context.CacheDir!.AbsolutePath, "report-view.pdf");
        var pages = new List<byte[]>();
        try
        {
            File.WriteAllBytes(path, pdf);
            using var file = ParcelFileDescriptor.Open(new Java.IO.File(path), ParcelFileMode.ReadOnly)!;
            using var renderer = new global::Android.Graphics.Pdf.PdfRenderer(file);
            for (int i = 0; i < renderer.PageCount; i++)
            {
                using var page = renderer.OpenPage(i)!;
                int height = Math.Max(1, (int)Math.Round(width * (double)page.Height / page.Width));
                using var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(width, height, global::Android.Graphics.Bitmap.Config.Argb8888!)!;
                bitmap.EraseColor(unchecked((int)0xFFFFFFFF));
                page.Render(bitmap, null, null, global::Android.Graphics.Pdf.PdfRenderMode.ForDisplay);
                using var png = new MemoryStream();
                bitmap.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png!, 100, png);
                pages.Add(png.ToArray());
            }
        }
        catch (Exception e) when (e is Java.Lang.Exception or IOException)
        {
            DiagnosticLog.Info("report.view", ("error", e.GetType().Name));
            return [];
        }
        finally
        {
            File.Delete(path);
        }

        DiagnosticLog.Info("report.view", ("pages", pages.Count));
        return pages;
    }

    /// <summary>Entry 376 item B6: the PDF saved in Downloads, in a GroupLab folder, where the files app finds it; a sentence either way.</summary>
    public static string Save(byte[] pdf, string name)
    {
        try
        {
            string file = string.Concat(name.Split(Path.GetInvalidFileNameChars())) + ".pdf";
            var values = new ContentValues();
            values.Put(global::Android.Provider.MediaStore.IMediaColumns.DisplayName, file);
            values.Put(global::Android.Provider.MediaStore.IMediaColumns.MimeType, "application/pdf");
            values.Put(global::Android.Provider.MediaStore.IMediaColumns.RelativePath, global::Android.OS.Environment.DirectoryDownloads + "/GroupLab");
            var resolver = Application.Context.ContentResolver!;
            var uri = resolver.Insert(global::Android.Provider.MediaStore.Downloads.ExternalContentUri!, values)
                ?? throw new IOException("Downloads would not take a new file");
            using (var output = resolver.OpenOutputStream(uri) ?? throw new IOException("the new file could not be opened"))
            {
                output.Write(pdf, 0, pdf.Length);
            }

            DiagnosticLog.Info("report.saved", ("bytes", pdf.Length));
            return $"Saved as {file} in Downloads, in the GroupLab folder.";
        }
        catch (Exception e) when (e is Java.Lang.Exception or IOException)
        {
            DiagnosticLog.Info("report.saved", ("error", e.GetType().Name));
            return "The PDF could not be saved: " + e.Message + " Use Share and choose a place to keep it instead.";
        }
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
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(activity, AndroidPhone.Authority, new Java.IO.File(path));
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
