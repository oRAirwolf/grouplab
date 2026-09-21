namespace GroupLab.App;

/// <summary>
/// The support link, NOTES-FROM-PLANNING.md entry 126 section 1, which answers question 28 for good. `DESIGN.md` section 20 promises "an
/// unobtrusive support link, one menu item opening a browser, with no payment handled inside the application", and grouplab.org is now live,
/// so the placeholder entry 120 section 9 left here has an address at last.
/// <para>
/// <b>This is the one place an address may be written.</b> <c>SupportLinkTests</c> fails if any other support address or domain appears in
/// anything a person receives, and it holds these two exactly: a made-up or stale domain in a shipped build would send somebody who wants to
/// help the project to a stranger's website.
/// </para>
/// </summary>
public static class SupportLink
{
    /// <summary>The page to open. One constant, one place, opened through <see cref="GroupLab.Core.Updates.IOutsideWorld"/> (entry 122).</summary>
    public static string? Address => "https://grouplab.org/support/";

    /// <summary>Where a report package can be emailed. It forwards to Alan.</summary>
    public const string Email = "support@grouplab.org";

    /// <summary>What the item is called.</summary>
    public const string Label = "Support GroupLab";

    /// <summary>What it says: where the button goes, and the promise that has been there since the beginning.</summary>
    public const string OpensTheSupportPage =
        "This opens the support page at grouplab.org, which says how to help and how to get in touch. GroupLab will never take a payment "
        + "inside the application.";

    /// <summary>
    /// What it said while there was no address, kept so the tests that hold the old behaviour can name it and so the change is legible in
    /// one place. Nothing shows this now.
    /// </summary>
    public const string NoAddressYet = "There is no support address yet. When there is one it will open here, and GroupLab will never take a payment inside the application.";

    /// <summary>Whether there is somewhere to go.</summary>
    public static bool Exists => !string.IsNullOrWhiteSpace(Address);
}
