using System.Diagnostics;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// Question 43 (entry 388 section 3): Open, drop and paste decode the picture on a background thread, so a test that drops or pastes waits
/// for the open to finish, running the window's jobs meanwhile, before it looks at what opened.
/// </summary>
internal static class Opens
{
    /// <summary>Whether the open under way opened, once it has finished.</summary>
    public static bool OpenFinished(this MainWindow window)
    {
        var clock = Stopwatch.StartNew();
        while (!window.Opening.IsCompleted && clock.Elapsed < TimeSpan.FromSeconds(120))
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        Dispatcher.UIThread.RunJobs();
        Xunit.Assert.True(window.Opening.IsCompleted, "the open did not finish in two minutes");
        return window.Opening.Result;
    }
}
