using System.Globalization;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// One store-bought target GroupLab can recognize, entry 340 section 1: who makes it, what it is called, its printed size as the maker
/// gives it, and the family it belongs to where the same artwork is sold at several sizes (section 2).
/// </summary>
/// <param name="Id">The fingerprint's name, which is also the folder its blank was scanned into.</param>
/// <param name="Maker">The maker, as printed on the packet.</param>
/// <param name="Name">The product's name without its size, the same for every member of a family.</param>
/// <param name="Size">The printed size as the maker names it, such as "6 in".</param>
/// <param name="Catalogue">The maker's item number.</param>
/// <param name="Family">The family's name where the same artwork comes at more than one size, otherwise null.</param>
public sealed record StoreTarget(string Id, string Maker, string Name, string Size, string Catalogue, string? Family)
{
    /// <summary>The name a person sees: size, name and maker, such as "Shoot-N-C 6 in bullseye, Birchwood Casey 34550".</summary>
    public string Title => string.Create(CultureInfo.InvariantCulture, $"{Name.Replace("{size}", Size, StringComparison.Ordinal)}, {Maker} {Catalogue}");

    /// <summary>The short name, without the maker.</summary>
    public string ShortName => Name.Replace("{size}", Size, StringComparison.Ordinal);

    /// <summary>The fingerprint, read once from the application's own resources.</summary>
    public TargetFingerprint Fingerprint => StoreTargetLibrary.FingerprintOf(Id);

    /// <summary>The printed area's width and height in inches, as the fingerprint measured it from the 600 dpi scan.</summary>
    public (double Width, double Height) PrintedInches => (Fingerprint.Layout.Width, Fingerprint.Layout.Height);
}

/// <summary>
/// The store-bought targets GroupLab recognizes, entry 340: the five Birchwood Casey products Alan scanned at 600 dpi (request 58), each
/// recognized from a fingerprint shipped inside the application. Only the fingerprints are shipped and committed; the scans are another
/// maker's printing and never leave the computer they were made on (samples/PROVENANCE.md). The fingerprints are made again with
/// <c>grouplab store-fingerprints build</c>.
/// </summary>
public static class StoreTargetLibrary
{
    /// <summary>The family of the Shoot-N-C bullseye, one artwork printed at 6 and at 8 inches.</summary>
    public const string ShootNCBullseye = "Shoot-N-C bullseye";

    /// <summary>Every product, in the order they are tried.</summary>
    public static IReadOnlyList<StoreTarget> All { get; } =
    [
        new("bc-34105-shoot-n-c-sight-in", "Birchwood Casey", "Shoot-N-C {size} sight-in grid", "8 in", "34105", null),
        new("bc-34550-shoot-n-c-6in-bull", "Birchwood Casey", "Shoot-N-C {size} bullseye", "6 in", "34550", ShootNCBullseye),
        new("bc-34805-shoot-n-c-8in-bull", "Birchwood Casey", "Shoot-N-C {size} bullseye", "8 in", "34805", ShootNCBullseye),
        new("bc-34806-shoot-n-c-8in-crosshair", "Birchwood Casey", "Shoot-N-C {size} crosshair", "8 in", "34806", null),
        new("bc-37826-eze-scorer-bull", "Birchwood Casey", "Eze-Scorer {size} bullseye", "paper", "37826", null),
    ];

    private static readonly Dictionary<string, TargetFingerprint> Loaded = new(StringComparer.Ordinal);

    private static readonly Lock Guard = new();

    /// <summary>The product with this identifier, or null.</summary>
    public static StoreTarget? Find(string id) => All.FirstOrDefault(t => t.Id == id);

    /// <summary>Every member of a family, smallest printed size first.</summary>
    public static IReadOnlyList<StoreTarget> Members(string family) =>
        [.. All.Where(t => t.Family == family).OrderBy(t => t.PrintedInches.Width * t.PrintedInches.Height)];

    /// <summary>The resource a product's fingerprint is shipped as.</summary>
    public static string ResourceName(string id) => $"GroupLab.Core.StoreTargets.Fingerprints.{id}.glfp";

    /// <summary>A product's fingerprint, read from the application's resources the first time it is asked for and kept.</summary>
    public static TargetFingerprint FingerprintOf(string id)
    {
        lock (Guard)
        {
            if (Loaded.TryGetValue(id, out var known))
            {
                return known;
            }

            using var stream = typeof(StoreTargetLibrary).Assembly.GetManifestResourceStream(ResourceName(id))
                ?? throw new InvalidOperationException($"no fingerprint is shipped for {id}");
            var read = TargetFingerprint.Read(stream);
            Loaded[id] = read;
            return read;
        }
    }
}
