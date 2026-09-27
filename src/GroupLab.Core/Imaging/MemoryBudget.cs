namespace GroupLab.Core.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 240: how much memory an analysis may use, from what the device says it has, in place of entry 206's single
/// number. The costs are measured on the Galaxy Z Fold 7 (entry 209): the published scan at 8 megapixels peaked at 373 MB and at 32 at 715,
/// so an analysis costs about <see cref="BaseMegabytes"/> and <see cref="PerMegapixelMegabytes"/> for every megapixel it works at.
/// <para>
/// The budget buys nothing yet, and says so. Entry 240 section 1.3 measured whether working above 8 megapixels changes a result: on the
/// published scan the shots moved by about 0.0005 in between 8 and 24 megapixels, and on three kitchen photographs 8 and 12 megapixels
/// agreed to 0.001 in against a photo-to-scan difference of 0.015 to 0.027 in, with extra marks going either way. So the phone works at 8
/// everywhere, and the budget is what keeps a device short of memory at the floor instead of letting it fail. More memory never makes two
/// phones' results differ.
/// </para>
/// </summary>
public static class MemoryBudget
{
    /// <summary>What an analysis costs before its image, megabytes.</summary>
    public const double BaseMegabytes = 260;

    /// <summary>What each megapixel of working image adds, megabytes: (715 - 373) / 24.</summary>
    public const double PerMegapixelMegabytes = 14.25;

    /// <summary>The floor, today's 4 GB phone at 8 megapixels, under about 400 MB.</summary>
    public const double FloorMegabytes = 400;

    /// <summary>The share of what the device says is available that one analysis may take.</summary>
    public const double AvailableShare = 0.25;

    /// <summary>The share of the device's whole memory never exceeded, well below where Android would stop the application or others.</summary>
    public const double CeilingShareOfTotal = 0.10;

    /// <summary>
    /// The phone's budget in megabytes: a quarter of what is available above the low memory threshold, never under
    /// <see cref="FloorMegabytes"/> and never over a tenth of the device's memory; the floor itself when the device says memory is low.
    /// </summary>
    public static double Phone(double totalMegabytes, double availableMegabytes, double thresholdMegabytes, bool low)
    {
        if (low)
        {
            return FloorMegabytes;
        }

        double share = AvailableShare * Math.Max(0, availableMegabytes - thresholdMegabytes);
        double ceiling = Math.Max(FloorMegabytes, CeilingShareOfTotal * totalMegabytes);
        return Math.Clamp(share, FloorMegabytes, ceiling);
    }

    /// <summary>
    /// The working size a budget allows, megapixels: what it holds at the measured costs, and never more than
    /// <see cref="WorkingSize.PhoneMegapixels"/>, because more buys nothing measurable (entry 240). The floor holds 9.8, so every phone works
    /// at 8.
    /// </summary>
    public static double PhoneWorkingMegapixels(double budgetMegabytes) =>
        Math.Min(WorkingSize.PhoneMegapixels, (budgetMegabytes - BaseMegabytes) / PerMegapixelMegabytes);

    /// <summary>
    /// The largest image a desktop reads whole, megapixels: what half its memory holds at the measured costs, never more than
    /// <paramref name="hardLimitMegapixels"/>. Entry 240 section 1.5: a desktop keeps full size, and memory only decides where it must stop.
    /// </summary>
    public static double DesktopMostMegapixels(double totalMegabytes, double hardLimitMegapixels) =>
        Math.Min(hardLimitMegapixels, Math.Max(WorkingSize.LargeImageMegapixels, ((0.5 * totalMegabytes) - BaseMegabytes) / PerMegapixelMegabytes));
}
