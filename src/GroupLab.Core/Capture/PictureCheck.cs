using System.Globalization;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Capture;

/// <summary>The three bands of the quality bar: red, amber and green.</summary>
public enum PictureBand
{
    Retake,
    Usable,
    Good,
}

/// <summary>One numbered note on a picture, with the outline of what it is about in the picture's pixels where it has one.</summary>
public sealed record PictureNote(int Number, string Words, IReadOnlyList<PointD>? Outline);

/// <summary>
/// What GroupLab thought of a picture, entry 260 ("Feedback B"): the score and its band, the verdict word, the sentence that leads, the numbered
/// notes, what was fine, and whether GroupLab can measure it at all. <see cref="Verdict"/> is "Good", "Good with notes" or "Retake".
/// </summary>
public sealed record PictureVerdict(int Score, PictureBand Band, string Verdict, string Lead, IReadOnlyList<PictureNote> Notes, IReadOnlyList<string> Fine, bool CanMeasure)
{
    /// <summary>The band as the bar writes it beside the number, so colour is never the only signal.</summary>
    public string BandWord => PictureCheck.BandWord(Band);

    /// <summary>One line for the log: the score, the verdict and the notes.</summary>
    public string Describe() => string.Create(CultureInfo.InvariantCulture,
        $"{Score} {BandWord}, {Verdict}{(Notes.Count > 0 ? ": " + string.Join(" | ", Notes.Select(n => n.Words)) : "")}");
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 260: every picture is checked after it is taken, in Guided and in Manual mode, and a picture chosen from the
/// phone's files too, with the checks the capture screen's guidance uses and the analysis's own. Alan: "I want the app to want good pictures
/// but it should be able to handle less than ideal pictures." So a retake is asked for only where GroupLab cannot measure: the codes or the
/// markers unreadable, the sheet refused past the angle limit, or blur or size too great to find the holes. Shadows, uneven light and moderate
/// tilt are corrected and reported, most notes saying what GroupLab did about it.
/// <para>
/// **The score** is docs/MOBILE-CAPTURE.md section 5's, the weakest of its parts (focus, exposure, angle, resolution, markings read), with one
/// part more, the evenness of the light: the dimmest bull's paper against the brightest, perfect at <see cref="EvenLight"/> and worthless at
/// <see cref="UnevenLight"/>. Then the band is made to agree with the decision: a picture GroupLab can measure never scores below
/// <see cref="CaptureQualities.Usable"/>, and one it cannot never scores at or above it. So red always means take it again, and amber and green
/// are how good a measurable picture is.
/// </para>
/// </summary>
public static class PictureCheck
{
    /// <summary>The evenness at which the light counts as even: the dimmest bull's paper at 85 percent of the brightest.</summary>
    public const double EvenLight = 0.85;

    /// <summary>The evenness at which the light part is worthless.</summary>
    public const double UnevenLight = 0.4;

    /// <summary>A bull's paper below this share of the median bull's is in shadow, and named.</summary>
    public const double Shadowed = 0.8;

    public static string BandWord(PictureBand band) => band switch
    {
        PictureBand.Good => "Good",
        PictureBand.Usable => "Usable",
        _ => "Retake",
    };

    /// <summary>The most a picture with any note scores: short of perfect, and still well inside good.</summary>
    public const int WithNotesMost = 95;

    public static PictureBand Band(int score) => score >= CaptureQualities.Good ? PictureBand.Good : score >= CaptureQualities.Usable ? PictureBand.Usable : PictureBand.Retake;

    /// <summary>
    /// Each scoring bull's paper level, numbered from 1 in the sheet's order: the median of eight samples a fifth of the outer disc's
    /// radius beyond it, each the brightest pixel within two of the sample, so a line, a hole or a mark is not taken for shadow.
    /// </summary>
    public static IReadOnlyList<(int Number, PointD Centre, double Radius, double Level)> BullPaper(GrayImage image, Homography pageToImage, TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(pageToImage);
        ArgumentNullException.ThrowIfNull(definition);
        var levels = new List<(int, PointD, double, double)>();
        int number = 0;
        foreach (var bull in definition.Bulls.Where(b => b.Scoring))
        {
            number++;
            double outer = (definition.RingSets.FirstOrDefault(r => r.Key == bull.RingSet)?.Discs[0].Diameter ?? 200) / 2.0;
            var centre = pageToImage.Apply(new PointD(bull.X / 254.0, bull.Y / 254.0));
            var edge = pageToImage.Apply(new PointD((bull.X + outer) / 254.0, bull.Y / 254.0));
            double radius = Math.Sqrt(((edge.X - centre.X) * (edge.X - centre.X)) + ((edge.Y - centre.Y) * (edge.Y - centre.Y)));
            var samples = new List<int>();
            for (int k = 0; k < 8; k++)
            {
                double a = k * Math.PI / 4;
                var p = pageToImage.Apply(new PointD((bull.X + (1.2 * outer * Math.Cos(a))) / 254.0, (bull.Y + (1.2 * outer * Math.Sin(a))) / 254.0));
                int x = (int)Math.Round(p.X), y = (int)Math.Round(p.Y);
                if (x < 2 || y < 2 || x >= image.Width - 2 || y >= image.Height - 2)
                {
                    continue;
                }

                int brightest = 0;
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        brightest = Math.Max(brightest, image[x + dx, y + dy]);
                    }
                }

                samples.Add(brightest);
            }

            if (samples.Count >= 4)
            {
                samples.Sort();
                levels.Add((number, centre, radius, samples[samples.Count / 2]));
            }
        }

        return levels;
    }

    /// <summary>The dimmest bull's paper against the brightest, or null with fewer than two bulls measured.</summary>
    public static double? Evenness(IReadOnlyList<(int Number, PointD Centre, double Radius, double Level)> bulls)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        return bulls.Count < 2 || bulls.Max(b => b.Level) <= 0 ? null : bulls.Min(b => b.Level) / bulls.Max(b => b.Level);
    }

    /// <summary>
    /// The check of an analysed picture: <paramref name="definition"/> is what the codes named or the person chose, null where neither;
    /// <paramref name="result"/> the analysis, null where it did not run; <paramref name="codesRead"/> the square codes read.
    /// </summary>
    /// <summary>The registration's error that costs nothing, a flat sheet's, and the one at which the part is worthless, in inches.</summary>
    public const double RegistrationFine = 0.005;

    public const double RegistrationUseless = 0.05;

    /// <summary>
    /// Entry 328 section 2: the registration's error from which it earns a note, where on its own it would hold the score to a noted
    /// picture's 95 or below, 0.00725 in. Nearer a flat sheet's it still counts in the score but costs less than a note would; Unholy read
    /// "agree only to 0.005 in, where a flat sheet gives 0.005" as nonsense.
    /// </summary>
    public const double RegistrationNoted = RegistrationFine + ((100 - WithNotesMost) / 100.0 * (RegistrationUseless - RegistrationFine));

    /// <summary>The note on how well the markers agree, in a shooter's words, or null where it would not be worth a note.</summary>
    public static string? RegistrationNote(double? registrationInches) => registrationInches switch
    {
        null or < RegistrationNoted => null,
        < (RegistrationFine + RegistrationUseless) / 2 => "The sheet looks slightly curled. GroupLab allowed for it; flattening the sheet would measure a little better.",
        _ => "The sheet looks curled or folded. GroupLab allowed for it; flattening the sheet would measure better.",
    };

    public static PictureVerdict Of(GrayImage image, TargetDefinition? definition, AutomaticResult? result, int codesRead, bool torch)
    {
        ArgumentNullException.ThrowIfNull(image);
        var notes = new List<(string Words, IReadOnlyList<PointD>? Outline)>();
        var fine = new List<string>();
        string? cannot = null;
        var quality = result?.Capture?.Quality;
        int codesExpected = definition?.Codes?.Positions.Count ?? 0;

        if (definition is null)
        {
            cannot = "GroupLab could not read the square codes that name the sheet. Take it again with the whole sheet in view, or choose which sheet it is.";
        }
        else if (result is null || result.Scale is null || result.Failure is not null)
        {
            cannot = "GroupLab could not match the sheet's markers" + (result?.Failure is { } why ? ": " + why.TrimEnd('.') : "") + ". Take it again with the whole sheet in view.";
        }
        else if (quality is not null && OffAxisLimit.Refusal(quality.OffAxisDegrees) is { } refused)
        {
            cannot = refused;
        }
        else if (quality is { FocusPart: 0 })
        {
            cannot = "Too blurred to find the holes: hold the phone steady, or tap the sheet to focus, and take it again.";
        }
        else if (quality is { ResolutionPart: 0 })
        {
            cannot = "The bulls are too small to measure: move closer and take it again.";
        }

        IReadOnlyList<PointD>? sheet = null;
        double? evenness = null;
        if (definition is not null && result?.Measurement.Registration is { } registration)
        {
            var pageToImage = CaptureRecord.PageToImage(registration.Mapping, definition.Page.Width, definition.Page.Height);
            double w = definition.Page.Width / 254.0, h = definition.Page.Height / 254.0;
            sheet = [.. new PointD[] { new(0, 0), new(w, 0), new(w, h), new(0, h) }.Select(pageToImage.Apply)];
            var bulls = BullPaper(image, pageToImage, definition);
            evenness = Evenness(bulls);
            if (bulls.Count >= 2)
            {
                double median = bulls.Select(b => b.Level).Order().ElementAt(bulls.Count / 2);
                var dark = bulls.Where(b => b.Level < Shadowed * median).ToList();
                if (dark.Count > 0)
                {
                    notes.Add(($"{(dark.Count == 1 ? "A shadow falls on bull" : "A shadow falls across bulls")} {Numbers(dark.Select(b => b.Number))}, evened out: check {(dark.Count == 1 ? "its hole" : $"those {dark.Count} holes")} if you like.",
                        Box(dark.Select(b => (b.Centre, b.Radius)))));
                }
            }
        }

        if (quality is not null && cannot is null)
        {
            if (quality.OffAxisDegrees > CaptureQualities.FineDegrees)
            {
                notes.Add((string.Create(CultureInfo.InvariantCulture, $"Hold the phone square to the sheet next time: the picture was {quality.OffAxisDegrees:0} degrees off square, and GroupLab corrected for it."), sheet));
            }

            if (quality.ResolutionPart < 1)
            {
                // Entry 282 section 6: in a shooter's words; "137 pixels an inch" meant nothing to one.
                notes.Add(("Move a little closer next time, so the sheet fills more of the picture: GroupLab measures best with a little more detail.", sheet));
            }

            if (quality.FocusPart is < 1)
            {
                notes.Add(("The picture is a little soft: hold the phone steady, or tap the sheet to focus, next time.", sheet));
            }

            if (quality.ExposurePart is < 1)
            {
                notes.Add((quality.ClippedShare is { } clipped && clipped > CaptureQualities.FineClipped
                    ? "Part of the paper is washed out by glare or bright light; tilt away from the light next time."
                    : "The paper is dim; GroupLab evened it out. More light, or the torch, would help next time.", sheet));
            }

            // Entry 282 section 6: only where the missing markers cost the picture something; the "Fine" line always says how many were read.
            if (quality.MarkingsRead is { } read && quality.MarkingsExpected is { } expected && read < expected && quality.MarkingsPart is < 1)
            {
                notes.Add((string.Create(CultureInfo.InvariantCulture, $"{read} of {expected} markers read; GroupLab measured from those it read."),
                    result?.MissingMarkers is { Count: > 0 } missing ? Box(missing.Select(m => (m, 12.0))) : null));
            }
        }

        // Entry 260: the registration's own error is part of the score, the fit's residual or, through a bent sheet's mesh, each marker as
        // the others predict it. On the Phase 0 photographs it was what separated the pictures that measured badly from those that did not.
        double? registrationInches = result?.Measurement.Registration?.RmsResidual / 254;
        double registrationPart = registrationInches is { } rms ? Math.Clamp((RegistrationUseless - rms) / (RegistrationUseless - RegistrationFine), 0, 1) : 1;
        if (RegistrationNote(registrationInches) is { } curled && cannot is null)
        {
            notes.Add((curled, sheet));
        }

        if (quality?.FocusPart is >= 0.9)
        {
            fine.Add("sharp");
        }

        if (quality?.ExposurePart is >= 0.9 && evenness is null or >= EvenLight)
        {
            fine.Add("light even");
        }

        if (quality?.MarkingsRead is { } r && quality.MarkingsExpected is { } e)
        {
            fine.Add(string.Create(CultureInfo.InvariantCulture, $"{r} of {e} tags read"));
        }

        if (codesExpected > 0)
        {
            fine.Add(string.Create(CultureInfo.InvariantCulture, $"{Math.Min(codesRead, codesExpected)} of {codesExpected} codes read"));
        }

        fine.Add(torch ? "torch on" : "no torch");

        double evenPart = evenness is { } v ? Math.Clamp((v - UnevenLight) / (EvenLight - UnevenLight), 0, 1) : 1;
        int raw = quality is null ? 0 : (int)Math.Round(Math.Min(Math.Min(quality.Score, 100 * evenPart), 100 * registrationPart), MidpointRounding.AwayFromZero);
        bool can = cannot is null;
        int score = can ? Math.Max(raw, CaptureQualities.Usable) : Math.Min(raw, CaptureQualities.Usable - 1);

        // Entry 282 section 6: "Good with notes" and 100 said two things. A picture with something to note is not a perfect one.
        if (can && notes.Count > 0)
        {
            score = Math.Min(score, WithNotesMost);
        }
        var band = Band(score);
        if (!can)
        {
            notes.Insert(0, (cannot!, sheet));
        }

        var numbered = notes.Select((n, i) => new PictureNote(i + 1, n.Words, n.Outline)).ToList();
        string verdict = !can ? "Retake" : numbered.Count == 0 && band == PictureBand.Good ? "Good" : "Good with notes";
        string lead = !can ? "Take it again: GroupLab cannot measure this picture."
            : band == PictureBand.Good ? numbered.Count == 0 ? "Good." : "Good. A few notes:"
            : "Good enough to measure. Here is what would make the next one better:";
        return new PictureVerdict(score, band, verdict, lead, numbered, fine, can);
    }

    /// <summary>
    /// The capture screen's forecast from one frame's quality: its score, placed in the band the picture would get, since a frame the
    /// markers registered is one GroupLab can measure unless it is past the angle limit, or too blurred or too small to find the holes.
    /// </summary>
    public static int Forecast(CaptureQuality quality)
    {
        ArgumentNullException.ThrowIfNull(quality);
        bool can = OffAxisLimit.Refusal(quality.OffAxisDegrees) is null && quality.FocusPart is not 0 && quality.ResolutionPart > 0;
        return can ? Math.Max(quality.Score, CaptureQualities.Usable) : Math.Min(quality.Score, CaptureQualities.Usable - 1);
    }

    /// <summary>Bull numbers as a person reads them: "11 to 15", "3, 7 and 9".</summary>
    public static string Numbers(IEnumerable<int> numbers)
    {
        var sorted = numbers.Distinct().Order().ToList();
        var runs = new List<string>();
        for (int i = 0; i < sorted.Count;)
        {
            int j = i;
            while (j + 1 < sorted.Count && sorted[j + 1] == sorted[j] + 1)
            {
                j++;
            }

            runs.Add(j - i >= 2 ? string.Create(CultureInfo.InvariantCulture, $"{sorted[i]} to {sorted[j]}") : string.Join(", ", sorted.Skip(i).Take(j - i + 1)));
            i = j + 1;
        }

        return runs.Count == 1 ? runs[0] : string.Join(", ", runs.Take(runs.Count - 1)) + " and " + runs[^1];
    }

    private static IReadOnlyList<PointD> Box(IEnumerable<(PointD Centre, double Radius)> parts)
    {
        var list = parts.ToList();
        double left = list.Min(p => p.Centre.X - p.Radius), right = list.Max(p => p.Centre.X + p.Radius);
        double top = list.Min(p => p.Centre.Y - p.Radius), bottom = list.Max(p => p.Centre.Y + p.Radius);
        return [new(left, top), new(right, top), new(right, bottom), new(left, bottom)];
    }
}
