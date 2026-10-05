using Android.Content;
using AndroidX.Work;

using GroupLab.Mobile;

namespace GroupLab.Android.Updates;

/// <summary>
/// The check about every six hours, NOTES-FROM-PLANNING.md entry 288, run by WorkManager only on an unmetered network. It checks, downloads
/// and verifies, and installs only where GroupLab is not on screen and Android will do it without a tap.
/// </summary>
public sealed class UpdateWorker(Context context, WorkerParameters parameters) : Worker(context, parameters)
{
    public override Result DoWork()
    {
        // Entry 374 section 3: where the update page could not be reached, WorkManager tries again within minutes, not six hours.
        return SelfUpdate.RunAsync(asked: false, fromWorker: true).GetAwaiter().GetResult() ? Result.InvokeSuccess()! : Result.InvokeRetry()!;
    }
}
