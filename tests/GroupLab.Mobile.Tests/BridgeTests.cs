using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 1: GroupLab Dev's automation bridge listens on the loopback address only, refuses a request
/// without this run's key or longer than its limit, and answers every scenario step as a command, with the controls showing and the log
/// inline.
/// </summary>
public class BridgeTests
{
    private static Window Open()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        var window = new Window { Width = 412, Height = 915, Content = new Shell() };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static bool Ok(JsonObject answer) => answer["ok"]!.GetValue<bool>();

    [AvaloniaFact]
    public async Task TheBridgeNeedsItsKeyAndDrivesTheScreensByName()
    {
        var window = Open();
        string key = Bridge.Start(port: 0)!;
        try
        {
            Assert.Equal(32, key.Length);
            Assert.Equal(key, File.ReadAllText(Path.Combine(Phone.Platform!.FilesFolder, "bridge", "key")));

            // Without the key, or not JSON at all: refused, whatever is asked, and nothing done.
            foreach (string bad in new[] { """{ "key": "wrong", "do": "ping" }""", """{ "do": "ping" }""", "not json", """{ "key": 7, "do": "ping" }""", "[]" })
            {
                var refused = await Bridge.Answer(bad);
                Assert.True(refused["refused"]!.GetValue<bool>(), bad);
                Assert.False(Ok(refused));
            }

            string Ask(string what) => $$"""{ "key": "{{key}}", {{what}} }""";
            var ping = await Bridge.Answer(Ask("\"do\": \"ping\""));
            Assert.True(Ok(ping));
            Assert.Contains(ping["commands"]!.AsArray(), c => c!.GetValue<string>() == "reset");
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"open\", \"place\": \"settings\""))));
            Assert.False(Ok(await Bridge.Answer(Ask("\"do\": \"open\", \"place\": \"7\""))));
            var tree = await Bridge.Answer(Ask("\"do\": \"tree\""));
            Assert.Contains(tree["elements"]!.AsArray(), e => e!["id"]?.GetValue<string>() == "settings-send-diagnostics" && e["text"]?.GetValue<string>() == "Send diagnostics");
            var shot = await Bridge.Answer(Ask("\"do\": \"screenshot\", \"name\": \"bridge\""));
            Assert.True(Convert.FromBase64String(shot["png"]!.GetValue<string>()).Length > 1000);
            Assert.False(Ok(await Bridge.Answer(Ask("\"do\": \"press\", \"name\": \"Nothing like this\""))));
            Assert.False(Ok(await Bridge.Answer(Ask("\"do\": \"fly\""))));

            // A check box by its id, turned off and on, as a finger would; the page scrolled to it first.
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"scroll\", \"name\": \"settings-printer-correction\""))));
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"choose\", \"name\": \"settings-printer-correction\", \"on\": false"))));
            Assert.False(Phone.Settings.LoadPrinterCorrection());
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"choose\", \"name\": \"settings-printer-correction\""))));
            Assert.True(Phone.Settings.LoadPrinterCorrection());
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"scroll\", \"to\": \"bottom\""))));
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"scroll\", \"by\": -200"))));

            // A setting by its key, and the tabs by their ids.
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"setting\", \"name\": \"showWork\", \"value\": true"))));
            Assert.True(Phone.Settings.LoadShowWork());
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"setting\", \"name\": \"showWork\", \"value\": null"))));
            Assert.False(Phone.Settings.LoadShowWork());
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"press\", \"name\": \"tab-sessions\""))));
            Assert.Equal(Shell.Place.Sessions, Shell.Current!.Showing);
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"back\""))));
            Assert.Equal(Shell.Place.Capture, Shell.Current!.Showing);

            Assert.Contains("bridge.do", (await Bridge.Answer(Ask("\"do\": \"log\", \"lines\": 50")))["lines"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Contains("ms=", (await Bridge.Answer(Ask("\"do\": \"timings\", \"lines\": 20")))["lines"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.True((await Bridge.Answer(Ask("\"do\": \"memory\"")))["managedMb"]!.GetValue<double>() > 0);
        }
        finally
        {
            Bridge.Stop();
            window.Close();
        }

        Assert.Null(Bridge.Key);
        Assert.Equal(0, Bridge.Listening);
        Assert.False(File.Exists(Path.Combine(Phone.Platform!.FilesFolder, "bridge", "key")));
        Assert.True((await Bridge.Answer("""{ "key": "", "do": "ping" }"""))["refused"]!.GetValue<bool>());
    }

    [AvaloniaFact]
    public async Task OverTheSocketItIsOneLineEachWayOnTheLoopbackAndALongLineIsRefused()
    {
        var window = Open();
        string key = Bridge.Start(port: 0)!;
        try
        {
            int port = Bridge.Listening;
            string Ask(string what) => $$"""{ "key": "{{key}}", {{what}} }""";
            var said = await Task.Run(() =>
            {
                using var client = new TcpClient("127.0.0.1", port);
                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
                using var reader = new StreamReader(stream, Encoding.UTF8);
                writer.WriteLine(Ask("\"do\": \"memory\""));
                string first = reader.ReadLine()!;
                writer.WriteLine("""{ "key": "wrong", "do": "ping" }""");
                string second = reader.ReadLine()!;
                return (First: first, Second: second, Closed: reader.ReadLine() is null);
            });
            Assert.True(JsonNode.Parse(said.First)!["ok"]!.GetValue<bool>());
            Assert.True(JsonNode.Parse(said.Second)!["refused"]!.GetValue<bool>());
            Assert.True(said.Closed);

            // A line past the limit is not read to its end: refused, and the connection closed.
            var past = await Task.Run(() =>
            {
                using var client = new TcpClient("127.0.0.1", port);
                using var stream = client.GetStream();
                stream.Write(Encoding.UTF8.GetBytes(new string('x', Bridge.Longest + 10)));
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return (Answer: reader.ReadLine()!, Closed: reader.ReadLine() is null);
            });
            Assert.True(JsonNode.Parse(past.Answer)!["refused"]!.GetValue<bool>());
            Assert.True(past.Closed);
        }
        finally
        {
            Bridge.Stop();
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task AResetTakesAwayWhatUseLeftAndKeepsTheSittingsPictures()
    {
        var window = Open();
        string key = Bridge.Start(port: 0)!;
        string files = Phone.Platform.FilesFolder;
        try
        {
            Directory.CreateDirectory(Path.Combine(files, "own-sheets"));
            File.WriteAllText(Path.Combine(files, "own-sheets", "mine.json"), "{}");
            Directory.CreateDirectory(Path.Combine(files, "sitting"));
            File.WriteAllText(Path.Combine(files, "sitting", "kept.txt"), "kept");
            Phone.Settings.SaveShowWork(true);
            string Ask(string what) => $$"""{ "key": "{{key}}", {{what}} }""";

            // As a first run, its questions left to ask: the first run's page is what shows.
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"reset\", \"firstRun\": \"ask\""))));
            Assert.False(Directory.Exists(Path.Combine(files, "own-sheets")));
            Assert.True(File.Exists(Path.Combine(files, "sitting", "kept.txt")));
            Assert.False(Phone.Settings.LoadShowWork());
            Assert.IsType<FirstRunView>(Shell.Current!.Content);

            // And with them answered, as a script's run wants it: straight to Capture.
            Assert.True(Ok(await Bridge.Answer(Ask("\"do\": \"reset\""))));
            Assert.IsNotType<FirstRunView>(Shell.Current!.Content);
            Assert.Equal(Shell.Place.Capture, Shell.Current.Showing);
            Assert.True(File.Exists(Path.Combine(files, "bridge", "key")));
        }
        finally
        {
            Bridge.Stop();
            Scenario.AnswerFirstRun(Phone.Settings);
            File.Delete(Path.Combine(files, "sitting", "kept.txt"));
            window.Close();
        }
    }

    [Fact]
    public async Task ALineIsReadToItsEndOrRefusedPastTheLimit()
    {
        var (text, tooLong) = await Bridge.Line(new MemoryStream(Encoding.UTF8.GetBytes("{\"do\":\"ping\"}\r\nnext")), CancellationToken.None);
        Assert.Equal("{\"do\":\"ping\"}", text);
        Assert.False(tooLong);
        Assert.Equal((null, false), await Bridge.Line(new MemoryStream(), CancellationToken.None));
        Assert.Equal((null, true), await Bridge.Line(new MemoryStream(new byte[Bridge.Longest + 1]), CancellationToken.None));
        Assert.False((await Bridge.Line(new MemoryStream(Encoding.UTF8.GetBytes(new string('x', Bridge.Longest) + "\n")), CancellationToken.None)).TooLong);
    }

    [Fact]
    public void ItListensOnTheLoopbackOnlyAndIsInNoPublicBuild()
    {
        string source = File.ReadAllText(Repo.PathTo("mobile", "GroupLab.Mobile", "Dev", "Bridge.cs"));
        Assert.StartsWith("#if GROUPLAB_DEV", source, StringComparison.Ordinal);
        Assert.Contains("new TcpListener(IPAddress.Loopback, port)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IPAddress.Any", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IPv6Any", source, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.FixedTimeEquals", source, StringComparison.Ordinal);
        Assert.Null(typeof(Phone).Assembly.GetType("GroupLab.Mobile.Dev.Bridge"));
    }
}
