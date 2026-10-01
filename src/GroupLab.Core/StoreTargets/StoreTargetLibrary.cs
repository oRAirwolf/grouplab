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

    /// <summary>The built-in library's list, shipped beside the fingerprints in every build; <c>grouplab target-reference add</c> adds to it.</summary>
    public const string ListResource = "GroupLab.Core.StoreTargets.Fingerprints.library.json";

    private static readonly IReadOnlyList<StoreTarget> BuiltIn = ReadList();

    private static IReadOnlyList<StoreTarget> installed = [];

    private static readonly Dictionary<string, TargetFingerprint> Loaded = new(StringComparer.Ordinal);

    private static readonly Lock Guard = new();

    /// <summary>
    /// Every product, in the order they are tried: the built-in library, then any newer one fetched and installed (entry 344 section 3), a
    /// fetched product replacing a built-in one of the same name.
    /// </summary>
    public static IReadOnlyList<StoreTarget> All
    {
        get
        {
            lock (Guard)
            {
                return [.. BuiltIn.Where(b => installed.All(i => i.Id != b.Id)), .. installed];
            }
        }
    }

    /// <summary>The built-in products alone, as this build shipped them.</summary>
    public static IReadOnlyList<StoreTarget> Shipped => BuiltIn;

    /// <summary>
    /// Entry 344 section 3: the products of a signed library fetched after this build, added to the built-in ones until the application
    /// closes. Only a library whose signature checked reaches here (<see cref="StoreLibraryFile.Read"/>).
    /// </summary>
    public static void Install(IEnumerable<TargetReference> references)
    {
        ArgumentNullException.ThrowIfNull(references);
        lock (Guard)
        {
            var list = references.ToList();
            foreach (var r in list)
            {
                Loaded[r.Target.Id] = r.Fingerprint;
            }

            installed = [.. list.Select(r => r.Target)];
        }
    }

    /// <summary>Takes away every installed product, leaving the built-in library, for the tests.</summary>
    public static void Uninstall()
    {
        lock (Guard)
        {
            foreach (var t in installed.Where(i => BuiltIn.All(b => b.Id != i.Id)))
            {
                Loaded.Remove(t.Id);
            }

            installed = [];
        }
    }

    private static List<StoreTarget> ReadList()
    {
        using var stream = typeof(StoreTargetLibrary).Assembly.GetManifestResourceStream(ListResource)
            ?? throw new InvalidOperationException("the store-bought target list is not shipped");
        var root = System.Text.Json.Nodes.JsonNode.Parse(stream)!;
        return [.. root["targets"]!.AsArray().Select(t => new StoreTarget((string)t!["id"]!, (string)t["maker"]!, (string)t["name"]!, (string)t["size"]!,
            (string)t["catalog"]!, (string?)t["family"]))];
    }

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
