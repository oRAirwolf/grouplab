using Android.Content;
using Android.Content.PM;

using GroupLab.Mobile;

namespace GroupLab.Android.Updates;

/// <summary>
/// What the PackageInstaller session answered, NOTES-FROM-PLANNING.md entry 288. Where Android wants a tap, the first update of a copy
/// installed by adb or any update before Android 12, it hands back its own screen, which is shown; everything else is logged.
/// </summary>
[BroadcastReceiver(Exported = false)]
public sealed class InstallResultReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        int status = intent.GetIntExtra(PackageInstaller.ExtraStatus, -999);
        if (status == (int)PackageInstallStatus.PendingUserAction)
        {
            if (Confirm(intent) is { } confirm)
            {
                confirm.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(confirm);
            }

            SelfUpdate.Installed(status, "asked the person");
            return;
        }

        SelfUpdate.Installed(status, intent.GetStringExtra(PackageInstaller.ExtraStatusMessage));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422", Justification = "The typed call is used from Android 13; this is the older form for Android 10 to 12.")]
    private static Intent? Confirm(Intent intent)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return intent.GetParcelableExtra(Intent.ExtraIntent, Java.Lang.Class.FromType(typeof(Intent))) as Intent;
        }

#pragma warning disable CA1422
        return intent.GetParcelableExtra(Intent.ExtraIntent) as Intent;
#pragma warning restore CA1422
    }
}
