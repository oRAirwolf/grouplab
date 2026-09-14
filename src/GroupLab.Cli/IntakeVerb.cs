using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Publication;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab intake &lt;submission&gt; &lt;public directory&gt; [--accept &lt;file&gt;]...</c>: NOTES-FROM-PLANNING.md entry 22 section 2,
/// the single way a donated photograph enters public test data, with entry 27 section 1's triage. The triage is the cheapest check the
/// application already has: a photograph on which fewer than four GroupLab markers decode can be neither registered nor scaled, so it is
/// held with that reason until a person who has looked at it accepts it by name. Entry 27 section 3's facts, the stored size and aspect
/// and the lens grouping key with digital zoom, are recorded beside the verdict.
/// </summary>
internal static class IntakeVerb
{
    /// <summary>Markers registration needs, DETECTION-PIPELINE.md stage S3.</summary>
    private const int MarkersForRegistration = 4;

    public static int Run(string submission, string publicRoot, string[] rest, TextWriter output)
    {
        var accepted = new List<string>();
        for (int i = 0; i < rest.Length; i++)
        {
            if (rest[i] == "--accept" && i + 1 < rest.Length)
            {
                accepted.Add(rest[++i]);
            }
            else
            {
                output.WriteLine($"intake: unknown option {rest[i]}");
                return 2;
            }
        }

        var backend = new OpenCvSharpBackend();
        var result = Intake.Run(submission, publicRoot, (name, _) => Triage(Path.Combine(submission, name), backend), accepted);
        if (result.Refused is { } reason)
        {
            output.WriteLine($"{result.Submission}: refused, {reason}. Nothing was written.");
            return 1;
        }

        output.WriteLine($"{result.Submission}: published to {result.PublishedDirectory}");
        foreach (var file in result.Files)
        {
            output.WriteLine(file.Held is null
                ? $"  {file.Name}: published, received {file.ReceivedSha256[..12]}, published {file.PublishedSha256![..12]}; removed {(file.Removed.Count == 0 ? "nothing" : string.Join(", ", file.Removed))}"
                : $"  {file.Name}: {file.Held}");
            foreach (string finding in file.Triage)
            {
                output.WriteLine($"      {finding}");
            }
        }

        return 0;
    }

    /// <summary>Whether the photograph could be measured at all, and what it is: markers decoded at three guesses of their size, its geometry and its lens key.</summary>
    private static TriageVerdict Triage(string path, IImagingBackend backend)
    {
        var findings = new List<string>();
        GrayImage image;
        ImageMetadata metadata;
        try
        {
            (image, metadata) = ImageLoader.Load(path);
        }
        catch (Exception ex) when (ex is IOException or OpenCvSharp.OpenCVException or InvalidOperationException)
        {
            return new TriageVerdict(false, ["the image cannot be decoded: " + ex.Message]);
        }

        int markers = new[] { 20.0, 40.0, 80.0 }
            .Select(fraction => backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, Math.Max(image.Width, image.Height) / fraction)).Markers.Select(m => m.Id).Distinct().Count())
            .Max();
        findings.Add(markers >= MarkersForRegistration
            ? string.Create(CultureInfo.InvariantCulture, $"{markers} GroupLab markers decoded, enough to register")
            : string.Create(CultureInfo.InvariantCulture, $"{markers} GroupLab markers decoded, fewer than the {MarkersForRegistration} registration needs, so the automatic path can neither register nor scale it"));
        double aspect = (double)Math.Max(image.Width, image.Height) / Math.Min(image.Width, image.Height);
        findings.Add(string.Create(CultureInfo.InvariantCulture, $"{image.Width} by {image.Height} px, aspect {aspect:0.00}:1"));
        if (metadata.IsCamera)
        {
            findings.Add("lens group " + metadata.LensGroupKey);
        }

        findings.AddRange(PublicationCheck.LocationProblems(File.ReadAllBytes(path)).Select(p => "as received: " + p));
        return new TriageVerdict(markers >= MarkersForRegistration, findings);
    }
}
