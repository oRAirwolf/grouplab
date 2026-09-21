using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace GroupLab.Cli;

/// <summary>
/// Reads the train and version stamped into a built assembly, NOTES-FROM-PLANNING.md entry 125 section 1.
/// <para>
/// <b>Why this exists.</b> `v0.2.0-nightly.12` was published with its version and its commit stamped in and its train missing, so it called
/// itself a development build and would never have updated itself. The cause was an initialisation order in <c>AppInfo</c> and is fixed, but
/// the packaging is where the claim can actually be checked, and a nightly that would call itself a development build must never be published
/// again. The packaging workflow runs this on what it has just published, for every platform, and fails the run rather than releasing it.
/// </para>
/// <para>
/// It reads the metadata out of the file rather than loading it, so the check works on a Linux assembly from a Windows runner and on a build
/// for an architecture the runner cannot execute. <c>System.Reflection.Metadata</c> is in the shared framework, so this needs no package.
/// </para>
/// </summary>
public static class BuildStampVerb
{
    /// <summary>The metadata key the build stamps the train into, which <c>Directory.Build.props</c> writes.</summary>
    public const string TrainKey = "GroupLabTrain";

    /// <summary>
    /// Prints what an assembly says it is, and with <paramref name="expected"/> fails unless it says that train.
    /// </summary>
    public static int Run(string path, string? expected, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (!File.Exists(path))
        {
            error.WriteLine("there is no assembly at " + path);
            return 2;
        }

        string? train, version;
        try
        {
            (train, version) = ReadStamp(path);
        }
        catch (BadImageFormatException)
        {
            error.WriteLine(path + " is not a .NET assembly, so it carries no build stamp");
            return 2;
        }

        output.WriteLine($"train: {train ?? "none"}");
        output.WriteLine($"version: {version ?? "none"}");

        if (expected is null)
        {
            return 0;
        }

        if (!string.Equals(train, expected, StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine(
                $"this build is stamped \"{train ?? "none"}\" and was packaged for the {expected} train. A build whose train is missing "
                + "calls itself a development build, never offers an update and never installs one, so it must not be published. "
                + "See NOTES-FROM-PLANNING.md entry 125 section 1.");
            return 1;
        }

        output.WriteLine($"stamped for the {expected} train, as packaged");
        return 0;
    }

    /// <summary>The train and informational version an assembly carries, without loading or running any of it.</summary>
    public static (string? Train, string? Version) ReadStamp(string path)
    {
        using var file = File.OpenRead(path);
        using var pe = new PEReader(file);
        var reader = pe.GetMetadataReader();

        string? train = null;
        string? version = null;
        foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(handle);
            string name = AttributeName(reader, attribute);

            // Only these two are read. An assembly carries attributes of every shape, and trying to read strings out of one that holds an
            // enum or an array throws, so the name is checked before the blob is touched.
            if (name is not ("AssemblyMetadataAttribute" or "AssemblyInformationalVersionAttribute"))
            {
                continue;
            }

            var arguments = Strings(reader, attribute);
            if (name == "AssemblyMetadataAttribute" && arguments is [TrainKey, var value])
            {
                train = value;
            }
            else if (name == "AssemblyInformationalVersionAttribute" && arguments is [var informational])
            {
                version = informational;
            }
        }

        return (train, version);
    }

    private static string AttributeName(MetadataReader reader, CustomAttribute attribute) => attribute.Constructor.Kind switch
    {
        HandleKind.MemberReference when reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent is { Kind: HandleKind.TypeReference } parent
            => reader.GetString(reader.GetTypeReference((TypeReferenceHandle)parent).Name),
        HandleKind.MethodDefinition
            => reader.GetString(reader.GetTypeDefinition(reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType()).Name),
        _ => "",
    };

    /// <summary>
    /// The string arguments of an attribute, read from its blob. Both attributes this cares about take only strings, so the blob is a
    /// two-byte prologue and then one length-prefixed UTF-8 string per argument; anything else is not one of ours and is skipped.
    /// </summary>
    private static string?[]? Strings(MetadataReader reader, CustomAttribute attribute)
    {
        var blob = reader.GetBlobReader(attribute.Value);
        if (blob.Length < 2 || blob.ReadUInt16() != 1)
        {
            return null;
        }

        var found = new List<string?>();
        try
        {
            while (blob.RemainingBytes > 2)
            {
                found.Add(blob.ReadSerializedString());
            }
        }
        catch (BadImageFormatException)
        {
            // Not the shape expected, so it is not one of ours however it is named.
            return null;
        }

        return [.. found];
    }
}
