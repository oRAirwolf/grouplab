using GroupLab.iOS;
using GroupLab.Mobile;

// NOTES-FROM-PLANNING.md entry 290 section 2 item 4: the desktop runs the iOS self-test's imaging and pipeline checks, from the same code,
// on the same sheets and the same sample, and writes the numbers in the same form, for .github/workflows/ios-app.yml to compare.
// Usage: <work folder> <repository> <budget in MB> <results.json>
if (args.Length != 4 || !double.TryParse(args[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double budget))
{
    Console.Error.WriteLine("usage: GroupLab.iOS.SelfTestReference <work folder> <repository> <budget in MB> <results.json>");
    return 2;
}

string work = Path.GetFullPath(args[0]);
string repository = Path.GetFullPath(args[1]);
Directory.CreateDirectory(work);
Environment.SetEnvironmentVariable(GroupLab.App.Diagnostics.LogDirectory.Override, Path.Combine(work, "logs"));
var platform = new DesktopPhone(work, repository, budget);
Phone.Start(platform, new Avalonia.Application(), () => "US", null);

var checks = SelfTestChecks.Imaging();
checks.Add(SelfTestChecks.Pipeline(Path.Combine(repository, "samples", "gl-cf25-ltr-d-25-shots-600-dpi.png")));
File.WriteAllText(args[3], SelfTestChecks.Json("desktop " + System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier, true, budget, checks));
foreach (var check in checks)
{
    Console.WriteLine(SelfTestChecks.Line(check));
}

return checks.All(c => c.Passed || c.Skipped) ? 0 : 1;
