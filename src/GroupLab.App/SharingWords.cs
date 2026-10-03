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

    /// <summary>
    /// Entry 299: the one short line each of Settings' sharing sections shows under its choice; the full explanation is behind "More".
    /// </summary>
    public const string TargetsShort = "Targets you analyze can go to the project to test and improve detection.";

    public const string ErrorsShort = "A report of an error can go to the project, where it is fixed.";

    public const string SurveyShort = "Once a week, what GroupLab runs on and how fast.";

    /// <summary>Entry 299: the page that says exactly what GroupLab sends, linked from Settings where it can always be seen.</summary>
    public const string WhatIsSentLabel = "What GroupLab sends";

    public const string WhatIsSentAddress = "https://grouplab.org/research/what-grouplab-sends/";

    /// <summary>Entry 299: the consent level's name alone, for the choice in Settings; the receiver's full description is under "More".</summary>
    public static string LevelName(ConsentLevel level) => level == ConsentLevel.Publishable ? MayBePublished.Trim() : TestingOnly.Trim();

    public const string TargetsIntro = "Each target you analyze, once you press Accept and analyze, can go to the project, to test and improve detection. A picture GroupLab could not read is not sent. This is what goes:";

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

    /// <summary>NOTES-FROM-PLANNING.md entry 241 section 2.5: said above the question to somebody who said yes to its earlier wording.</summary>
    public const string SurveyWordingChanged = "What the hardware survey sends has changed since you said yes: every benchmark run now goes, and the random number is explained below. Nothing more is sent until you answer again.";

    /// <summary>Entry 241 section 1.2: the heading over the device's own runs in Settings.</summary>
    public const string BenchmarkHistory = "Your benchmark runs";

    /// <summary>One run in Settings' history: the date, the version and the time, and whether it has gone.</summary>
    public static string BenchmarkRunLine(BenchmarkRunRecord run) =>
        (run.RanAt is { } at ? string.Create(CultureInfo.CurrentCulture, $"{at.ToLocalTime():d MMM yyyy}") : "Earlier")
        + ", " + (run.Version is { } v ? $"GroupLab {v}" : "an earlier GroupLab")
        + string.Create(CultureInfo.CurrentCulture, $", {run.Result.TotalMilliseconds / 1000.0:0.0} s")
        + (run.Sent ? ", sent" : run.ToSend ? ", waiting to go" : ", not sent (the survey was off)");

    /// <summary>Entry 241 section 2.4: the two buttons under the history, and what each says when pressed.</summary>
    public const string ResetNumber = "Reset my survey number";

    public const string ResetNumberSaid = "This copy of GroupLab has a new survey number. Runs sent before stay counted under the old one.";

    public const string DeleteReports = "Delete my survey reports";

    public const string DeleteReportsSaid = "The project's server will delete everything it keeps under this copy's survey number at its next hourly run, and count the published figures again without it.";

    public const string DeleteReportsFailed = "The request to delete your survey reports could not reach the project just now. Nothing was changed; try again later.";

    /// <summary>What the benchmark found, in one sentence, and whether it goes with the next report.</summary>
    public static string BenchmarkDone(BenchmarkResult result, bool goes)
    {
        ArgumentNullException.ThrowIfNull(result);
        return string.Create(CultureInfo.CurrentCulture,
            $"The benchmark took {result.TotalMilliseconds / 1000.0:0.0} seconds and at most {result.PeakMegabytes:N0} MB of memory, and found {result.HolesFound} of its {result.HolesPlaced} holes.")
            + (goes ? " It goes with your next report." : "");
    }
}
