// The application tests run one at a time. The diagnostics they exercise are process-wide by design, as they are in the application,
// which has one user: DiagnosticLog.Current, DiagnosticLog.LastAction and CrashReporter.InFlight. Run in parallel, the report upload
// tests logged report.send through the shared log while the crash test was between its last action and its crash, and the crash record
// named the wrong action on one CI platform or another (NOTES-FROM-PLANNING.md entry 41 section 5). The Avalonia tests were already
// serialised on the UI thread, so this costs little.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
