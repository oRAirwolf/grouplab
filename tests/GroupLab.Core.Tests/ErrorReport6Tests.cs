using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// Error report issue 6: a phone ends GroupLab in the background without notice, so each phone holds the run's marker only while GroupLab
/// is on screen. Android has done it since nightly 114; the iPhone build never did, and from nightly 226 its routine background ends were
/// reported as closes. The shared behaviour is held in Entry192Tests; this holds each phone to calling it on leaving and coming back.
/// </summary>
public class ErrorReport6Tests
{
    [Theory]
    [InlineData("android/GroupLab.Android/MainActivity.cs", "OnStop", "OnStart")]
    [InlineData("ios/GroupLab.iOS/App.cs", "ObserveDidEnterBackground", "ObserveWillEnterForeground")]
    public void EachPhoneLetsGoOfItsMarkerOffScreen(string file, string leaving, string returning)
    {
        string source = File.ReadAllText(Path.Combine(Repo.Root, file));
        int left = source.IndexOf(leaving, StringComparison.Ordinal);
        int back = source.IndexOf(returning, StringComparison.Ordinal);
        Assert.True(left >= 0 && back >= 0, $"{file} no longer hears {leaving} and {returning}");
        Assert.Contains("CrashReporter.EndRun(", source.Substring(left, Math.Min(800, source.Length - left)), StringComparison.Ordinal);
        Assert.Contains("CrashReporter.ResumeRun(", source.Substring(back, Math.Min(800, source.Length - back)), StringComparison.Ordinal);
    }
}
