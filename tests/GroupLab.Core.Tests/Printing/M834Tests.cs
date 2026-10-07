using System.Security.Cryptography;
using GroupLab.Core.Printing.Labels;
using GroupLab.Core.Printing.Thermal;

namespace GroupLab.Core.Tests.Printing;

/// <summary>Request 73's recording (2026-10-05): the Phomemo M834 takes its page as LZO1X blocks of 4096 bytes, and GroupLab now writes them.</summary>
public sealed class M834Tests
{
    /// <summary>The app's first block, the paper above the C bull sheet's top edge: 44 bytes for 4096 bytes of white.</summary>
    private const string White = "02000000000020000000000000000000000000000000da10000c000000000000000000000000000000110000";

    /// <summary>The app's 89th block, part of a row of bulls, which uses the short matches GroupLab's own compressor never writes.</summary>
    private const string Bulls = "0300003fff0000201003001fff802010cc002013c2007ff82012d6003ffe2012d2001ffc2013d0002014a501ff2012a4012012c9003f2013ec04201518042013a5010f2013c105072013c0052012ee041fe02012d1000f20131a0407f02012a501032013a501032013c005201272020fc02012d0002015180420139506032048c005201318042013ed0403201418042013d100012014d0002013c0052012ed04032012ce00000120131d1b0120139506002014d100602013500320000000000000000003cc000c000000000000000000000000000000110000";

    [Fact]
    public void TheAppsBlocksUnpackToThePageItPrinted()
    {
        Assert.Equal(new byte[4096], Lzo1x.Decompress(Convert.FromHexString(White)));
        byte[] bulls = Lzo1x.Decompress(Convert.FromHexString(Bulls));
        Assert.Equal(4096, bulls.Length);
        Assert.Equal("967356f65e57eeaefb6e2a19ee0665c19d7cf76b5b43edbbc3e8c75117b3c443", Convert.ToHexStringLower(SHA256.HashData(bulls)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(239)]
    [InlineData(4096)]
    [InlineData(70000)]
    public void WhatGroupLabPacksUnpacksToTheSameBytes(int length)
    {
        var rng = new Random(length);
        foreach (Func<int, byte> fill in new Func<int, byte>[] { _ => (byte)rng.Next(256), i => (byte)(rng.Next(25) == 0 ? rng.Next(256) : 0), i => (byte)((i / 9) % 4) })
        {
            byte[] data = [.. Enumerable.Range(0, length).Select(fill)];
            Assert.Equal(data, Lzo1x.Decompress(Lzo1x.Compress(data)));
        }

        Assert.True(Lzo1x.Compress(new byte[4096]).Length < 40, "a white piece packs small");
    }

    [Fact]
    public void ALetterPageIsSentCentredOnTheHeadAfterTheAppsSettings()
    {
        var profile = PrinterProfiles.All.Single(p => p.Id == "phomemo-m834");
        Assert.IsType<PhomemoLzoEncoder>(PrinterEncoders.For(profile));
        var page = new DotImage(2550, 3300);
        page.Fill(0, 0, 8, 3300);
        page.Fill(2542, 100, 2550, 101);
        byte[] stream = PrinterEncoders.For(profile).Encode(new LabelJob(page, 215.9, 279.4), profile);
        Assert.Equal(PhomemoLzoEncoder.Settings, stream[..PhomemoLzoEncoder.Settings.Length]);
        var (across, height, rows) = PhomemoLzoEncoder.Read(stream);
        Assert.Equal((316, 3300), (across, height));
        // 319 bytes on a 316 byte head: one byte off the left and two off the right, so the left edge's line is gone and so is the right's.
        Assert.All(Enumerable.Range(0, height), y => Assert.Equal(0, rows[y * across]));
        Assert.Equal(0, rows.Count(b => b != 0));
        page.Fill(400, 50, 408, 51);
        rows = PhomemoLzoEncoder.Read(PrinterEncoders.For(profile).Encode(new LabelJob(page, 215.9, 279.4), profile)).Rows;
        Assert.Equal(0xFF, rows[(50 * across) + 49]);
    }

    /// <summary>
    /// Entry 382 section 1: GroupLab's Letter page is 3300 rows at 300 dpi, true size, against the 3294 the app sent with its page shrunk
    /// to 94.7 percent and about 15 mm of white left at the bottom. On a roll 15.5 mm of white follows (184 rows), on fanfold nothing.
    /// </summary>
    [Fact]
    public void OnARollTheSheetIsFedPastTheTearBarAndOnFanfoldItIsNot()
    {
        var profile = PrinterProfiles.All.Single(p => p.Id == "phomemo-m834");
        var scene = GroupLab.Core.Rendering.SceneBuilder.Build(GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(Support.Repo.PathTo("targets", "GL-CF25-LTR.gltd.json")).Definition!).Pages[0];
        var page = ThermalRaster.Render(scene, profile.Head).Image;
        Assert.Equal(3300, page.Height);
        Assert.Equal(184, TearBar.Rows(TearBar.FeedAfterMm(PaperForm.Roll), profile.DotsPerInch));
        Assert.Equal(0, TearBar.FeedAfterMm(PaperForm.Fanfold));

        var fanfold = PhomemoLzoEncoder.Read(PrinterEncoders.For(profile).Encode(new LabelJob(page, 215.9, 279.4, FeedAfterMm: TearBar.FeedAfterMm(PaperForm.Fanfold)), profile));
        var roll = PhomemoLzoEncoder.Read(PrinterEncoders.For(profile).Encode(new LabelJob(page, 215.9, 279.4, FeedAfterMm: TearBar.FeedAfterMm(PaperForm.Roll)), profile));
        Assert.Equal(3300, fanfold.Height);
        Assert.Equal(3484, roll.Height);
        Assert.Equal(fanfold.Rows, roll.Rows[..fanfold.Rows.Length]);
        Assert.All(roll.Rows[fanfold.Rows.Length..], b => Assert.Equal(0, b));
        Assert.True(page.BlackDots() > 0);
    }
}
