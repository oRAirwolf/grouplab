using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.Mobile;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 section 2.3: pictures that arrive from another app, read straight into analysis as if chosen on the
/// Capture screen. Two ways in: "Open in GroupLab" (the document types in Info.plist), where iOS copies the picture into GroupLab's
/// Documents/Inbox and hands over its address; and the share extension, which leaves pictures in the app group and opens
/// <c>grouplab://shared</c> (<see cref="Handoff"/>). Several shared at once are a set, one picture a sheet. Pictures left waiting, because
/// the extension could not open GroupLab, are read the next time GroupLab starts or comes back to the front. Nothing about where a picture
/// was taken is read.
/// </summary>
internal static class IncomingPhotos
{
    /// <summary>The folder pictures are moved into from the app group and the Inbox before they are read; the cache, which iOS may empty.</summary>
    private static string Claimed => Directory.CreateDirectory(Path.Combine(IosPhone.Caches, "incoming")).FullName;

    /// <summary>Listens to the application's activations: an address it was opened with, and every return to the front.</summary>
    internal static void Listen(Avalonia.Application application)
    {
        if (application.TryGetFeature(typeof(IActivatableLifetime)) is not IActivatableLifetime lifetime)
        {
            DiagnosticLog.Info("ios.incoming", ("listening", false));
            return;
        }

        lifetime.Activated += (_, e) => Activated(e);

        // A share whose extension could not open GroupLab waits until GroupLab is next opened.
        Dispatcher.UIThread.Post(() => Hand(Waiting(), "waiting"));
    }

    /// <summary>What GroupLab was activated for: pictures opened in it, the share extension's address, or a return to the front.</summary>
    internal static void Activated(ActivatedEventArgs e)
    {
        switch (e)
        {
            case FileActivatedEventArgs files:
                // Entry 307: a GroupLab data file goes to Settings to be imported; everything else is a picture.
                var data = files.Files.Where(IsDataFile).ToList();
                if (data.Count > 0)
                {
                    DataFile(data[0]);
                }

                Hand(Opened(files.Files.Except(data)), "open in");
                break;
            case ProtocolActivatedEventArgs address when IsHandoff(address.Uri):
                Hand(Waiting(), "share");
                break;
            case { Kind: ActivationKind.Background }:
                Hand(Waiting(), "waiting");
                break;
        }
    }

    /// <summary>Whether a file opened in GroupLab is a GroupLab data file, by its ending (entry 307).</summary>
    internal static bool IsDataFile(IStorageItem item) =>
        (item.TryGetLocalPath() ?? item.Name).EndsWith(GroupLab.Core.Records.DataExport.Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Entry 307, as Android's MainActivity does: a data file opened in GroupLab is moved out of the Inbox into the cache and kept only when
    /// it begins as a GroupLab data file does; then Settings opens with what importing it would do, and nothing is written until Import is
    /// pressed.
    /// </summary>
    internal static void DataFile(IStorageItem item)
    {
        if (item.TryGetLocalPath() is not { } path || !File.Exists(path))
        {
            return;
        }

        string copy = Path.Combine(IosPhone.Caches, "incoming" + GroupLab.Core.Records.DataExport.Extension);
        try
        {
            File.Copy(path, copy, overwrite: true);
            File.Delete(path);
            string start;
            using (var head = new StreamReader(copy))
            {
                var chars = new char[64];
                start = new string(chars, 0, head.Read(chars, 0, chars.Length));
            }

            if (!start.Replace(" ", "", StringComparison.Ordinal).Contains("\"format\":\"grouplab-data\"", StringComparison.Ordinal))
            {
                File.Delete(copy);
                DiagnosticLog.Info("data.incoming", ("grouplab", false));
                return;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "data.incoming", e);
            return;
        }

        DiagnosticLog.Info("data.incoming", ("grouplab", true));
        Dispatcher.UIThread.Post(() => DispatcherTimer.RunOnce(() =>
        {
            DataSection.Waiting = copy;
            Shell.Current?.Show(Shell.Place.Settings);
        }, TimeSpan.FromSeconds(1)));
    }

    /// <summary>Whether an address is the share extension's: <c>grouplab://shared</c>.</summary>
    internal static bool IsHandoff(Uri? address) =>
        address is not null && string.Equals(address.Scheme, Handoff.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(address.Host, Handoff.Shared, StringComparison.OrdinalIgnoreCase);

    /// <summary>Pictures opened in GroupLab from another app: moved out of the Inbox, so each is read once, and handed over unread.</summary>
    internal static IReadOnlyList<PhotoHandle> Opened(IEnumerable<IStorageItem> items)
    {
        var handles = new List<PhotoHandle>();
        foreach (var item in items)
        {
            if (item.TryGetLocalPath() is not { } path || !File.Exists(path))
            {
                continue;
            }

            string claimed = Path.Combine(Claimed, Guid.NewGuid().ToString("N") + Path.GetExtension(path));
            try
            {
                File.Move(path, claimed);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // A file iOS let GroupLab read in place: copied instead, and the original left where it is.
                File.Copy(path, claimed, overwrite: true);
            }

            handles.Add(PhotoPickers.FromFile(claimed));
        }

        return handles;
    }

    /// <summary>
    /// The shares waiting in the app group, oldest first, each moved into GroupLab's own cache so it is read once. Only the newest share is
    /// read, as a set where it holds several; an older one left behind, which the person has not been waiting for, is let go.
    /// </summary>
    internal static IReadOnlyList<PhotoHandle> Waiting()
    {
        if (Handoff.Incoming() is not { } incoming)
        {
            return [];
        }

        var shares = Handoff.Waiting(incoming);
        if (shares.Count == 0)
        {
            return [];
        }

        foreach (string old in shares.Take(shares.Count - 1))
        {
            Remove(old);
        }

        var handles = new List<PhotoHandle>();
        foreach (string picture in Handoff.Pictures(shares[^1]))
        {
            string claimed = Path.Combine(Claimed, Guid.NewGuid().ToString("N") + Path.GetExtension(picture));
            File.Copy(picture, claimed, overwrite: true);
            handles.Add(PhotoPickers.FromFile(claimed));
        }

        Remove(shares[^1]);
        DiagnosticLog.Info("ios.incoming.share", ("pictures", handles.Count), ("older", shares.Count - 1));
        return handles;
    }

    /// <summary>Hands pictures to the Capture screen, once it is there; the first start builds it a moment after the address arrives.</summary>
    internal static void Hand(IReadOnlyList<PhotoHandle> handles, string how)
    {
        if (handles.Count == 0)
        {
            return;
        }

        DiagnosticLog.Info("phone.shared", ("action", how), ("pictures", handles.Count));
        int tries = 0;
        void Try()
        {
            if (CapturePage.SharedPicture is { } shared)
            {
                shared(handles);
            }
            else if (++tries < 60)
            {
                DispatcherTimer.RunOnce(Try, TimeSpan.FromMilliseconds(500));
            }
        }

        Dispatcher.UIThread.Post(Try);
    }

    private static void Remove(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Left in the app group's container; the next share is read after it, and this one never again.
        }
    }
}
