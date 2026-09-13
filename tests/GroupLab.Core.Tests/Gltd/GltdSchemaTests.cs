using GroupLab.Core.Gltd.Schema;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Gltd;

public class GltdSchemaTests
{
    [Fact]
    public void EmbeddedSchemaIsSection9Verbatim()
    {
        Assert.Equal(Spec.JsonBlock("## 9.") + "\n", GltdSchema.ReadText().Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
