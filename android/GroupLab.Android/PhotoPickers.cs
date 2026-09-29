using Android.Content;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using GroupLab.App.Diagnostics;
using GroupLab.Mobile;
using Uri = Android.Net.Uri;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 sections 1 and 4 on Android: the system photo picker, the apps that offer pictures by name, and the
/// pictures other apps hand over. Every one arrives as a content address; it becomes a <see cref="PhotoHandle"/> the shared screens fetch
/// with a progress line. No storage permission is asked for: the picker and the apps grant GroupLab the one picture chosen.
/// </summary>
internal static class PhotoPickers
{
    /// <summary>The request the pickers answer through <see cref="MainActivity.ForResult"/>.</summary>
    internal const int PickRequest = 2921;

    /// <summary>
    /// Whether the system photo picker is on this phone: built into Android 13 and later, added to Android 11 and 12 by Google Play system
    /// updates, and to older versions by Google Play services. A phone without Google Play services may have none (section 4 item 3).
    /// </summary>
    internal static bool PhotoPickerAvailable(Context context) => ActivityResultContracts.PickVisualMedia.InvokeIsPhotoPickerAvailable(context);

    /// <summary>The picker for <paramref name="opens"/>: the photo picker for images only, or a chooser of every app that offers pictures.</summary>
    internal static Intent For(Context context, PhotoSource opens)
    {
        if (opens == PhotoSource.Photos)
        {
            var request = new PickVisualMediaRequest.Builder().SetMediaType(ActivityResultContracts.PickVisualMedia.ImageOnly.Instance).Build();
            return new ActivityResultContracts.PickVisualMedia().CreateIntent(context, request);
        }

        // Section 1.2: GET_CONTENT, not OPEN_DOCUMENT, is what Google Photos, Samsung Gallery and every maker's gallery answer, and the
        // chooser lists them by name, as a photo editor's does. Nothing asks for pictures on the phone only: cloud ones are wanted too.
        var content = new Intent(Intent.ActionGetContent);
        content.SetType("image/*");
        content.AddCategory(Intent.CategoryOpenable);
        return Intent.CreateChooser(content, "Choose a photograph from")!;
    }

    /// <summary>The pictures a picker handed back: its one address, or each of several.</summary>
    internal static List<Uri> Returned(Intent? data)
    {
        var uris = new List<Uri>();
        if (data?.ClipData is { } clip)
        {
            for (int i = 0; i < clip.ItemCount; i++)
            {
                if (clip.GetItemAt(i)?.Uri is { } uri)
                {
                    uris.Add(uri);
                }
            }
        }

        if (uris.Count == 0 && data?.Data is { } one)
        {
            uris.Add(one);
        }

        return uris;
    }

    /// <summary>
    /// Section 1.3: the pictures another app shared or opened with GroupLab: one with Share, several with Share of a selection, one with Open
    /// with and Edit with. Several are a set, one per sheet.
    /// </summary>
    internal static List<Uri> Shared(Intent intent)
    {
        var uris = new List<Uri>();
        switch (intent.Action)
        {
            case Intent.ActionSend:
                if (Stream(intent) is { } one)
                {
                    uris.Add(one);
                }

                break;
            case Intent.ActionSendMultiple:
                uris.AddRange(Streams(intent));
                break;
            default:
                if (intent.Data is { } data)
                {
                    uris.Add(data);
                }

                break;
        }

        return uris.Count > 0 ? uris : Returned(intent);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422", Justification = "The typed call is used from Android 13; this is the older form for Android 10 to 12.")]
    private static Uri? Stream(Intent intent) =>
        OperatingSystem.IsAndroidVersionAtLeast(33)
            ? intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(Uri))) as Uri
#pragma warning disable CA1422
            : intent.GetParcelableExtra(Intent.ExtraStream) as Uri;
#pragma warning restore CA1422

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422", Justification = "The typed call is used from Android 13; this is the older form for Android 10 to 12.")]
    private static IEnumerable<Uri> Streams(Intent intent)
    {
        var list = OperatingSystem.IsAndroidVersionAtLeast(33)
            ? intent.GetParcelableArrayListExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(Uri)))
#pragma warning disable CA1422
            : intent.GetParcelableArrayListExtra(Intent.ExtraStream);
#pragma warning restore CA1422
        return list?.OfType<Uri>() ?? [];
    }

    /// <summary>
    /// Each picture as the shared screens take it: the app it comes from, the size the app states, and its stream, opened only when it is
    /// fetched. A stated type that is not a picture is left out. Nothing is asked of the app about where a picture was taken.
    /// </summary>
    internal static List<PhotoHandle> Handles(Context context, IEnumerable<Uri> uris)
    {
        var resolver = context.ContentResolver!;
        var handles = new List<PhotoHandle>();
        foreach (var uri in uris)
        {
            string? type = Try(() => resolver.GetType(uri));
            if (!PhotoIntake.IsImage(type))
            {
                continue;
            }

            var (width, height, bytes) = Stated(resolver, uri);
            handles.Add(new PhotoHandle(AppFor(context, uri), width, height, bytes, PhotoIntake.Extension(type),
                () => Task.FromResult(resolver.OpenInputStream(uri))));
        }

        DiagnosticLog.Info("phone.pick.handed", ("pictures", handles.Count), ("stated", handles.Count(h => h.Width is not null)));
        return handles;
    }

    /// <summary>The app that provides a picture, by the name a person knows it by, where Android says which app it is.</summary>
    private static string? AppFor(Context context, Uri uri)
    {
        string? authority = uri.Authority;
        if (authority is null || context.PackageManager is not { } packages)
        {
            return null;
        }

        var provider = Try(() => Provider(packages, authority));
        return PhotoIntake.AppName(authority, provider?.PackageName, Try(() => provider?.ApplicationInfo?.LoadLabel(packages)));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422", Justification = "The typed call is used from Android 13; this is the older form for Android 10 to 12.")]
    private static global::Android.Content.PM.ProviderInfo? Provider(global::Android.Content.PM.PackageManager packages, string authority) =>
        OperatingSystem.IsAndroidVersionAtLeast(33)
            ? packages.ResolveContentProvider(authority, global::Android.Content.PM.PackageManager.ComponentInfoFlags.Of(0))
#pragma warning disable CA1422
            : packages.ResolveContentProvider(authority, 0);
#pragma warning restore CA1422

    /// <summary>
    /// Section 1.4: the size the app states for the picture, in pixels and bytes. Only these three columns are asked for, never a whole row,
    /// so no location an app keeps beside a picture is ever read; an app that knows only the size in bytes is asked for that alone.
    /// </summary>
    private static (int? Width, int? Height, long? Bytes) Stated(ContentResolver resolver, Uri uri)
    {
        foreach (string[] columns in (string[][])[["_size", "width", "height"], ["_size"]])
        {
            try
            {
                using var cursor = resolver.Query(uri, columns, (string?)null, null, null);
                if (cursor is null || !cursor.MoveToFirst())
                {
                    return (null, null, null);
                }

                long? Column(string name) => cursor.GetColumnIndex(name) is int at and >= 0 && !cursor.IsNull(at) && cursor.GetLong(at) > 0 ? cursor.GetLong(at) : null;
                return ((int?)Column("width"), (int?)Column("height"), Column("_size"));
            }
            catch (Java.Lang.Exception)
            {
                // An app that does not know a column says so by refusing the query; the next asks for less.
            }
        }

        return (null, null, null);
    }

    private static T? Try<T>(Func<T?> read)
        where T : class
    {
        try
        {
            return read();
        }
        catch (Java.Lang.Exception)
        {
            return null;
        }
    }
}
