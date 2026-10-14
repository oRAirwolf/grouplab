namespace GroupLab.App;

/// <summary>Whether the computer asks for less motion: Windows' "Animation effects" off, macOS's "Reduce motion" on, GNOME's animations off.</summary>
internal static class SystemMotion
{
    internal static bool Reduced()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return SystemParametersInfo(0x1042 /* SPI_GETCLIENTAREAANIMATION */, 0, out int on, 0) && on == 0;
            }

            if (OperatingSystem.IsMacOS())
            {
                var workspace = objc_msgSend(objc_getClass("NSWorkspace"), sel_registerName("sharedWorkspace"));
                return workspace != IntPtr.Zero && objc_msgSend_bool(workspace, sel_registerName("accessibilityDisplayShouldReduceMotion"));
            }

            if (OperatingSystem.IsLinux())
            {
                string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "gtk-3.0", "settings.ini");
                return File.Exists(settings) && File.ReadLines(settings).Any(l => l.Replace(" ", "", StringComparison.Ordinal).Equals("gtk-enable-animations=false", StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or IOException or UnauthorizedAccessException)
        {
            // Not knowing is the same as animations on: a flash is a hint, never the only way to see where the column went.
        }

        return false;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint param, out int value, uint winIni);

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_getClass(string name);

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string name);

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector);
}
