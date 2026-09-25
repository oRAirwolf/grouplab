using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3: the words the first run screen and Settings use to ask what may be shared, in one place,
/// because the desktop and the phone ask the same questions and entry 208 says in the same order and words. Nothing here draws anything:
/// the Android application compiles this file as it is.
/// </summary>
internal static class SharingWords
{
    public const string TargetsQuestion = "Send your targets to help improve GroupLab?";

    public const string TargetsIntro = "Each target you analyze can go to the project, to test and improve detection. This is what goes:";

    public const string TestingOnly = "Testing only. ";

    public const string MayBePublished = "May be published. ";

    public const string LevelFirst = "Choose testing only or may be published first, so every target goes with the consent you mean.";

    public const string TargetsLater = "You can change this at any time in Settings, under Sending targets.";

    public const string TargetsClosed = "Sending targets to the project is not open yet. When it is, GroupLab will ask once whether you want to.";

    public const string LevelHeading = "Consent for targets sent from now on";

    /// <summary>The two consent levels, with the receiver's own description of each.</summary>
    public static IReadOnlyList<(ConsentLevel Level, string Words)> Levels(ReceiverTerms terms) =>
        [(ConsentLevel.Testing, TestingOnly + terms.TestingText), (ConsentLevel.Publishable, MayBePublished + terms.PublishableText)];

    /// <summary>The target choices, in the order they are offered.</summary>
    public static IReadOnlyList<(SendingChoice Choice, string Words)> TargetChoices { get; } =
        [(SendingChoice.Always, "Send every target automatically"), (SendingChoice.Ask, "Ask me each time"), (SendingChoice.Never, "Never")];

    public const string ErrorsQuestion = "Send error reports to the project?";

    public const string ErrorsIntro = "When GroupLab hits an error, a report of it can go to the project, where it is fixed. This is what a report holds:";

    public const string ErrorsLater = "You can change this at any time in Settings, under Error reports.";

    /// <summary>The error report choices, in the order they are offered on the first run screen.</summary>
    public static IReadOnlyList<(ErrorReportChoice Choice, string Words)> ErrorChoices { get; } =
        [(ErrorReportChoice.Always, "Send them automatically"), (ErrorReportChoice.Ask, "Ask me each time"), (ErrorReportChoice.Never, "Never send them")];
}
