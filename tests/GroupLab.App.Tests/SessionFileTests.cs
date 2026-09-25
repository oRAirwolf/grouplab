using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A5: a session file from the phone opens on the desktop as a session of its own, with its marks,
/// its sheet and its picture, saved in Session records and saying which device wrote which revision; a file that is not one is refused with
/// the reason and changes nothing.
/// </summary>
public class SessionFileTests
{
    [AvaloniaFact]
    public void ASessionFileFromThePhoneOpensAsASessionOfItsOwn()
    {
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-sessionfile-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
            store.SaveUnits(UnitSettings.Imperial);
            var marking = new MarkingSession();
            marking.Open("target.jpg");
            marking.SetScale(new LengthReference(new PointD(0, 0), new PointD(1000, 0), 10));
            marking.AddShot(new PointD(400, 300));
            marking.AddShot(new PointD(420, 310));
            marking.AddShot(new PointD(405, 330));
            using var pixels = new Mat(600, 800, MatType.CV_8UC3, new Scalar(240, 240, 240));
            string file = Path.Combine(root, "from the phone" + SessionPackage.Extension);
            using (var to = File.Create(file))
            {
                SessionPackage.Write(to, marking.State, null, UnitSettings.Imperial, pixels.ImEncode(".jpg"), ".jpg", "samsung SM-F966U", 2, DateTime.UtcNow);
            }

            var window = new MainWindow(store) { Width = 1400, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.OpenSessionFile(file);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(3, window.Session.State.Shots.Count);
            Assert.NotNull(window.CurrentSession);
            Assert.True(File.Exists(window.Session.State.ImagePath));
            Assert.Contains("samsung SM-F966U, revision 2", window.StatusText, StringComparison.Ordinal);

            File.WriteAllText(Path.Combine(root, "not one" + SessionPackage.Extension), "hello");
            window.OpenSessionFile(Path.Combine(root, "not one" + SessionPackage.Extension));
            Assert.Equal(3, window.Session.State.Shots.Count);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }
}
