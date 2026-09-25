using GroupLab.App.Diagnostics;
using System.Globalization;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;

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

    public const string TargetsLater = "You can change this at any time in Settings, under Sharing.";

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

    public const string ErrorsLater = "You can change this at any time in Settings, under Sharing.";

    /// <summary>The error report choices, in the order they are offered on the first run screen.</summary>
    public static IReadOnlyList<(ErrorReportChoice Choice, string Words)> ErrorChoices { get; } =
        [(ErrorReportChoice.Always, "Send them automatically"), (ErrorReportChoice.Ask, "Ask me each time"), (ErrorReportChoice.Never, "Never send them")];

    public const string SurveyQuestion = "Take part in the hardware survey?";

    public const string SurveyIntro = "Once a week, GroupLab can tell the project what it runs on and how fast, so the machines it has to run on are known rather than guessed. This is what a report holds:";

    /// <summary>The survey's choices, in the order they are offered.</summary>
    public static IReadOnlyList<(SurveyChoice Choice, string Words)> SurveyChoices { get; } = [(SurveyChoice.Yes, "Yes, take part"), (SurveyChoice.No, "No")];

    public const string BenchmarkOffer = "The benchmark times one analysis of a built-in target, the same work on every machine. It never starts by itself: run it now, or later from Settings.";

    public const string BenchmarkButton = "Run the benchmark";

    public const string BenchmarkRunning = "Running the benchmark…";

    public const string SurveyLater = "You can change this at any time in Settings, under Sharing.";

    public const string SurveyClosed = "The hardware survey is not open yet. When it is, GroupLab will ask once whether you want to take part.";

    public const string EarlierKept = "Your earlier answers about sending targets and error reports are kept. You can change them in Settings, under Sharing.";

    /// <summary>What the benchmark found, in one sentence, and whether it goes with the next report.</summary>
    public static string BenchmarkDone(BenchmarkResult result, bool goes)
    {
        ArgumentNullException.ThrowIfNull(result);
        return string.Create(CultureInfo.CurrentCulture,
            $"The benchmark took {result.TotalMilliseconds / 1000.0:0.0} seconds and at most {result.PeakMegabytes:N0} MB of memory, and found {result.HolesFound} of its {result.HolesPlaced} holes.")
            + (goes ? " It goes with your next report." : "");
    }
}
