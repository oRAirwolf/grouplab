using Avalonia.Controls;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Mobile;
using OpenCvSharp;

namespace GroupLab.iOS;

/// <summary>
/// The desktop standing in for the phone, for the iOS self-test's reference numbers: its own work folder, the repository's sheets, and the
/// same reduced OpenCV decode the iOS head makes, with the size read from the file's header. Nothing here is ever shown; the screens are
/// not opened.
/// </summary>
internal sealed class DesktopPhone(string work, string repository, double budget) : IPhonePlatform
{
    public string FilesFolder => work;

    public string CacheFolder { get; } = Directory.CreateDirectory(Path.Combine(work, "cache")).FullName;

    public string DeviceWords => "GroupLab self-test reference";

    public GroupLab.Core.Survey.MachineFacts Machine() => GroupLab.Core.Survey.SurveyReport.ThisMachine();

    /// <summary>The sheets as the iOS bundle carries them: targets/*.gltd.json, and every frozen one flat in targets/frozen.</summary>
    public void CopyBundledTargets(string into)
    {
        string targets = Path.Combine(repository, "targets");
        foreach (var (from, folder, option) in (ReadOnlySpan<(string, string, SearchOption)>)[
            (targets, "targets", SearchOption.TopDirectoryOnly), (Path.Combine(targets, "frozen"), Path.Combine("targets", "frozen"), SearchOption.AllDirectories)])
        {
            string to = Directory.CreateDirectory(Path.Combine(into, folder)).FullName;
            foreach (string file in Directory.EnumerateFiles(from, "*.gltd.json", option))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
            }
        }
    }

    public (Mat Colour, int Width, int Height, int Sample)? DecodeReduced(string photo, double mostMegapixels)
    {
        var header = ImageMetadataReader.Read(File.ReadAllBytes(photo));
        int width = header.Width ?? 0, height = header.Height ?? 0;
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        int sample = Math.Min(8, WorkingSize.SampleFor(width, height, mostMegapixels));
        var mode = sample switch
        {
            8 => ImreadModes.ReducedColor8,
            4 => ImreadModes.ReducedColor4,
            2 => ImreadModes.ReducedColor2,
            _ => ImreadModes.Color,
        };
        var colour = Cv2.ImRead(photo, mode | ImreadModes.IgnoreOrientation);
        if (colour.Empty())
        {
            colour.Dispose();
            return null;
        }

        return (colour, width, height, sample);
    }

    public double MemoryBudgetMegabytes() => budget;

    public bool CameraAllowed() => false;

    public Control Camera(Action<string, bool> taken, Action back, Action choose, Action? result = null) => throw new NotSupportedException();

    public bool IsCamera(object? content) => false;

    public bool CameraOpen => false;

    public string? ShareFile(string path, string mimeType, string title) => "not on the desktop reference";

    public string? PrintPdf(byte[] pdf, string name, PageSize paper) => "not on the desktop reference";

    public string? SharePdf(byte[] pdf, string name) => "not on the desktop reference";

    public Task<string?> PastePicture(string folder) => Task.FromResult<string?>(null);

    public Control? UpdateCard() => null;

    public bool IsDevBuild => false;
}
