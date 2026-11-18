using System.Runtime.InteropServices;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 501: what the Mac App Store copy, which runs in Apple's App Sandbox, needs from macOS itself. A folder the
/// person chose (the sync folder) can be read again after a restart only through a security-scoped bookmark made while the choice still
/// holds, and resolved, with access started, before its path is used; GroupLab reads and writes that folder by its path, so the bookmark
/// is made and resolved here rather than through the window's storage items. Links and files are opened through NSWorkspace, which the
/// sandbox allows, rather than by starting the <c>open</c> command. Everything returns null or false when it cannot be done, and nothing
/// here is used outside the sandboxed copy (<see cref="Diagnostics.AppInfo.Sandboxed"/>).
/// </summary>
internal static class MacSandbox
{
    private const ulong BookmarkWithSecurityScope = 1UL << 11;
    private const ulong ResolveWithSecurityScope = 1UL << 10;

    /// <summary>Resolved folders whose access has been started, kept for the life of the run so their access is never stopped.</summary>
    private static readonly List<IntPtr> Started = [];

    /// <summary>A security-scoped bookmark of a folder or file the person has just chosen, as Base64, or null.</summary>
    internal static string? Bookmark(string path)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return null;
        }

        try
        {
            var url = FileUrl(path);
            if (url == IntPtr.Zero)
            {
                return null;
            }

            var data = Send(url, "bookmarkDataWithOptions:includingResourceValuesForKeys:relativeToURL:error:", BookmarkWithSecurityScope, IntPtr.Zero, IntPtr.Zero, out _);
            return data == IntPtr.Zero ? null : Convert.ToBase64String(Bytes(data));
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            return null;
        }
    }

    /// <summary>The path a bookmark leads to, with the sandbox's access to it started for the rest of the run, or null.</summary>
    internal static string? Resolve(string bookmark)
    {
        if (!OperatingSystem.IsMacOS() || string.IsNullOrEmpty(bookmark))
        {
            return null;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(bookmark);
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            IntPtr data;
            try
            {
                data = SendBytes(Class("NSData"), "dataWithBytes:length:", handle.AddrOfPinnedObject(), (ulong)bytes.Length);
            }
            finally
            {
                handle.Free();
            }

            var url = SendResolve(Class("NSURL"), "URLByResolvingBookmarkData:options:relativeToURL:bookmarkDataIsStale:error:", data, ResolveWithSecurityScope,
                IntPtr.Zero, out _, out _);
            if (url == IntPtr.Zero || !SendBool(url, "startAccessingSecurityScopedResource"))
            {
                return null;
            }

            lock (Started)
            {
                Started.Add(Send(url, "retain"));
            }

            return Text(Send(url, "path"));
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException or FormatException)
        {
            return null;
        }
    }

    /// <summary>Opens a web address or a file as macOS opens it, through NSWorkspace. False where it could not.</summary>
    internal static bool Open(string what)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        try
        {
            bool web = what.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || what.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || what.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
            var url = web ? Send(Class("NSURL"), "URLWithString:", NSString(what)) : FileUrl(what);
            var workspace = Send(Class("NSWorkspace"), "sharedWorkspace");
            return url != IntPtr.Zero && workspace != IntPtr.Zero && SendBool(workspace, "openURL:", url);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            return false;
        }
    }

    private static IntPtr FileUrl(string path) => Send(Class("NSURL"), "fileURLWithPath:", NSString(path));

    private static IntPtr NSString(string text)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(text);
        try
        {
            return Send(Class("NSString"), "stringWithUTF8String:", utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    private static string? Text(IntPtr nsString) => nsString == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(Send(nsString, "UTF8String"));

    private static byte[] Bytes(IntPtr nsData)
    {
        var length = (int)SendLength(nsData, Selector("length"));
        var bytes = new byte[length];
        if (length > 0)
        {
            Marshal.Copy(Send(nsData, "bytes"), bytes, 0, length);
        }

        return bytes;
    }

    private static IntPtr Class(string name) => objc_getClass(name);

    private static IntPtr Selector(string name) => sel_registerName(name);

    private static IntPtr Send(IntPtr receiver, string selector) => objc_msgSend(receiver, Selector(selector));

    private static IntPtr Send(IntPtr receiver, string selector, IntPtr argument) => objc_msgSend(receiver, Selector(selector), argument);

    private static bool SendBool(IntPtr receiver, string selector) => objc_msgSend_bool(receiver, Selector(selector));

    private static bool SendBool(IntPtr receiver, string selector, IntPtr argument) => objc_msgSend_bool(receiver, Selector(selector), argument);

    private static IntPtr Send(IntPtr receiver, string selector, ulong options, IntPtr keys, IntPtr relative, out IntPtr error) =>
        objc_msgSend(receiver, Selector(selector), options, keys, relative, out error);

    private static IntPtr SendBytes(IntPtr receiver, string selector, IntPtr bytes, ulong length) => objc_msgSend(receiver, Selector(selector), bytes, length);

    private static IntPtr SendResolve(IntPtr receiver, string selector, IntPtr data, ulong options, IntPtr relative, out byte stale, out IntPtr error) =>
        objc_msgSend(receiver, Selector(selector), data, options, relative, out stale, out error);

    private const string ObjC = "/usr/lib/libobjc.dylib";

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjC)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr bytes, ulong length);

    [DllImport(ObjC)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, ulong options, IntPtr keys, IntPtr relative, out IntPtr error);

    [DllImport(ObjC)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr data, ulong options, IntPtr relative, out byte stale, out IntPtr error);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern ulong SendLength(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector, IntPtr argument);
}
