// The application tests run one at a time. The diagnostics they exercise are process-wide by design, as they are in the application,
// which has one user: DiagnosticLog.Current, DiagnosticLog.LastAction and CrashReporter.InFlight. Run in parallel, the report upload
// tests logged report.send through the shared log while the crash test was between its last action and its crash, and the crash record
// named the wrong action on one CI platform or another (NOTES-FROM-PLANNING.md entry 41 section 5). The Avalonia tests were already
// serialised on the UI thread, so this costs little.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace GroupLab.App.Tests
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 76 section 4: the window detects on opening an image, which in a test would load marks behind the test's
    /// back. Off unless a test turns it on for its own window.
    /// </summary>
    internal static class TestDefaults
    {
        /// <summary>
        /// Entry 122: nothing in a test run may open a browser, a viewer or a file manager on the machine it runs on. The control walk of
        /// entry 117 clicks every control it finds, and one of them opened tabs on Alan's machine. The recorder takes the real one's place
        /// for the whole run, and <c>OutsideWorldTests</c> reads what it was asked for.
        /// </summary>
        internal static readonly GroupLab.Core.Updates.RecordedOutsideWorld Outside = new();

        [ModuleInitializer]
        internal static void Initialise()
        {
            GroupLab.App.MainWindow.DetectOnOpenByDefault = false;

            // Entry 195: sending targets is switched on in limits.json. A test window does not ask its first run question or offer to send
            // a target unless the test says so; the tests of sending set ReceiverOpen themselves.
            GroupLab.App.MainWindow.ReceiverOpenByDefault = false;

            // Entry 200: error reports are switched on too. A test window does not ask about them or send one unless the test says so.
            GroupLab.App.MainWindow.ErrorsOpenByDefault = false;
            GroupLab.Core.Updates.TheOutsideWorld.Current = Outside;

            // Entry 123 section 2.6: an update downloads into a folder GroupLab owns, so a test run is pointed at one of its own rather than
            // at the folder the installed application uses on the machine the tests are running on.
            GroupLab.App.MainWindow.UpdateFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grouplab-test-updates");

            // And a window opened by a test does not go looking for an update of its own accord; the tests that care ask for one.
            GroupLab.App.MainWindow.CheckOnLaunchByDefault = false;
        }
    }
}
