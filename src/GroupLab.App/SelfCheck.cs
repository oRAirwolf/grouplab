using System.Diagnostics;
using System.Text.Json.Nodes;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 501 section 3.3: <c>GroupLab.App --self-check</c>, the run that proves a build on a machine nobody sits at,
/// above all the Mac App Store copy in Apple's App Sandbox on GitHub's Mac machines. It starts as a person's run does, makes the demo's
/// sample sheet, opens it, waits for the sheet to be read, answers the caliber, accepts and analyzes it, and checks a session was saved.
/// In the sandbox it also checks the picture was kept in GroupLab's own folder and that a bookmark of a folder leads back to it. What it
/// found goes to <c>self-check.json</c> beside the settings (inside the sandbox's container there) and to the console, and the run ends with
/// 0 when every step held and 1 when one did not.
/// </summary>
internal static class SelfCheck
{
    internal static async Task<int> RunAsync(MainWindow window)
    {
        var steps = new JsonObject
        {
            ["sandboxed"] = AppInfo.Sandboxed,
            ["container"] = Environment.GetEnvironmentVariable("APP_SANDBOX_CONTAINER_ID") is { Length: > 0 },
            ["distribution"] = AppInfo.Distribution,
            ["version"] = AppInfo.Version,
        };
        bool ok = true;
        void Step(string name, bool held, string? detail = null)
        {
            steps[name] = detail is null ? held : $"{held}: {detail}";
            ok &= held;
        }

        try
        {
            int before = window.Sessions?.List().Count ?? -1;
            Step("database", before >= 0);
            string sample = window.WriteSampleForSelfCheck();
            window.OpenImage(sample);
            var clock = Stopwatch.StartNew();
            while (window.DetectionTask is { IsCompleted: false } && clock.Elapsed < TimeSpan.FromMinutes(3))
            {
                await Task.Delay(200);
            }

            Step("read", window.DetectionTask is { IsCompletedSuccessfully: true } && window.Session.State.Shots.Count > 0,
                $"{window.Session.State.Shots.Count} holes in {clock.Elapsed.TotalSeconds:0.0} s");
            if (AppInfo.Sandboxed)
            {
                Step("kept", window.Session.State.ImagePath is { } kept && kept.Contains("originals", StringComparison.Ordinal) && File.Exists(kept));
            }

            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            int after = window.Sessions?.List().Count ?? -1;
            Step("saved", after == before + 1, $"{before} sessions before, {after} after");

            if (AppInfo.Sandboxed)
            {
                string folder = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(window.SettingsPath) ?? Path.GetTempPath(), "self-check-folder")).FullName;
                string? bookmark = MacSandbox.Bookmark(folder);
                string? back = bookmark is null ? null : MacSandbox.Resolve(bookmark);
                Step("bookmark", back is not null && Path.GetFullPath(back).TrimEnd('/') == folder.TrimEnd('/'), back);
            }
        }
        catch (Exception e)
        {
            Step("exception", false, e.GetType().Name + ": " + e.Message);
        }

        steps["ok"] = ok;
        string text = steps.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        try
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(window.SettingsPath) ?? Path.GetTempPath(), "self-check.json"), text);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ok = false;
        }

        Console.WriteLine(text);
        DiagnosticLog.Info("app.selfcheck", ("ok", ok));
        return ok ? 0 : 1;
    }
}
