using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GroupLab.Core.Marking;

/// <summary>
/// Phase 9 on the phones (Alan's queue of 2026-11-24, part 3): a short fingerprint of what a reading found, the exact bits of every bull's
/// and every hole's position, each hole's diameter and the bull it went to, so a change made for speed can be shown to give every result
/// identical on a phone or the emulator, where only the log comes back. It says nothing about the picture and holds no path or metadata.
/// </summary>
public static class ReadFingerprint
{
    /// <summary>Sixteen hex digits of SHA-256 over the reading's bulls and holes, in the order found; "none" for a reading with neither.</summary>
    public static string Of(AutomaticResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Bulls.Count == 0 && result.Detections.Count == 0)
        {
            return "none";
        }

        var text = new StringBuilder();
        foreach (var bull in result.Bulls)
        {
            text.Append(CultureInfo.InvariantCulture, $"b{bull.Index}:{Bits(bull.Image.X)}:{Bits(bull.Image.Y)};");
        }

        foreach (var hole in result.Detections)
        {
            text.Append(CultureInfo.InvariantCulture, $"h{Bits(hole.Image.X)}:{Bits(hole.Image.Y)}:{(hole.DiameterInches is { } d ? Bits(d) : "-")}:{hole.Assignment.Bull?.ToString(CultureInfo.InvariantCulture) ?? "-"};");
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..16].ToLowerInvariant();
    }

    private static string Bits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("x16", CultureInfo.InvariantCulture);
}
