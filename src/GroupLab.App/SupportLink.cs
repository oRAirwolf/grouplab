namespace GroupLab.App;

/// <summary>
/// The support link, NOTES-FROM-PLANNING.md entry 120 section 9 answering question 28. `DESIGN.md` section 20 promises "an unobtrusive
/// support link, one menu item opening a browser, with no payment handled inside the application", and there is no address yet: Alan may
/// register a domain later.
/// <para>
/// <b>This is the one place an address may be written.</b> Until there is one, <see cref="Address"/> stays null and the item says so rather
/// than opening anything. Nothing here is invented: a made-up domain in a shipped build would send a person who wants to help the project to
/// somebody else's website, and <c>SupportLinkTests</c> fails if an address appears anywhere else.
/// </para>
/// </summary>
public static class SupportLink
{
    /// <summary>The page to open, or null while there is none. One constant, one place.</summary>
    public static string? Address => null;

    /// <summary>What the item is called.</summary>
    public const string Label = "Support GroupLab";

    /// <summary>What it says while there is no address: the truth, rather than a dead link or a silent button.</summary>
    public const string NoAddressYet = "There is no support address yet. When there is one it will open here, and GroupLab will never take a payment inside the application.";

    /// <summary>Whether there is somewhere to go.</summary>
    public static bool Exists => !string.IsNullOrWhiteSpace(Address);
}
