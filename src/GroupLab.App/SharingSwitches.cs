using GroupLab.Core.Publication;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 357's two switches, as the application reads them: the build's limits.json, which a test may override for
/// its own run. Both are false until Alan confirms the store privacy answers, and while one is false nothing it governs is shown, asked or
/// sent. Nothing here draws anything: the phone compiles this file as it is.
/// </summary>
internal static class SharingSwitches
{
    /// <summary>A test's own value for <see cref="EverythingOpen"/>, or null for the build's.</summary>
    internal static bool? EverythingOverride { get; set; }

    /// <summary>A test's own value for <see cref="FullLogOpen"/>, or null for the build's.</summary>
    internal static bool? FullLogOverride { get; set; }

    /// <summary>Whether "Send everything I open" is offered, section 1.</summary>
    public static bool EverythingOpen => EverythingOverride ?? ReceiverTerms.Current.SendEverythingOpen;

    /// <summary>Whether an automatic error report carries the log package, section 2.</summary>
    public static bool FullLogOpen => FullLogOverride ?? ReceiverTerms.Current.FullLogErrorReports;
}
