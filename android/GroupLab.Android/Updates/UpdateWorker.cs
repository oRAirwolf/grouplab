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
        SelfUpdate.RunAsync(asked: false, fromWorker: true).GetAwaiter().GetResult();
        return Result.InvokeSuccess()!;
    }
}
