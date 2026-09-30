#if GROUPLAB_DEV
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile.Dev;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 1: the automation bridge, in GroupLab Dev only (compiled out of the public application, as the
/// scenario files are, and <c>ScenarioTests</c> holds that the public library has none of its types). A small command server inside the
/// application that a developer's script reaches over the USB cable (<c>adb forward tcp:47315 tcp:47315</c> on Android,
/// <c>pymobiledevice3 usbmux forward 47315 47315</c> on iOS; <c>scripts/app-bridge.py</c> does either), so the application is driven by
/// name rather than by a finger or a screen position.
///
/// It listens on 127.0.0.1 only, so nothing on a network can reach it and iOS asks for no local network permission. Another application
/// on the same device can reach the port, so every request carries a key: 32 random hex digits made each time the bridge starts, shown in
/// Settings, About and written to <c>bridge/key</c> in the application's own files (Documents on iOS), which only the cable reaches. The key
/// is compared in constant time, a request without it is refused and its connection closed, a request is one line of at most
/// <see cref="Longest"/> bytes, at most <see cref="MostConnections"/> connections are open at once, one idle for
/// <see cref="Idle"/> is closed, and one command runs at a time. It starts with GroupLab Dev; a switch in Settings, About turns it off,
/// and the switch is remembered.
///
/// Each request is one line of JSON and each answer one line: <c>{"key": "...", "do": "press", "name": "result-fix-holes"}</c>. Every
/// scenario step (<see cref="Scenario"/>) is a command, with the same fields and the same code; besides them, <c>ping</c> answers the
/// version and the commands, <c>tree</c> the controls showing, <c>log</c> the newest lines, <c>timings</c> the newest lines that carry a
/// time, <c>memory</c> what is held, and <c>screenshot</c> the picture as base64 PNG as well as keeping it.
/// </summary>
internal static partial class Bridge
{
    /// <summary>The port on the device's own loopback address.</summary>
    internal const int Port = 47315;

    /// <summary>The longest request line, in bytes; a longer one is refused and its connection closed.</summary>
    internal const int Longest = 64 * 1024;

    /// <summary>The most connections open at once; another is closed as it arrives.</summary>
    internal const int MostConnections = 4;

    /// <summary>How long a connection may wait between requests before it is closed, so a script that went away frees its place.</summary>
    internal static TimeSpan Idle { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>The commands the bridge answers itself; every scenario step is one too.</summary>
    internal static readonly string[] Own = ["ping", "tree", "log", "timings", "memory"];

    /// <summary>The scenario steps, for <c>ping</c> to list.</summary>
    internal static readonly string[] Steps = ["open", "back", "picture", "read", "wait", "press", "type", "choose", "scroll", "setting", "reset", "screenshot", "tree", "sleep", "log", "replay", "record"];

    private static readonly Lock Gate = new();
    private static readonly SemaphoreSlim OneAtATime = new(1, 1);
    private static TcpListener? listener;
    private static CancellationTokenSource? stopping;
    private static int open;

    /// <summary>The key a request must carry; null while the bridge is off.</summary>
    internal static string? Key { get; private set; }

    /// <summary>The port it listens on while it runs; 0 while it is off.</summary>
    internal static int Listening
    {
        get
        {
            lock (Gate)
            {
                return listener?.LocalEndpoint is IPEndPoint point ? point.Port : 0;
            }
        }
    }

    private static string Folder => Path.Combine(Phone.Platform.FilesFolder, "bridge");

    private static string KeyFile => Path.Combine(Folder, "key");

    /// <summary>The file whose presence turns the bridge off, from the switch in Settings, About.</summary>
    private static string Off => Path.Combine(Folder, "off");

    /// <summary>Whether the switch in Settings, About is on: on in GroupLab Dev until turned off.</summary>
    internal static bool Wanted
    {
        get => !File.Exists(Off);
        set
        {
            Directory.CreateDirectory(Folder);
            if (value)
            {
                File.Delete(Off);
                Start();
            }
            else
            {
                File.WriteAllText(Off, "");
                Stop();
            }
        }
    }

    /// <summary>At the start of GroupLab Dev: the bridge, unless it was turned off.</summary>
    internal static void StartUnlessTurnedOff()
    {
        if (Wanted)
        {
            Start();
        }
    }

    /// <summary>Starts listening, on <paramref name="port"/> (0 for any free one, as a test does); the key it made, or null where it could not.</summary>
    internal static string? Start(int port = Port)
    {
        lock (Gate)
        {
            if (listener is not null)
            {
                return Key;
            }

            var made = new TcpListener(IPAddress.Loopback, port);
            try
            {
                made.Start();
            }
            catch (SocketException e)
            {
                made.Dispose();
                DiagnosticLog.Info("bridge.start", ("error", e.SocketErrorCode.ToString()));
                return null;
            }

            // The key is in place before the first connection is taken, so no request is answered without one.
            Key = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            Directory.CreateDirectory(Folder);
            File.WriteAllText(KeyFile, Key);
            listener = made;
            stopping = new CancellationTokenSource();
            var token = stopping.Token;
            _ = Task.Run(() => Accept(made, token));
            DiagnosticLog.Info("bridge.start", ("port", ((IPEndPoint)made.LocalEndpoint).Port));
            return Key;
        }
    }

    internal static void Stop()
    {
        lock (Gate)
        {
            stopping?.Cancel();
            stopping?.Dispose();
            stopping = null;
            listener?.Stop();
            listener?.Dispose();
            listener = null;
            Key = null;
            if (File.Exists(KeyFile))
            {
                File.Delete(KeyFile);
            }
        }

        DiagnosticLog.Info("bridge.stop");
    }

    private static async Task Accept(TcpListener serving, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await serving.AcceptTcpClientAsync(token);
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException or InvalidOperationException)
            {
                return;
            }

            if (Interlocked.Increment(ref open) > MostConnections)
            {
                Interlocked.Decrement(ref open);
                client.Dispose();
                continue;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Serve(client, token);
                }
                catch (Exception e) when (e is not OutOfMemoryException)
                {
                    // A connection's failure ends that connection only, and is logged rather than left for the crash handler.
                    DiagnosticLog.Exception(LogLevel.Warn, "bridge.connection", e);
                }
                finally
                {
                    Interlocked.Decrement(ref open);
                }
            }, CancellationToken.None);
        }
    }

    private static async Task Serve(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                using var network = client.GetStream();

                // Read through a buffer, a byte at a time for the line end; answers are written to the connection itself.
                using var stream = new BufferedStream(network);
                while (!token.IsCancellationRequested)
                {
                    (string? Text, bool TooLong) line;
                    using (var idle = CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        idle.CancelAfter(Idle);
                        line = await Line(stream, idle.Token);
                    }

                    if (line.Text is null && !line.TooLong)
                    {
                        return;
                    }

                    var answer = line.TooLong
                        ? new JsonObject { ["ok"] = false, ["refused"] = true, ["detail"] = $"a request is one line of at most {Longest} bytes" }
                        : await Answer(line.Text!);
                    await network.WriteAsync(Encoding.UTF8.GetBytes(answer.ToJsonString() + "\n"), token);
                    if (answer["refused"]?.GetValue<bool>() == true)
                    {
                        // Closed after the answer has gone: what the caller sent beyond it is read and dropped for a moment, since a
                        // connection closed with unread bytes is reset, and a reset can lose the answer on its way.
                        client.Client.Shutdown(SocketShutdown.Send);
                        using var drain = CancellationTokenSource.CreateLinkedTokenSource(token);
                        drain.CancelAfter(TimeSpan.FromSeconds(2));
                        var dropped = new byte[4096];
                        int read = 0;
                        while (read < Longest * 4 && await stream.ReadAsync(dropped, drain.Token) is var got and > 0)
                        {
                            read += got;
                        }

                        return;
                    }
                }
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or ObjectDisposedException or SocketException)
            {
                // The script went away, sat idle too long, or the bridge stopped; nothing to answer.
            }
        }
    }

    /// <summary>
    /// One line of a request, without its line end; no text where the connection ended first. A line longer than <see cref="Longest"/>
    /// bytes is not read further, and is said to be too long.
    /// </summary>
    internal static async Task<(string? Text, bool TooLong)> Line(Stream stream, CancellationToken token)
    {
        using var line = new MemoryStream();
        var one = new byte[1];
        while (await stream.ReadAsync(one, token) == 1)
        {
            if (one[0] == (byte)'\n')
            {
                return (Encoding.UTF8.GetString(line.GetBuffer(), 0, (int)line.Length).TrimEnd('\r'), false);
            }

            if (line.Length == Longest)
            {
                return (null, true);
            }

            line.WriteByte(one[0]);
        }

        return (null, false);
    }

    /// <summary>Whether <paramref name="given"/> is this run's key, compared in the same time whatever it is and however long.</summary>
    private static bool IsKey(string given) => Key is { } key
        && CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(given)), SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    /// <summary>One request's answer. A request that is not JSON, or does not carry the right key, is refused and the connection closed.</summary>
    internal static async Task<JsonObject> Answer(string line)
    {
        JsonObject? request = null;
        try
        {
            request = JsonNode.Parse(line) as JsonObject;
        }
        catch (JsonException)
        {
            // Refused below, the same as a request without the key, so nothing is said to a caller that has not shown it.
        }

        string given = request?["key"]?.GetValueKind() == JsonValueKind.String ? request["key"]!.GetValue<string>() : "";
        if (request is null || !IsKey(given))
        {
            DiagnosticLog.Info("bridge.refused");
            return new JsonObject { ["ok"] = false, ["refused"] = true, ["detail"] = "a request is one JSON object with this run's key; read it from Settings, About or bridge/key" };
        }

        string what = request["do"]?.GetValueKind() == JsonValueKind.String ? request["do"]!.GetValue<string>().ToLowerInvariant() : "";
        await OneAtATime.WaitAsync();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        JsonObject answer;
        try
        {
            Directory.CreateDirectory(Scenario.Results);
            answer = await Do(what, request);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            answer = new JsonObject { ["ok"] = false, ["detail"] = e.GetType().Name + ": " + e.Message };
        }
        finally
        {
            OneAtATime.Release();
        }

        answer["ms"] = clock.ElapsedMilliseconds;
        DiagnosticLog.Info("bridge.do", ("do", what), ("ok", answer["ok"]?.GetValue<bool>()), ("ms", clock.ElapsedMilliseconds));
        return answer;
    }

    private static async Task<JsonObject> Do(string what, JsonObject request)
    {
        int lines = request["lines"]?.GetValueKind() == JsonValueKind.Number && request["lines"]!.AsValue().TryGetValue(out int asked) ? Math.Clamp(asked, 1, 100_000) : 200;
        switch (what)
        {
            case "ping":
                return new JsonObject
                {
                    ["ok"] = true,
                    ["version"] = AppInfo.Version,
                    ["device"] = Phone.Platform.DeviceWords,
                    ["commands"] = new JsonArray([.. Own.Concat(Steps).Distinct().Select(c => (JsonNode)c)]),
                };
            case "tree":
                return new JsonObject { ["ok"] = true, ["elements"] = await Scenario.OnUi(Scenario.Elements) };
            case "log":
                string? text = Scenario.LogLines(lines);
                return new JsonObject { ["ok"] = text is not null, ["lines"] = text };
            case "timings":
                // Every line that carries a time taken (ms=, elapsedMs=, seconds=), newest last: the reading's stages, the steps, the commands.
                string? all = Scenario.LogLines(null);
                var timed = all?.Split('\n').Where(l => Timed().IsMatch(l)).TakeLast(lines);
                return new JsonObject { ["ok"] = all is not null, ["lines"] = timed is null ? null : string.Join('\n', timed) };
            case "memory":
                return new JsonObject
                {
                    ["ok"] = true,
                    ["managedMb"] = Math.Round(GC.GetTotalMemory(false) / 1048576.0, 1),
                    ["workingSetMb"] = Math.Round(Environment.WorkingSet / 1048576.0, 1),
                    ["budgetMb"] = Math.Round(Phone.Platform.MemoryBudgetMegabytes()),
                };
            default:
                var (ok, detail) = await Scenario.Do(new Scenario.Step(what, request));
                var answer = new JsonObject { ["ok"] = ok, ["detail"] = detail };
                string shot = Path.Combine(Scenario.Results, detail);
                if (what == "screenshot" && ok && File.Exists(shot))
                {
                    answer["png"] = Convert.ToBase64String(await File.ReadAllBytesAsync(shot));
                }

                return answer;
        }
    }

    /// <summary>What Settings, About says while the bridge runs, or that it is off.</summary>
    internal static string Words() => Key is { } key
        ? string.Create(CultureInfo.InvariantCulture, $"Listening on this device only, port {Listening}, for a developer's scripts over the cable. This run's key: {key}")
        : "Off. Nothing listens.";

    [GeneratedRegex(@"\s\w*(ms|Ms|seconds)=\d", RegexOptions.CultureInvariant)]
    private static partial Regex Timed();
}
#endif
