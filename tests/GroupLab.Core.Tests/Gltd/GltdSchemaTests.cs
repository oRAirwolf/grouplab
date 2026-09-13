using GroupLab.Core.Gltd.Schema;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Gltd;

/// <summary>The shipped schema must be the one TARGET-SCHEMA.md section 9 publishes, so validation needs no network.</summary>
public class GltdSchemaTests
{
    [Fact]
    public void EmbeddedSchemaIsSection9Verbatim()
    {
        Assert.Equal(Spec.JsonBlock("## 9.") + "\n", GltdSchema.ReadText().Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
