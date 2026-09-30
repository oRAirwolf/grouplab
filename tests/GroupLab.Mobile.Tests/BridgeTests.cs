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
/// without this run's key, and answers every scenario step as a command, with the controls showing and the log inline.
/// </summary>
public class BridgeTests
{
    [AvaloniaFact]
    public async Task TheBridgeNeedsItsKeyAndDrivesTheScreensByName()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        string key = Bridge.Start(port: 0)!;
        try
        {
            Assert.Equal(32, key.Length);
            Assert.Equal(key, File.ReadAllText(Path.Combine(Phone.Platform!.FilesFolder, "bridge", "key")));

            // Without the key: refused, whatever is asked.
            var refused = await Bridge.Answer("""{ "key": "wrong", "do": "ping" }""");
            Assert.True(refused["refused"]!.GetValue<bool>());
            Assert.False(refused["ok"]!.GetValue<bool>());

            string Ask(string what) => $$"""{ "key": "{{key}}", {{what}} }""";
            Assert.True((await Bridge.Answer(Ask("\"do\": \"ping\"")))["ok"]!.GetValue<bool>());
            Assert.True((await Bridge.Answer(Ask("\"do\": \"open\", \"place\": \"settings\"")))["ok"]!.GetValue<bool>());
            var tree = await Bridge.Answer(Ask("\"do\": \"tree\""));
            Assert.Contains(tree["elements"]!.AsArray(), e => e!["text"]?.GetValue<string>() == "Send diagnostics");
            var shot = await Bridge.Answer(Ask("\"do\": \"screenshot\", \"name\": \"bridge\""));
            Assert.True(Convert.FromBase64String(shot["png"]!.GetValue<string>()).Length > 1000);
            Assert.False((await Bridge.Answer(Ask("\"do\": \"press\", \"name\": \"Nothing like this\"")))["ok"]!.GetValue<bool>());
            Assert.Contains("bridge.do", (await Bridge.Answer(Ask("\"do\": \"log\", \"lines\": 50")))["lines"]!.GetValue<string>(), StringComparison.Ordinal);

            // Over the socket, on the loopback address only, one line each way.
            int port = Bridge.Listening;
            var ping = await Task.Run(() =>
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
            Assert.True(JsonNode.Parse(ping.First)!["ok"]!.GetValue<bool>());
            Assert.True(JsonNode.Parse(ping.Second)!["refused"]!.GetValue<bool>());
            Assert.True(ping.Closed);
        }
        finally
        {
            Bridge.Stop();
            window.Close();
        }

        Assert.Null(Bridge.Key);
        Assert.Equal(0, Bridge.Listening);
        Assert.False(File.Exists(Path.Combine(Phone.Platform!.FilesFolder, "bridge", "key")));
    }
}
