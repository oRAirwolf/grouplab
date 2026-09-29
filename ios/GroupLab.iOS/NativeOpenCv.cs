using System.Reflection;
using System.Runtime.InteropServices;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 3: OpenCvSharp's managed half calls its native half by the library name
/// <c>OpenCvSharpExtern</c>. iOS loads no library of an application's own from outside it, so ios/opencv/build-extern.sh builds that half
/// as a static archive and the head links it into the application itself (a NativeReference, forced whole). This points the name at the
/// application's own executable, where every one of its entry points now is.
/// </summary>
internal static class NativeOpenCv
{
    private const string Library = "OpenCvSharpExtern";

    /// <summary>Whether this build linked OpenCV; a build made without the framework compiles and runs, and its imaging says it is missing.</summary>
    internal static bool Linked =>
#if GROUPLAB_OPENCV
        true;
#else
        false;
#endif

    private static bool resolving;

    internal static void Resolve()
    {
        if (resolving)
        {
            return;
        }

        resolving = true;
        NativeLibrary.SetDllImportResolver(typeof(OpenCvSharp.Cv2).Assembly, Resolver);
    }

    /// <summary>How often the runtime asked for a library of OpenCvSharp's, and the last name it asked for, for the self-test.</summary>
    internal static int Asked { get; private set; }

    internal static string? LastAsked { get; private set; }

    private static IntPtr Resolver(string name, Assembly assembly, DllImportSearchPath? path)
    {
        Asked++;
        LastAsked = name;
        return name == Library ? MainProgram() : IntPtr.Zero;
    }

    /// <summary>What the self-test reports: the executable's handle and whether an OpenCV entry point is found through it.</summary>
    internal static (bool Handle, bool Export) Probe()
    {
        IntPtr handle = MainProgram();
        return (handle != IntPtr.Zero, handle != IntPtr.Zero && NativeLibrary.TryGetExport(handle, "core_Mat_new1", out _));
    }

    private static IntPtr main;

    /// <summary>The application's own executable, where the static archive's symbols are.</summary>
    private static IntPtr MainProgram()
    {
        if (main != IntPtr.Zero)
        {
            return main;
        }

        try
        {
            main = NativeLibrary.GetMainProgramHandle();
        }
        catch (Exception e) when (e is PlatformNotSupportedException or NotImplementedException or EntryPointNotFoundException)
        {
            main = OpenSelf(IntPtr.Zero, RtldNow);
        }

        return main;
    }

    private const int RtldNow = 2;

    [DllImport("/usr/lib/libSystem.dylib", EntryPoint = "dlopen")]
    private static extern IntPtr OpenSelf(IntPtr path, int mode);
}
