using Avalonia;
using Avalonia.iOS;
using Foundation;
using UIKit;

namespace GroupLab.iOS;

/// <summary>NOTES-FROM-PLANNING.md entry 290 section 2 item 3: the application's start on iPhone and iPad.</summary>
public static class Program
{
    /// <summary>The launch arguments, for the simulator's self-test (<see cref="SelfTest"/>); an application from the Home Screen has none.</summary>
    internal static IReadOnlyList<string> Arguments { get; private set; } = [];

    public static void Main(string[] args)
    {
        Arguments = args;

        // OpenCvSharp calls its native half by name; on iOS that half is linked into the application itself.
        NativeOpenCv.Resolve();

        // The desktop's log goes beside the executable or in the user's folder; the phone's goes in the application's own files.
        Environment.SetEnvironmentVariable(GroupLab.App.Diagnostics.LogDirectory.Override, Path.Combine(IosPhone.Documents, "logs"));
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}

/// <summary>The application delegate: Avalonia's own, which makes the one view the shared screens live in.</summary>
[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) => base.CustomizeAppBuilder(builder);
}
