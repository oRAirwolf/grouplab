using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using GroupLab.Core.Gltd.Model;
using OpenCvSharp;

[assembly: AvaloniaTestApplication(typeof(GroupLab.Mobile.Tests.TestApplication))]

// The shared screens keep process-wide state, as the application does: Phone.Platform, the settings store and the log.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace GroupLab.Mobile.Tests;

/// <summary>The phone application's styles on the headless platform, as a head would start it.</summary>
public sealed class TestApplication : Avalonia.Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApplication>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });

    public override void Initialize() => Phone.Initialize(this);
}

/// <summary>
/// A phone that is not Android or iOS: folders in the test run's own temporary folder, the sheets copied from the repository, a decode by
/// OpenCV, and a record of what the screens asked to share or print. Nothing here reaches outside the process.
/// </summary>
internal sealed class TestPhone : IPhonePlatform
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "grouplab-mobile-" + Guid.NewGuid().ToString("N"));

    public List<(string What, string Name)> Asked { get; } = [];

    public string FilesFolder { get; } = Directory.CreateDirectory(Path.Combine(Root, "files")).FullName;

    public string CacheFolder { get; } = Directory.CreateDirectory(Path.Combine(Root, "cache")).FullName;

    public string DeviceWords => "GroupLab on a test phone";

    public GroupLab.Core.Survey.MachineFacts Machine() => GroupLab.Core.Survey.SurveyReport.ThisMachine(device: "test phone");

    public void CopyBundledTargets(string into)
    {
        foreach (string assets in (string[])["targets", "targets/frozen"])
        {
            string from = Repo.PathTo(assets.Split('/'));
            string folder = Directory.CreateDirectory(Path.Combine(into, assets)).FullName;
            foreach (string file in Directory.EnumerateFiles(from, "*.gltd.json"))
            {
                File.Copy(file, Path.Combine(folder, Path.GetFileName(file)), overwrite: true);
            }
        }
    }

    public (Mat Colour, int Width, int Height, int Sample)? DecodeReduced(string photo, double mostMegapixels)
    {
        using var whole = Cv2.ImRead(photo, ImreadModes.Color);
        if (whole.Empty())
        {
            return null;
        }

        int sample = GroupLab.Core.Imaging.WorkingSize.SampleFor(whole.Width, whole.Height, mostMegapixels);
        var reduced = new Mat();
        Cv2.Resize(whole, reduced, new OpenCvSharp.Size(whole.Width / sample, whole.Height / sample), 0, 0, InterpolationFlags.Area);
        return (reduced, whole.Width, whole.Height, sample);
    }

    public double MemoryBudgetMegabytes() => 2048;

    public bool CameraAllowed() => false;

    /// <summary>Whether the test has allowed the camera since Capture asked for it (entry 312 section 6).</summary>
    public bool Granted { get; set; }

    public bool CameraGranted() => Granted;

    /// <summary>A web address a screen asked to open, recorded and never opened.</summary>
    public void OpenAddress(string address) => Asked.Add(("open", address));

    public Control Camera(Action<string, bool> taken, Action back, Action choose, Action? result = null) => new TextBlock { Text = "camera" };

    public bool IsCamera(object? content) => content is TextBlock { Text: "camera" };

    public bool CameraOpen => false;

    public string? ShareFile(string path, string mimeType, string title)
    {
        Asked.Add(("share", Path.GetFileName(path)));
        return null;
    }

    public string? PrintPdf(byte[] pdf, string name, PageSize paper)
    {
        Asked.Add(("print", name));
        return null;
    }

    public string? SharePdf(byte[] pdf, string name)
    {
        Asked.Add(("share pdf", name));
        return null;
    }

    public Task<string?> PastePicture(string folder) => Task.FromResult<string?>(null);

    public Control? UpdateCard() => null;

    public bool IsDevBuild { get; set; }

    /// <summary>Entry 347's three conditions, unknown until a test sets them, as on a head that cannot tell.</summary>
    public bool? Unmetered { get; set; }

    public bool? BatteryLow { get; set; }

    public bool? StorageLow { get; set; }

    /// <summary>What the build offers of the keep-pictures switch; null follows the development build, as Android does.</summary>
    public (bool Offered, bool OnByDefault)? Keeps { get; set; }

    public (bool Offered, bool OnByDefault) KeepsSittings => Keeps ?? (IsDevBuild, IsDevBuild);
}

/// <summary>The repository's root, found from the test's own folder.</summary>
internal static class Repo
{
    public static string Root { get; } = Find();

    public static string PathTo(params string[] parts) => Path.Combine([Root, .. parts]);

    private static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "GroupLab.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("the repository root was not found above " + AppContext.BaseDirectory);
    }
}

/// <summary>Before anything else: the log goes into the run's own folder, and nothing can reach the network.</summary>
internal static class TestDefaults
{
    [ModuleInitializer]
    internal static void Initialise()
    {
        Environment.SetEnvironmentVariable(GroupLab.App.Diagnostics.LogDirectory.Override, Path.Combine(Path.GetTempPath(), "grouplab-mobile-logs"));
        GroupLab.Core.Updates.TheOutsideWorld.Current = new GroupLab.Core.Updates.RecordedOutsideWorld();
    }
}
