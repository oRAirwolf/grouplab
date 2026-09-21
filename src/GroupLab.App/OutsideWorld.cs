using System.Diagnostics;

namespace GroupLab.App;

/// <summary>
/// Every way GroupLab reaches out of its own process, behind one interface, NOTES-FROM-PLANNING.md entry 122.
/// <para>
/// <b>Why this exists.</b> The settings page's link to the repository opened the real default browser, and entry 117's control walk clicks
/// every control it finds, so a test run opened tabs on Alan's machine. That is the same fault as entry 114's print to the OneNote driver:
/// a test reaching out of the process and into the person's own applications. One way out, and the tests replace it.
/// </para>
/// </summary>
public interface IOutsideWorld
{
    /// <summary>Opens a web address in whatever the person uses for one.</summary>
    void OpenAddress(string address);

    /// <summary>Opens a file in whatever the person uses for its kind: a PDF in their reader, an image in their viewer.</summary>
    void OpenFile(string path);

    /// <summary>Opens a folder in the file manager.</summary>
    void OpenFolder(string path);
}

/// <summary>The real one, used by the running application and by nothing else.</summary>
public sealed class TheOutsideWorld : IOutsideWorld
{
    /// <summary>What the application uses unless something has replaced it. A test replaces it; nothing else may.</summary>
    public static IOutsideWorld Current { get; set; } = new TheOutsideWorld();

    public void OpenAddress(string address) => Start(address);

    public void OpenFile(string path) => Start(path);

    public void OpenFolder(string path) => Start(path);

    private static void Start(string what)
    {
        using var started = Process.Start(new ProcessStartInfo(what) { UseShellExecute = true });
    }
}

/// <summary>
/// What the tests and the benchmark use instead: it writes down what would have happened and does nothing. Entry 122 section 3: the control
/// walk clicks these buttons against this, so they are measured rather than excluded by name.
/// </summary>
public sealed class RecordedOutsideWorld : IOutsideWorld
{
    private readonly List<(string What, string Target)> _asked = [];

    /// <summary>Everything that was asked for, in order.</summary>
    public IReadOnlyList<(string What, string Target)> Asked => _asked;

    public void OpenAddress(string address) => _asked.Add(("address", address));

    public void OpenFile(string path) => _asked.Add(("file", path));

    public void OpenFolder(string path) => _asked.Add(("folder", path));
}
