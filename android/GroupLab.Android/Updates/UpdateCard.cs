using Avalonia.Controls;
using GroupLab.Core.Updates;

using GroupLab.Mobile;
using Screens = GroupLab.Mobile.Screens;

namespace GroupLab.Android.Updates;

/// <summary>
/// Settings, About, NOTES-FROM-PLANNING.md entry 288: the installed version, the newest on its train, "Update now", and the switch
/// "Install updates automatically", on by default in GroupLab Dev.
/// </summary>
internal static class UpdateCard
{
    internal static Control Build()
    {
        var column = new StackPanel { Spacing = 8 };
        column.Children.Add(Screens.Heading("Updates"));
        if (SelfUpdate.Off is { } off)
        {
            column.Children.Add(Screens.Dim(off.Words()));
            return column;
        }

        var build = SelfUpdate.Build;
        var installed = Screens.Line("Installed: " + Named(build.Version.Number));
        var newest = Screens.Line("");
        var status = Screens.Dim("");
        var permission = Screens.Dim("Android asks once for \"Install unknown apps\" before GroupLab can update itself; Update now opens that page.");
        void Refresh()
        {
            newest.Text = "Newest on the " + build.Train.Words().ToLowerInvariant() + " train: "
                + (SelfUpdate.Newest is { } n ? Named(n) + (SelfUpdate.CheckedUtc is { } at ? ", checked " + Ago(at) : "") : "not checked yet");
            status.Text = SelfUpdate.Status;
            status.IsVisible = status.Text.Length > 0;
            permission.IsVisible = !SelfUpdate.MayInstall;
        }

        var automatic = new ToggleSwitch
        {
            Content = "Install updates automatically", // one line on purpose: a switch's own label
            IsChecked = SelfUpdate.Automatic,
            OnContent = null,
            OffContent = null,
        };
        automatic.IsCheckedChanged += (_, _) => SelfUpdate.Automatic = automatic.IsChecked == true;
        var now = Screens.Primary("Update now", () =>
        {
            if (!SelfUpdate.MayInstall)
            {
                SelfUpdate.OpenUnknownAppsSetting();
                return;
            }

            if (SelfUpdate.Ready is not null)
            {
                _ = Task.Run(() => SelfUpdate.TryInstall(asked: true));
            }
            else
            {
                _ = SelfUpdate.RunAsync(asked: true);
            }
        });

        column.Children.Add(installed);
        column.Children.Add(newest);
        column.Children.Add(status);
        column.Children.Add(permission);
        column.Children.Add(now);
        column.Children.Add(automatic);
        column.Children.Add(Screens.Dim(SelfUpdate.Silent
            ? "Updates come from grouplab.org's signed list, download on Wi-Fi, and install when you leave GroupLab, never while the camera is open, a sheet is being read or a change is unsaved."
            : "Updates come from grouplab.org's signed list and download on Wi-Fi. Android asks you to confirm the first one; after that they install when you leave GroupLab, never in the middle of work."));
        Refresh();
        SelfUpdate.Changed += Refresh;
        column.DetachedFromVisualTree += (_, _) => SelfUpdate.Changed -= Refresh;
        return column;
    }

    /// <summary>"nightly 125" for a nightly, else the version as written.</summary>
    private static string Named(string version) =>
        SemanticVersion.Parse(version)?.PreRelease?.Split('.') is ["nightly", var n] ? "nightly " + n : version;

    private static string Ago(DateTimeOffset at)
    {
        var gap = DateTimeOffset.UtcNow - at;
        return gap.TotalMinutes < 1 ? "just now"
            : gap.TotalHours < 1 ? $"{(int)gap.TotalMinutes} minutes ago"
            : gap.TotalDays < 1 ? $"{(int)gap.TotalHours} hours ago"
            : $"{(int)gap.TotalDays} days ago";
    }
}
