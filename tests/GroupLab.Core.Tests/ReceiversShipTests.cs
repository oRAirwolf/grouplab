using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 195 section 1: error-report.php was written, given its nginx block and installed on the server, and the
/// site never shipped it, so the live address answered "File not found." and request 24 stopped at its last step. Every receiver in
/// website/api/ is now held to all three places it has to be: the site builder's list, the builder's copy into the site, and the nginx
/// include; and every receiver the include names has its file.
/// </summary>
public class ReceiversShipTests
{
    [Fact]
    public void EveryReceiverIsShippedAndNamedAndEveryNamedReceiverExists()
    {
        string build = File.ReadAllText(Repo.PathTo("website", "build.py"));
        string include = File.ReadAllText(Repo.PathTo("website", "server", "nginx.ssl.conf_grouplab"));
        var files = Directory.EnumerateFiles(Repo.PathTo("website", "api"), "*.php").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToList();
        var listed = Regex.Match(build, @"(?m)^RECEIVERS = \[(?<items>[^\]]*)\]").Groups["items"].Value;
        var named = Regex.Matches(include, @"(?m)^location = /api/(?<name>[\w-]+\.php)\s*\{").Select(m => m.Groups["name"].Value).ToList();

        Assert.NotEmpty(files);
        foreach (string file in files)
        {
            Assert.True(listed.Contains($"\"api/{file}\"", StringComparison.Ordinal), $"website/api/{file} is not in build.py's RECEIVERS");
            Assert.True(build.Contains($"copy(need(REPO / \"website\" / \"api\" / \"{file}\"), \"api/{file}\")", StringComparison.Ordinal),
                $"website/api/{file} is never copied into the site, so the server never has it");
            Assert.True(named.Contains(file), $"website/api/{file} has no location block in the nginx include");
        }

        foreach (string name in named)
        {
            Assert.True(files.Contains(name), $"the nginx include routes /api/{name} and there is no website/api/{name}");
        }
    }
}
