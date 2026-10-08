using System.Buffers.Binary;
using Avalonia.Headless.XUnit;
using GroupLab.App;
using GroupLab.Cli.Imaging;

namespace GroupLab.App.Tests;

/// <summary>
/// Question 43, approved by Alan in entry 388 section 3: a pixel cap on opening any image, judged from the file's own header before
/// anything is decoded and saying the size and the limit, and the decode on a background thread with a time limit, so a large scan no
/// longer stops the window answering.
/// </summary>
public class Question43Tests
{
    /// <summary>A PNG of a few dozen bytes whose header claims 50,000 by 50,000 pixels, 2,500 megapixels, and holds no picture at all.</summary>
    private static string Claims(string folder)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, 50_000);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), 50_000);
        header[8] = 8; // eight bits a channel
        header[9] = 2; // colour
        using var file = new MemoryStream();
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Chunk(file, "IHDR", header);
        Chunk(file, "IDAT", [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01]);
        Chunk(file, "IEND", []);
        string path = Path.Combine(folder, "claims-enormous.png");
        File.WriteAllBytes(path, file.ToArray());
        return path;
    }

    private static void Chunk(Stream to, string type, byte[] data)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        to.Write(length);
        byte[] typed = [.. System.Text.Encoding.ASCII.GetBytes(type), .. data];
        to.Write(typed);
        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc(typed));
        to.Write(crc);
    }

    private static uint Crc(byte[] bytes)
    {
        uint c = 0xFFFFFFFF;
        foreach (byte b in bytes)
        {
            c ^= b;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }
        }

        return c ^ 0xFFFFFFFF;
    }

    [Fact]
    public void AHeaderClaimingTooManyPixelsIsRefusedBeforeItIsDecoded()
    {
        string folder = GroupLab.Tests.Support.Temp.Folder("q43");
        try
        {
            string path = Claims(folder);
            foreach (var load in new Action[] { () => ImageLoader.Load(path), () => ImageLoader.LoadMaxChannel(path), () => ImageLoader.LoadForEditor(path), () => ImageLoader.Checked(path) })
            {
                var refused = Assert.ThrowsAny<InvalidDataException>(load);
                Assert.True(ImageLoader.IsTooLarge(refused), refused.Message);
                Assert.Contains("50000 by 50000, which is 2500 megapixels", refused.Message, StringComparison.Ordinal);
            }
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [AvaloniaFact]
    public void TheWindowSaysTheSizeAndTheLimitAndKeepsTheSheet()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            string was = window.Session.State.ImagePath!;
            string huge = Claims(Path.GetDirectoryName(path)!);
            window.OpenDropped([huge]);
            if (window.HasUnsavedWork)
            {
                window.AnswerDiscard();
            }

            Assert.False(window.Opened());
            Assert.Equal(was, window.Session.State.ImagePath);
            Assert.Equal("The picture is too large to open", window.ProblemTitle);
            Assert.Contains("claims-enormous.png is 50000 by 50000, which is 2500 megapixels", window.StatusText, StringComparison.Ordinal);
            Assert.DoesNotContain(Path.GetDirectoryName(path)!, window.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void AnOpenPastTheTimeLimitStopsWaitingAndSaysSo()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        var limit = MainWindow.OpenTimeLimit;
        try
        {
            string was = window.Session.State.ImagePath!;
            string second = Path.Combine(Path.GetDirectoryName(path)!, "second.png");
            File.Copy(path, second);
            MainWindow.OpenTimeLimit = TimeSpan.FromTicks(1);
            window.OpenDropped([second]);
            if (window.HasUnsavedWork)
            {
                window.AnswerDiscard();
            }

            Assert.False(window.Opened());
            Assert.Equal(was, window.Session.State.ImagePath);
            Assert.Equal("The picture took too long to open", window.ProblemTitle);
            Assert.Contains("GroupLab stopped waiting for second.png after 0 seconds", window.StatusText, StringComparison.Ordinal);

            // With the limit back, the same file opens, off the window's thread.
            MainWindow.OpenTimeLimit = limit;
            window.OpenDropped([second]);
            if (window.HasUnsavedWork)
            {
                window.AnswerDiscard();
            }

            Assert.True(window.Opened());
            Assert.Equal(second, window.Session.State.ImagePath);
        }
        finally
        {
            MainWindow.OpenTimeLimit = limit;
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
