using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia.Media;
using GroupLab.App.Theme;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 508 section 1: the iPhone's launch screen is an empty outline of the home screen, the lockup, the caliber
/// card, Take a picture and its pair, and the bar along the bottom, in the application's own colors in both appearances, with no words
/// but the name, so the change from it to the first screen is barely seen. The storyboard itself is compiled on a Mac (ios-app.yml).
/// </summary>
public sealed class Entry508LaunchScreenTests
{
    private static readonly string Ios = Path.Combine(Entry109Tests.Repository(), "ios", "GroupLab.iOS");

    /// <summary>Each launch color and the palette role it copies, light and dark.</summary>
    private static readonly (string Name, Func<Palette, Color> Role)[] Colors =
    [
        ("LaunchBackground", p => p.Bg),
        ("LaunchPanel", p => p.Panel),
        ("LaunchPanel2", p => p.Panel2),
        ("LaunchLine", p => p.Line),
        ("LaunchLine2", p => p.Line2),
        ("LaunchAmber", p => p.Amber),
    ];

    [Fact]
    public void BothPlistsNameTheStoryboardAndTheProjectCompilesIt()
    {
        foreach (string plist in new[] { "Info.plist", "Info.dev.plist" })
        {
            string text = File.ReadAllText(Path.Combine(Ios, plist));
            Assert.Contains("<key>UILaunchStoryboardName</key>\r\n\t<string>LaunchScreen</string>", text, StringComparison.Ordinal);
            Assert.DoesNotContain("<key>UILaunchScreen</key>", text, StringComparison.Ordinal);
        }

        string project = File.ReadAllText(Path.Combine(Ios, "GroupLab.iOS.csproj"));
        Assert.Contains("<InterfaceDefinition Include=\"LaunchScreen.storyboard\" />", project, StringComparison.Ordinal);
    }

    [Fact]
    public void TheStoryboardIsALaunchScreenWithNoWordsButTheName()
    {
        var story = XDocument.Load(Path.Combine(Ios, "LaunchScreen.storyboard"));
        Assert.Equal("YES", (string?)story.Root!.Attribute("launchScreen"));
        string[] wordy = ["label", "textView", "textField", "button", "progressView", "activityIndicatorView"];
        Assert.DoesNotContain(story.Descendants(), e => wordy.Contains(e.Name.LocalName));
        var images = story.Descendants("imageView").Select(e => (string?)e.Attribute("image")).ToList();
        Assert.Equal(["LaunchLockup"], images);

        // Every color it draws with is one of the launch colors, all of which exist in the asset catalog.
        var used = story.Descendants("color").Select(e => (string?)e.Attribute("name")).Where(n => n is not null).Distinct().ToList();
        Assert.NotEmpty(used);
        Assert.All(used, n => Assert.Contains(n, Colors.Select(c => c.Name)));
    }

    [Fact]
    public void ItsColorsAreTheApplicationsInBothAppearances()
    {
        var story = XDocument.Load(Path.Combine(Ios, "LaunchScreen.storyboard"));
        foreach (var (name, role) in Colors)
        {
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Ios, "Assets.xcassets", name + ".colorset", "Contents.json")));
            var entries = json.RootElement.GetProperty("colors").EnumerateArray().ToList();
            Assert.Equal(2, entries.Count);
            foreach (var entry in entries)
            {
                bool dark = entry.TryGetProperty("appearances", out var looks)
                    && looks.EnumerateArray().Any(a => a.GetProperty("value").GetString() == "dark");
                var parts = entry.GetProperty("color").GetProperty("components");
                Color expected = role(dark ? Tokens.Dark : Tokens.Light);
                Assert.Equal(expected.R, Hex(parts, "red"));
                Assert.Equal(expected.G, Hex(parts, "green"));
                Assert.Equal(expected.B, Hex(parts, "blue"));
            }

            // The storyboard's own copy, used only where the catalog cannot be read, is the dark one it was drawn in.
            var copy = story.Descendants("namedColor").Single(e => (string?)e.Attribute("name") == name).Element("color")!;
            Color darkRole = role(Tokens.Dark);
            Assert.Equal(darkRole.R, Unit(copy, "red"));
            Assert.Equal(darkRole.G, Unit(copy, "green"));
            Assert.Equal(darkRole.B, Unit(copy, "blue"));
        }
    }

    /// <summary>The name is the application's own lockup, light and dark, the same files the home screen draws.</summary>
    [Fact]
    public void TheNameIsTheHomeScreensLockup()
    {
        string set = Path.Combine(Ios, "Assets.xcassets", "LaunchLockup.imageset");
        string assets = Path.Combine(Entry109Tests.Repository(), "src", "GroupLab.App", "Assets");
        foreach (string file in new[] { "grouplab-lockup.svg", "grouplab-lockup-light.svg" })
        {
            Assert.Equal(File.ReadAllBytes(Path.Combine(assets, file)), File.ReadAllBytes(Path.Combine(set, file)));
        }

        string contents = File.ReadAllText(Path.Combine(set, "Contents.json"));
        Assert.Contains("\"preserves-vector-representation\": true", contents, StringComparison.Ordinal);
        Assert.Contains("\"value\": \"dark\"", contents, StringComparison.Ordinal);
    }

    private static byte Hex(JsonElement parts, string channel) =>
        byte.Parse(parts.GetProperty(channel).GetString()!.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static byte Unit(XElement color, string channel) =>
        (byte)Math.Round(double.Parse((string)color.Attribute(channel)!, CultureInfo.InvariantCulture) * 255);
}
