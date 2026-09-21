using System.Text;
using System.Text.Json.Nodes;

namespace GroupLab.Core.Tests.Support;

internal static class Repo
{
    public static string Root { get; } = FindRoot();

    public static string PathTo(params string[] parts) => Path.Combine([Root, .. parts]);

    /// <summary>The version in Directory.Build.props, which every package and every build is named by.</summary>
    public static string Version()
    {
        string props = File.ReadAllText(PathTo("Directory.Build.props"));
        var m = System.Text.RegularExpressions.Regex.Match(props, @"<Version>(?<version>[^<]+)</Version>");
        return m.Success ? m.Groups["version"].Value : "";
    }

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GroupLab.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException($"No GroupLab.slnx above {AppContext.BaseDirectory}.");
    }
}

/// <summary>
/// Pulls examples straight out of docs/TARGET-SCHEMA.md, so the tests follow the specification
/// rather than a copy of it that can drift.
/// </summary>
internal static class Spec
{
    private static readonly Lazy<string[]> Lines = new(() =>
        File.ReadAllText(Repo.PathTo("docs", "TARGET-SCHEMA.md")).Replace("\r\n", "\n").Split('\n'));

    /// <summary>The first fenced json block after the heading that starts with <paramref name="heading"/>.</summary>
    public static string JsonBlock(string heading)
    {
        string[] lines = Lines.Value;
        int start = Array.FindIndex(lines, l => l.StartsWith(heading, StringComparison.Ordinal));
        Assert.True(start >= 0, $"Heading {heading} not found in TARGET-SCHEMA.md.");
        int open = Array.FindIndex(lines, start, l => l.Trim() == "```json");
        int close = Array.FindIndex(lines, open + 1, l => l.Trim() == "```");
        return string.Join("\n", lines[(open + 1)..close]);
    }

    public static string Section4Example => JsonBlock("## 4.");

    public static JsonObject Section4Node() => JsonNode.Parse(Section4Example)!.AsObject();

    /// <summary>A section 3 fragment such as <c>"dataBlock": { ... }</c>, as its property name and value.</summary>
    public static (string Name, JsonNode Value) Fragment(string heading)
    {
        var wrapped = JsonNode.Parse("{" + JsonBlock(heading) + "}")!.AsObject();
        var (name, value) = wrapped.Single();
        return (name, value!.DeepClone());
    }

    public static byte[] Utf8(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
}
