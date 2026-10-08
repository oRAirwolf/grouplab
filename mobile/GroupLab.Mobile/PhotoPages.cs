using Avalonia.Controls;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 section 1: the screens' side of choosing a photograph from any app. The picker the platform opens, then
/// each photograph fetched with a line saying which app it comes from and how far it has got, and a Cancel; a reduced copy is said to be one
/// before it is read, with another way to reach the whole photograph.
/// </summary>
internal static class PhotoPages
{
    /// <summary>Opens the picker for <paramref name="source"/> and reads what was chosen into the cache; <paramref name="said"/> hears why not.</summary>
    internal static async Task<IReadOnlyList<PickedPhoto>> Pick(ContentControl host, PhotoSource source, string name, Action<string> said)
    {
        IReadOnlyList<PhotoHandle> handles;
        try
        {
#if GROUPLAB_DEV
            // Entry 388 section 1: a scenario's staged photo, where one is waiting, stands in for the person choosing.
            handles = GroupLab.Mobile.Dev.Scenario.NextPick is { } staged ? [staged] : await Phone.Platform.PickPhotos(source, TopLevel.GetTopLevel(host));
            GroupLab.Mobile.Dev.Scenario.NextPick = null;
#else
            handles = await Phone.Platform.PickPhotos(source, TopLevel.GetTopLevel(host));
#endif
        }
        catch (Exception e) when (e is InvalidOperationException or IOException or NotSupportedException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "phone.pick", e);
            said("The picker could not be opened. Try From another app, or share the photo into GroupLab from the app that keeps it.");
            return [];
        }

        DiagnosticLog.Info("phone.pick", ("source", source.ToString()), ("chosen", handles.Count));
        return await Read(host, handles, name, said);
    }

    /// <summary>
    /// Reads each photograph into the cache in turn. The progress page appears only when a fetch takes long enough to notice, as a
    /// photograph kept only in the cloud does, and Cancel returns to where the person was with nothing kept.
    /// </summary>
    internal static async Task<IReadOnlyList<PickedPhoto>> Read(ContentControl host, IReadOnlyList<PhotoHandle> handles, string name, Action<string> said)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(handles);
        if (handles.Count == 0)
        {
            return [];
        }

        object? before = host.Content;
        using var cancel = new CancellationTokenSource();
        var (page, line, stop) = Screens.Progress(handles.Count > 1 ? "Getting the photos" : "Getting the photo");
        // Entry 313 section 1: Cancel never throws, even pressed as the page goes away.
        bool over = false;
        stop.Click += (_, _) =>
        {
            if (!over)
            {
                cancel.Cancel();
            }
        };
        bool shown = false;
        var read = new List<PickedPhoto>();
        var failed = new List<string>();
        try
        {
            for (int i = 0; i < handles.Count; i++)
            {
                var fetch = PhotoIntake.Fetch(handles[i], Phone.Platform.CacheFolder, name, i, handles.Count,
                    words => Dispatcher.UIThread.Post(() => line.Text = words), cancel.Token);
                if (!shown && await Task.WhenAny(fetch, Task.Delay(300)) != fetch)
                {
                    shown = true;
                    host.Content = page;
                }

                var got = await fetch;
                if (got.Canceled)
                {
                    Forget(read);
                    return [];
                }

                if (got.Photo is { } photo)
                {
                    read.Add(photo);
                }
                else if (got.Said is { } why)
                {
                    failed.Add(why);
                }
            }
        }
        finally
        {
            over = true;
            if (shown)
            {
                host.Content = before;
            }
        }

        if (failed.Count > 0)
        {
            said(failed[0] + (failed.Count > 1 ? $" {failed.Count - 1} more could not be read either." : ""));
        }

        return read;
    }

    /// <summary>
    /// Section 1.4: a photograph the app handed over smaller than it is, said before it is read, with the way to the whole one; true where the
    /// person reads it anyway.
    /// </summary>
    internal static Task<bool> UseReduced(ContentControl host, string words)
    {
        ArgumentNullException.ThrowIfNull(host);
        var answer = new TaskCompletionSource<bool>();
        object? before = host.Content;
        void Answer(bool read)
        {
            host.Content = before;
            DiagnosticLog.Info("phone.pick.reduced", ("read", read));
            answer.TrySetResult(read);
        }

        host.Content = Screens.Page(new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Screens.Title("A smaller copy"),
                Screens.Line(words),
                Screens.Primary("Read it anyway", () => Answer(true)).Id("photo-read-anyway"),
                Screens.Choice("Choose another way", () => Answer(false)).Id("photo-choose-another"),
            },
        });
        return answer.Task;
    }

    /// <summary>The photographs read so far, deleted: nothing of a canceled choice is kept.</summary>
    internal static void Forget(IEnumerable<PickedPhoto> photos)
    {
        foreach (var photo in photos)
        {
            try
            {
                File.Delete(photo.Path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Left in the cache, which the phone empties itself.
            }
        }
    }
}
