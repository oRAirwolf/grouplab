#if GROUPLAB_DEV
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile.Dev;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 1: the automation bridge, in GroupLab Dev only (compiled out of the public application, as the
/// scenario files are). A small command server inside the application that a developer's script reaches over the USB cable
/// (<c>adb forward tcp:47315 tcp:47315</c> on Android, <c>pymobiledevice3 usbmux forward 47315 47315</c> on iOS), so the application is
/// driven by name rather than by a finger or a screen position. It listens on 127.0.0.1 only, so nothing on a network can reach it and iOS
/// asks for no local network permission. It needs a key, a random one made each time it starts, shown in Settings, About and written to
/// <c>bridge/key</c> in the application's files, where the same cable can read it. A switch in Settings, About turns it off.
///
/// Each request is one line of JSON and each answer one line: <c>{"key": "...", "do": "press", "name": "Fix holes"}</c>. Every scenario step
/// (<see cref="Scenario"/>) is a command, with the same fields; besides them, <c>ping</c> answers the version, <c>tree</c> answers the
/// controls showing, <c>log</c> answers the newest lines, <c>screenshot</c> answers the picture as base64 PNG as well as keeping it, and
/// <c>memory</c> answers what is held.
/// </summary>
internal static class Bridge
{
    /// <summary>The port on the device's own loopback address.</summary>
    internal const int Port = 47315;

    private static TcpListener? listener;
    private static CancellationTokenSource? stopping;

    /// <summary>The key a request must carry; null while the bridge is off.</summary>
    internal static string? Key { get; private set; }

    /// <summary>The port it listens on while it runs; 0 while it is off.</summary>
    internal static int Listening => listener?.LocalEndpoint is IPEndPoint point ? point.Port : 0;

    private static string Folder => Path.Combine(Phone.Platform.FilesFolder, "bridge");

    /// <summary>The file whose presence turns the bridge off, from the switch in Settings, About.</summary>
    private static string Off => Path.Combine(Folder, "off");

    /// <summary>Whether the switch in Settings, About is on: on until turned off.</summary>
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
        if (listener is not null)
        {
            return Key;
        }

        try
        {
            var made = new TcpListener(IPAddress.Loopback, port);
            made.Start();
            listener = made;
        }
        catch (SocketException e)
        {
            DiagnosticLog.Info("bridge.start", ("error", e.SocketErrorCode.ToString()));
            return null;
        }

        Key = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, "key"), Key);
        stopping = new CancellationTokenSource();
        var token = stopping.Token;
        var serving = listener;
        _ = Task.Run(() => Accept(serving, token));
        DiagnosticLog.Info("bridge.start", ("port", Listening));
        return Key;
    }

    internal static void Stop()
    {
        stopping?.Cancel();
        listener?.Stop();
        listener = null;
        Key = null;
        File.Delete(Path.Combine(Folder, "key"));
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
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            _ = Task.Run(() => Serve(client, token), token);
        }
    }

    private static async Task Serve(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
                while (!token.IsCancellationRequested && await reader.ReadLineAsync(token) is { } line)
                {
                    var answer = await Answer(line);
                    await writer.WriteLineAsync(answer.ToJsonString());
                    if (answer["refused"]?.GetValue<bool>() == true)
                    {
                        return;
                    }
                }
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or ObjectDisposedException)
            {
                // The script went away; nothing to answer.
            }
        }
    }

    /// <summary>One request's answer. A request without the right key is refused and the connection closed.</summary>
    internal static async Task<JsonObject> Answer(string line)
    {
        JsonObject request;
        try
        {
            request = JsonNode.Parse(line) as JsonObject ?? throw new JsonException("not an object");
        }
        catch (JsonException e)
        {
            return new JsonObject { ["ok"] = false, ["detail"] = "not a JSON object: " + e.Message };
        }

        string given = request["key"]?.GetValueKind() == JsonValueKind.String ? request["key"]!.GetValue<string>() : "";
        if (Key is not { } key || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(key)))
        {
            DiagnosticLog.Info("bridge.refused");
            return new JsonObject { ["ok"] = false, ["refused"] = true, ["detail"] = "the key is not this run's; read it from Settings, About or bridge/key" };
        }

        string what = request["do"]?.GetValueKind() == JsonValueKind.String ? request["do"]!.GetValue<string>().ToLowerInvariant() : "";
        Directory.CreateDirectory(Scenario.Results);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        JsonObject answer;
        switch (what)
        {
            case "ping":
                answer = new JsonObject { ["ok"] = true, ["version"] = AppInfo.Version, ["device"] = Phone.Platform.DeviceWords };
                break;
            case "tree":
                answer = new JsonObject { ["ok"] = true, ["elements"] = await Scenario.OnUi(Scenario.Elements) };
                break;
            case "log":
                int lines = request["lines"]?.GetValueKind() == JsonValueKind.Number ? Math.Clamp(request["lines"]!.GetValue<int>(), 1, 100_000) : 200;
                string? text = Scenario.LogLines(lines);
                answer = new JsonObject { ["ok"] = text is not null, ["lines"] = text };
                break;
            case "memory":
                answer = new JsonObject
                {
                    ["ok"] = true,
                    ["managedMb"] = Math.Round(GC.GetTotalMemory(false) / 1048576.0, 1),
                    ["workingSetMb"] = Math.Round(Environment.WorkingSet / 1048576.0, 1),
                    ["budgetMb"] = Math.Round(Phone.Platform.MemoryBudgetMegabytes()),
                };
                break;
            default:
                var (ok, detail) = await Scenario.Do(new Scenario.Step(what, request));
                answer = new JsonObject { ["ok"] = ok, ["detail"] = detail };
                if (what == "screenshot" && ok && File.Exists(Path.Combine(Scenario.Results, detail)))
                {
                    answer["png"] = Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(Scenario.Results, detail)));
                }

                break;
        }

        answer["ms"] = clock.ElapsedMilliseconds;
        DiagnosticLog.Info("bridge.do", ("do", what), ("ok", answer["ok"]?.GetValue<bool>()), ("ms", clock.ElapsedMilliseconds));
        return answer;
    }

    /// <summary>What Settings, About says while the bridge runs, or that it is off.</summary>
    internal static string Words() => Key is { } key
        ? string.Create(CultureInfo.InvariantCulture, $"Listening on this device only, port {Listening}, for a developer's scripts over the cable. This run's key: {key}")
        : "Off. Nothing listens.";
}
#endif
