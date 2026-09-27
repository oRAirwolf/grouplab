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

    /// <summary>
    /// Entry 227 section 2: said before the person answers, so nobody wonders afterwards. Alan pressed Yes on nightly 110 and was never
    /// offered the benchmark, because the offer sat under the question and went with it.
    /// </summary>
    public const string BenchmarkOffer = "Yes does not run the benchmark. The benchmark is a separate test that times one analysis of a built-in target, the same work on every machine; when you have answered, you can run it now or later from Settings. It never starts by itself.";

    public const string BenchmarkButton = "Run the benchmark now";

    public const string BenchmarkNowQuestion = "Run the benchmark now?";

    public const string BenchmarkNowExplained = "It takes about a minute on most computers, and its times go with your next report. You can cancel it while it runs.";

    public const string BenchmarkRunNow = "Run it now";

    public const string BenchmarkLater = "Later";

    public const string BenchmarkLaterSaid = "You can run it any time from Settings, under Sharing.";

    public const string BenchmarkCancel = "Cancel";

    public const string BenchmarkCancelled = "The benchmark was canceled. Nothing from it is kept or sent.";

    public const string BenchmarkNever = "The benchmark has not run here yet. It runs only when you start it.";

    public const string BenchmarkFailed = "The benchmark could not finish, and nothing from it is kept. The log says why.";

    /// <summary>The benchmark moving: the stage it has reached, of about how many, and the seconds so far.</summary>
    public static string BenchmarkProgress(int stagesDone, int stagesExpected, double seconds) => string.Create(CultureInfo.CurrentCulture,
        $"Running the benchmark: stage {Math.Min(stagesDone + 1, stagesExpected)} of about {stagesExpected}, {seconds:0} seconds so far.");

    /// <summary>What Settings says about the benchmark that last ran: when, and what it found.</summary>
    public static string BenchmarkLast(DateTimeOffset? ranAt, BenchmarkResult result, bool sent) =>
        (ranAt is { } at ? string.Create(CultureInfo.CurrentCulture, $"Last run {at.ToLocalTime():d MMMM yyyy, HH:mm}. ") : "Last run before GroupLab kept the time. ")
        + BenchmarkDone(result, goes: false) + (sent ? " It went with a report." : "");

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
