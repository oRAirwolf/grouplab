using GroupLab.Core.Publication;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 357's two switches, as the application reads them: the build's limits.json, which a test may override for
/// its own run. Entry 363: on for the computer since Alan's yes; on the phones, off until he confirms the store privacy answers, and while one
/// is off nothing it governs is shown, asked or sent. Nothing here draws anything: the phone compiles this file as it is.
/// </summary>
internal static class SharingSwitches
{
    /// <summary>A test's own value for <see cref="EverythingOpen"/>, or null for the build's.</summary>
    internal static bool? EverythingOverride { get; set; }

    /// <summary>A test's own value for <see cref="FullLogOpen"/>, or null for the build's.</summary>
    internal static bool? FullLogOverride { get; set; }

#if GROUPLAB_MOBILE
    // Entry 363 section 1: on the phones both stay off until the App Privacy and Data safety answers are updated.
    private static bool Everything => ReceiverTerms.Current.SendEverythingOpen && ReceiverTerms.Current.SendEverythingOpenPhones;

    private static bool FullLog => ReceiverTerms.Current.FullLogErrorReports && ReceiverTerms.Current.FullLogErrorReportsPhones;
#else
    private static bool Everything => ReceiverTerms.Current.SendEverythingOpen;

    private static bool FullLog => ReceiverTerms.Current.FullLogErrorReports;
#endif

    /// <summary>Whether "Send everything I open" is offered, section 1.</summary>
    public static bool EverythingOpen => EverythingOverride ?? Everything;

    /// <summary>A test's own value for <see cref="TargetsFromPhone"/>, or null for the build's.</summary>
    internal static bool? TargetsFromPhoneOverride { get; set; }

    /// <summary>Entry 363 section 3.5: whether the phone's sender of targets is on, off until the stores' privacy answers are updated.</summary>
    public static bool TargetsFromPhone => TargetsFromPhoneOverride ?? ReceiverTerms.Current.SendTargetsPhones;

    /// <summary>Whether an automatic error report carries the log package, section 2.</summary>
    public static bool FullLogOpen => FullLogOverride ?? FullLog;
}
