using System.Globalization;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab sync</c>, NOTES-FROM-PLANNING.md entry 461 section 3.1: synchronization through a folder from the command line, for testing it
/// before any screen exists. A database and a folder: the database's designed sheets and portable settings are kept beside it, in
/// <c>&lt;database&gt;.sheets</c> and <c>&lt;database&gt;.settings.json</c>, so several databases can stand for several devices sharing one
/// folder. <c>--list</c> shows the records waiting for a choice and the records kept; <c>--choose</c> settles one.
/// </summary>
public static class SyncVerb
{
    public const string Usage = """
        grouplab sync <database> <folder> [--name <device name>] [--originals]
        grouplab sync <database> --list
        grouplab sync <database> --choose <number> this|other|both|understood
        """;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Count < 2)
        {
            error.WriteLine(Usage);
            return 2;
        }

        string database = args[0];
        var store = SessionStore.Open(database);
        var local = new FileSyncLocal(database + ".sheets", database + ".settings.json");
        if (args[1] == "--list")
        {
            foreach (var c in store.SyncConflicts())
            {
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{c.Id}: {c.Kind} {c.Key}, {(c.Payload is null ? "a deletion against a change, the change kept" : "changed here and on device " + c.FromDevice)}, {c.FoundUtc}"));
            }

            foreach (var k in store.Kept())
            {
                output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"kept {k.Id}: {k.Kind} {k.Key}, {k.Why}, {k.KeptUtc}"));
            }

            return 0;
        }

        if (args[1] == "--choose")
        {
            if (args.Count < 4 || !long.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long id))
            {
                error.WriteLine(Usage);
                return 2;
            }

            var choice = args[3] switch
            {
                "this" => SyncChoice.KeepThisOne,
                "other" => SyncChoice.KeepTheOther,
                "both" => SyncChoice.KeepBoth,
                "understood" => SyncChoice.Understood,
                _ => (SyncChoice?)null,
            };
            if (choice is null)
            {
                error.WriteLine(Usage);
                return 2;
            }

            SyncEngine.Resolve(store, local, id, choice.Value, DateTime.UtcNow);
            output.WriteLine($"Chosen. The change travels on the next sync.");
            return 0;
        }

        string folder = args[1];
        string name = Environment.MachineName;
        bool originals = false;
        for (int i = 2; i < args.Count; i++)
        {
            if (args[i] == "--name" && i + 1 < args.Count)
            {
                name = args[++i];
            }
            else if (args[i] == "--originals")
            {
                originals = true;
            }
            else
            {
                error.WriteLine(Usage);
                return 2;
            }
        }

        var report = SyncEngine.Run(store, local, new DirectorySyncFolder(folder), name, DateTime.UtcNow, new SyncOptions(Originals: originals));
        foreach (string line in report.Said)
        {
            output.WriteLine(line);
        }

        return report.Reached ? 0 : 1;
    }
}
