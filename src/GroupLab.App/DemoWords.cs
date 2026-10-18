namespace GroupLab.App;

/// <summary>The demo's words, in one place, the desktop's and the phones' (entry 446: the phones' file links this one).</summary>
internal static class DemoWords
{
    public const string TabName = "Demo: sample sheet";
    public const string Offer = "Walk me through a demo (about five minutes)";
    public const string OfferSays = "GroupLab can show you how it works on a sample sheet it makes for the tour, nobody's target: reading it, the analysis, " +
        "printing, ballistics and where to get help. Nothing the demo makes is kept unless you ask.";
    public const string NotNow = "Not now";
    public const string SettingsRow = "Walk me through a demo";
    public const string SettingsSays = "A guided tour of GroupLab on a sample sheet, about five minutes. You can end it at any step.";
    public const string Back = "Back";
    public const string Next = "Next";
    public const string Finish = "Finish";
    public const string End = "End the tour";
    public const string Keep = "Keep this demo in Sessions";
    public const string Ended = "The demo has ended, and what it made has been removed.";
    public const string Kept = "The demo has ended. Its session is kept in Session records.";
    public const string KeptOnPhone = "The demo has ended. Its session is kept in Sessions.";

    public static string Of(int step, int count) => $"{step} of {count}";

    public static string CarryOn(int step, int count) => $"Carry on from step {step} of {count}";
}
