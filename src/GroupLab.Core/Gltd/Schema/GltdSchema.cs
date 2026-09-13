namespace GroupLab.Core.Gltd.Schema;

/// <summary>The JSON Schema of TARGET-SCHEMA.md section 9, shipped so validation never needs a network.</summary>
public static class GltdSchema
{
    public const string ResourceName = "GroupLab.Core.Gltd.Schema.gltd-1.schema.json";

    public static string ReadText()
    {
        using var stream = typeof(GltdSchema).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
