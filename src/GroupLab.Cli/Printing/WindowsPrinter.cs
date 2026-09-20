using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Printing;
using GroupLab.Core.Rendering;

namespace GroupLab.Cli.Printing;

/// <summary>
/// Printing from inside GroupLab on Windows, NOTES-FROM-PLANNING.md entry 107 section 2, as question 21 planned it: Win32 through P/Invoke,
/// <c>PrintDlgEx</c> for the real dialog and GDI to draw, with no package added. The renderer's scene is drawn as vector in its own units, half-dmm,
/// through GDI's anisotropic mapping, so one unit of the definition is one unit on the paper; the origin is shifted by the printer's physical
/// offsets, because GDI's origin is the printable area and not the paper's edge. What is decided before anything is sent, the paper and the
/// margin, is <see cref="PrintFit"/>'s, tested on every platform; this class only asks the driver and draws.
/// <para>
/// <b>Why not <c>System.Drawing.Printing</c> or the WinRT print manager</b>, question 21 section 2: the first needs the
/// <c>System.Drawing.Common</c> package and is Windows-only anyway; the second needs a print document source built for its preview model.
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowsPrinter
{
    /// <summary>
    /// Shows the print dialog owned by <paramref name="owner"/>, with the sheet's paper chosen when it is a known size, and prints what the
    /// person chose, or says why not. Copies are the driver's, through the dialog; a set of several sheets offers page ranges.
    /// </summary>
    public static PrintOutcome PrintWithDialog(IntPtr owner, IReadOnlyList<Scene> pages, string sheet)
    {
        ArgumentNullException.ThrowIfNull(pages);
        var ranges = Marshal.AllocHGlobal((int)MaxRanges * 8);
        IntPtr devMode = PresetDevMode(pages[0]);
        var dialog = new PrintDlgEx
        {
            StructSize = (uint)Marshal.SizeOf<PrintDlgEx>(),
            Owner = owner,
            DevMode = devMode,
            Flags = PdReturnDc | PdUseDevModeCopiesAndCollate | PdHidePrintToFile | PdNoSelection | PdNoCurrentPage | (pages.Count == 1 ? PdNoPageNums : 0),
            MaxPageRanges = pages.Count == 1 ? 0u : MaxRanges,
            PageRanges = pages.Count == 1 ? IntPtr.Zero : ranges,
            MinPage = 1,
            MaxPage = (uint)pages.Count,
            Copies = 1,
            StartPage = StartPageGeneral,
        };

        try
        {
            int hr = PrintDlgExW(ref dialog);
            if (hr < 0 && devMode != IntPtr.Zero)
            {
                // A driver that will not take the preset paper still gets its dialog, with its own defaults.
                GlobalFree(devMode);
                dialog.DevMode = IntPtr.Zero;
                hr = PrintDlgExW(ref dialog);
            }

            if (hr < 0)
            {
                return new PrintOutcome(PrintOutcomeKind.Failed, $"The print dialog could not be shown ({Marshal.GetExceptionForHR(hr)?.Message}). Nothing was printed; use Open to print instead.");
            }

            if (dialog.ResultAction != PdResultPrint)
            {
                return new PrintOutcome(PrintOutcomeKind.Cancelled, "Printing was cancelled; nothing was sent.");
            }

            string printer = DeviceName(dialog.DevNames);
            var chosen = pages;
            if ((dialog.Flags & PdPageNums) != 0)
            {
                var numbers = new SortedSet<int>();
                for (int i = 0; i < dialog.PageRangeCount; i++)
                {
                    int from = Marshal.ReadInt32(ranges, i * 8), to = Marshal.ReadInt32(ranges, (i * 8) + 4);
                    for (int n = Math.Max(1, from); n <= Math.Min(pages.Count, to); n++)
                    {
                        numbers.Add(n);
                    }
                }

                chosen = [.. numbers.Select(n => pages[n - 1])];
            }

            return Print(dialog.Dc, printer, chosen, sheet, output: null);
        }
        finally
        {
            if (dialog.Dc != IntPtr.Zero)
            {
                DeleteDC(dialog.Dc);
            }

            foreach (var handle in new[] { dialog.DevMode, dialog.DevNames })
            {
                if (handle != IntPtr.Zero)
                {
                    GlobalFree(handle);
                }
            }

            Marshal.FreeHGlobal(ranges);
        }
    }

    /// <summary>
    /// Prints to a named printer with no dialog, on the sheet's paper when it is a known size, into <paramref name="output"/> for a printer
    /// that writes a file. The size test prints this way to "Microsoft Print to PDF".
    /// </summary>
    public static PrintOutcome PrintTo(string printer, IReadOnlyList<Scene> pages, string sheet, string? output)
    {
        ArgumentNullException.ThrowIfNull(pages);
        IntPtr dc = OpenDc(printer, pages[0]);
        if (dc == IntPtr.Zero)
        {
            return new PrintOutcome(PrintOutcomeKind.Failed, $"{printer} could not be opened for printing.");
        }

        try
        {
            return Print(dc, printer, pages, sheet, output);
        }
        finally
        {
            DeleteDC(dc);
        }
    }

    /// <summary>A printer as its driver describes it on the page's paper, asking it for nothing else and printing nothing; null when it cannot be opened.</summary>
    public static PrinterPage? Describe(string printer, Scene page)
    {
        IntPtr dc = OpenDc(printer, page);
        if (dc == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Describe(dc, printer);
        }
        finally
        {
            DeleteDC(dc);
        }
    }

    /// <summary>A device context for the printer with its own settings, the paper set to the page's when it is a known paper, or zero.</summary>
    private static IntPtr OpenDc(string printer, Scene page)
    {
        if (!OpenPrinterW(printer, out IntPtr handle, IntPtr.Zero))
        {
            return IntPtr.Zero;
        }

        IntPtr devMode = IntPtr.Zero;
        try
        {
            int size = DocumentPropertiesW(IntPtr.Zero, handle, printer, IntPtr.Zero, IntPtr.Zero, 0);
            if (size > 0)
            {
                devMode = Marshal.AllocHGlobal(size);
                DocumentPropertiesW(IntPtr.Zero, handle, printer, devMode, IntPtr.Zero, DmOutBuffer);
                if (PaperCode(page) is short paper)
                {
                    Marshal.WriteInt32(devMode, DmFieldsOffset, Marshal.ReadInt32(devMode, DmFieldsOffset) | DmOrientation | DmPaperSize);
                    Marshal.WriteInt16(devMode, DmOrientationOffset, DmOrientPortrait);
                    Marshal.WriteInt16(devMode, DmPaperSizeOffset, paper);
                    DocumentPropertiesW(IntPtr.Zero, handle, printer, devMode, devMode, DmInBuffer | DmOutBuffer);
                }
            }

            return CreateDCW(null, printer, null, devMode);
        }
        finally
        {
            if (devMode != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(devMode);
            }

            ClosePrinter(handle);
        }
    }

    /// <summary>Whether a printer of this name is installed.</summary>
    public static bool Installed(string printer)
    {
        if (!OpenPrinterW(printer, out IntPtr handle, IntPtr.Zero))
        {
            return false;
        }

        ClosePrinter(handle);
        return true;
    }

    /// <summary>The printer as its driver describes it, from the device context the dialog or <see cref="PrintTo"/> made.</summary>
    public static PrinterPage Describe(IntPtr dc, string printer) => new(
        printer,
        GetDeviceCaps(dc, LogPixelsX), GetDeviceCaps(dc, LogPixelsY),
        GetDeviceCaps(dc, PhysicalWidth), GetDeviceCaps(dc, PhysicalHeight),
        GetDeviceCaps(dc, PhysicalOffsetX), GetDeviceCaps(dc, PhysicalOffsetY),
        GetDeviceCaps(dc, HorzRes), GetDeviceCaps(dc, VertRes));

    /// <summary>Checks the pages against the printer, and draws them only when every one prints at actual size.</summary>
    private static PrintOutcome Print(IntPtr dc, string printer, IReadOnlyList<Scene> pages, string sheet, string? output)
    {
        var described = Describe(dc, printer);
        if (PrintFit.Refusal(pages, described) is { } refusal)
        {
            return new PrintOutcome(PrintOutcomeKind.Refused, refusal, printer, pages.Count);
        }

        IntPtr name = Marshal.StringToHGlobalUni(sheet), file = output is null ? IntPtr.Zero : Marshal.StringToHGlobalUni(output);
        var fonts = new Dictionary<long, IntPtr>();
        try
        {
            var doc = new DocInfo { Size = Marshal.SizeOf<DocInfo>(), DocName = name, Output = file };
            if (StartDocW(dc, ref doc) <= 0)
            {
                return Failure(printer, pages.Count, "the print job could not be started");
            }

            var mapping = PrintFit.Mapping(described);
            foreach (var page in pages)
            {
                if (StartPage(dc) <= 0)
                {
                    AbortDoc(dc);
                    return Failure(printer, pages.Count, "a page could not be started");
                }

                // Entry 114 section 1: a page the driver would not draw in full is never committed. A sheet with a marker missing looks
                // normal and cannot be measured, and that is found only when it comes back from the range.
                int failed = Draw(dc, mapping, page, fonts);
                if (failed > 0)
                {
                    AbortDoc(dc);
                    return new PrintOutcome(PrintOutcomeKind.Failed,
                        $"{printer} would not draw {failed} of the sheet's {page.Items.Count} marks, so nothing was printed: a sheet missing any of them cannot be measured. Use Save PDF and print from your viewer.",
                        printer, pages.Count);
                }

                if (EndPage(dc) <= 0)
                {
                    AbortDoc(dc);
                    return Failure(printer, pages.Count, "a page could not be finished");
                }
            }

            if (EndDoc(dc) <= 0)
            {
                return Failure(printer, pages.Count, "the print job could not be finished");
            }

            return new PrintOutcome(PrintOutcomeKind.Sent, PrintFit.Confirmation(printer, sheet, pages.Count), printer, pages.Count);
        }
        finally
        {
            foreach (var font in fonts.Values)
            {
                DeleteObject(font);
            }

            Marshal.FreeHGlobal(name);
            if (file != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(file);
            }
        }
    }

    private static PrintOutcome Failure(string printer, int pages, string what) => new(PrintOutcomeKind.Failed,
        $"{printer} refused the job: {what} ({new Win32Exception(Marshal.GetLastPInvokeError()).Message}). Nothing may have printed; check the print queue.", printer, pages);

    /// <summary>
    /// One page, every item in scene order as the PDF writer draws it: rectangles and disc bands filled as closed paths, text in Arial, the
    /// metric match of the Helvetica the PDF names, set on its baseline with the same anchor. The mapping is set after StartPage, since a
    /// driver may reset the device context there. It returns how many items the driver would not draw, which is zero on a good page.
    /// <para>
    /// <b>Every filled shape is a path, NOTES-FROM-PLANNING.md entry 114 section 1.</b> The rectangles were drawn with <c>FillRect</c>, which is
    /// a pattern blit rather than a drawing call. Microsoft Print to PDF honoured it; the Brother MFC-J430W driver dropped every one, so its
    /// sheets printed with no markers, no codes and no load block rules, and could not be measured at all. Both drivers report the same
    /// <c>RASTERCAPS</c>, blits included, so the capability bits do not tell the two apart. A closed path filled with <c>FillPath</c> is what
    /// every driver honours, and it is what the disc bands always used, which is why the rings printed while the rectangles did not.
    /// </para>
    /// </summary>
    private static int Draw(IntPtr dc, GdiMapping mapping, Scene page, Dictionary<long, IntPtr> fonts)
    {
        SetGraphicsMode(dc, GmAdvanced);
        SetMapMode(dc, MmAnisotropic);
        SetWindowExtEx(dc, mapping.WindowExtX, mapping.WindowExtY, IntPtr.Zero);
        SetViewportExtEx(dc, mapping.ViewportExtX, mapping.ViewportExtY, IntPtr.Zero);
        SetViewportOrgEx(dc, mapping.ViewportOrgX, mapping.ViewportOrgY, IntPtr.Zero);
        IntPtr brush = GetStockObject(DcBrush);
        SelectObject(dc, brush);
        SelectObject(dc, GetStockObject(NullPen));
        SetPolyFillMode(dc, Alternate);
        SetBkMode(dc, Transparent);
        int failed = 0;
        foreach (var item in page.Items)
        {
            uint colour = ColourRef(item.Colour);
            switch (item)
            {
                case RectFill r:
                    SetDCBrushColor(dc, colour);
                    int left = (int)r.X, top = (int)r.Y, right = (int)(r.X + r.Width), bottom = (int)(r.Y + r.Height);
                    var corners = new[] { new PointL(left, top), new PointL(right, top), new PointL(right, bottom), new PointL(left, bottom) };
                    bool drawn = BeginPath(dc) && Polygon(dc, corners, corners.Length) && EndPath(dc) && FillPath(dc);
                    if (!drawn)
                    {
                        failed++;
                    }

                    break;
                case DiscBand d:
                    SetDCBrushColor(dc, colour);
                    bool band = BeginPath(dc)
                        && Ellipse(dc, (int)(d.CentreX - d.OuterRadius), (int)(d.CentreY - d.OuterRadius), (int)(d.CentreX + d.OuterRadius), (int)(d.CentreY + d.OuterRadius))
                        && (d.InnerRadius <= 0 || Ellipse(dc, (int)(d.CentreX - d.InnerRadius), (int)(d.CentreY - d.InnerRadius), (int)(d.CentreX + d.InnerRadius), (int)(d.CentreY + d.InnerRadius)))
                        && EndPath(dc)
                        && FillPath(dc);
                    if (!band)
                    {
                        failed++;
                    }

                    break;
                case TextRun t:
                    if (!fonts.TryGetValue(t.FontSize, out IntPtr font))
                    {
                        // A negative height is the em size, which is what a PDF font size is.
                        font = CreateFontW(-(int)t.FontSize, 0, 0, 0, 400, 0, 0, 0, DefaultCharset, OutTtOnlyPrecis, 0, 0, 0, "Arial");
                        fonts[t.FontSize] = font;
                    }

                    SelectObject(dc, font);
                    SetTextColor(dc, colour);
                    SetTextAlign(dc, TaBaseline | t.Anchor switch { TextAnchor.Centre => TaCenter, TextAnchor.Right => TaRight, _ => TaLeft });
                    if (!TextOutW(dc, (int)t.X, (int)t.Baseline, t.Text, t.Text.Length))
                    {
                        failed++;
                    }

                    break;
            }
        }

        return failed;
    }

    private static uint ColourRef(Rgb c) => c.R | ((uint)c.G << 8) | ((uint)c.B << 16);

    /// <summary>The Windows paper code of a page that is a known paper in portrait, or null.</summary>
    private static short? PaperCode(Scene page)
    {
        foreach (var (code, width, height) in new (short, long, long)[] { (1, 4318, 5588), (9, 4200, 5940), (5, 4318, 7112), (3, 5588, 8636), (8, 5940, 8400) })
        {
            if (Math.Abs(page.Width - width) <= PrintFit.PaperTolerance && Math.Abs(page.Height - height) <= PrintFit.PaperTolerance)
            {
                return code;
            }
        }

        return null;
    }

    /// <summary>A movable DEVMODE asking only for the sheet's paper, portrait, which the dialog merges with the chosen printer's own.</summary>
    private static IntPtr PresetDevMode(Scene page)
    {
        if (PaperCode(page) is not short paper)
        {
            return IntPtr.Zero;
        }

        IntPtr handle = GlobalAlloc(GMemMoveable | GMemZeroInit, (nuint)DevModeSize);
        IntPtr p = GlobalLock(handle);
        Marshal.WriteInt16(p, DmSpecVersionOffset, DmSpecVersion);
        Marshal.WriteInt16(p, DmSizeOffset, DevModeSize);
        Marshal.WriteInt32(p, DmFieldsOffset, DmOrientation | DmPaperSize);
        Marshal.WriteInt16(p, DmOrientationOffset, DmOrientPortrait);
        Marshal.WriteInt16(p, DmPaperSizeOffset, paper);
        GlobalUnlock(handle);
        return handle;
    }

    private static string DeviceName(IntPtr devNames)
    {
        IntPtr p = GlobalLock(devNames);
        try
        {
            return Marshal.PtrToStringUni(p + (Marshal.ReadInt16(p, 2) * 2)) ?? "the printer";
        }
        finally
        {
            GlobalUnlock(devNames);
        }
    }

    private const uint PdReturnDc = 0x100, PdNoSelection = 0x4, PdNoPageNums = 0x8, PdPageNums = 0x2, PdUseDevModeCopiesAndCollate = 0x40000;
    private const uint PdHidePrintToFile = 0x100000, PdNoCurrentPage = 0x800000, StartPageGeneral = 0xFFFFFFFF, PdResultPrint = 1, MaxRanges = 16;
    private const int LogPixelsX = 88, LogPixelsY = 90, PhysicalWidth = 110, PhysicalHeight = 111, PhysicalOffsetX = 112, PhysicalOffsetY = 113;
    private const int HorzRes = 8, VertRes = 10, MmAnisotropic = 8, GmAdvanced = 2, DcBrush = 18, NullPen = 8, Alternate = 1, Transparent = 1;
    private const uint TaBaseline = 24, TaLeft = 0, TaCenter = 6, TaRight = 2, DefaultCharset = 1, OutTtOnlyPrecis = 7;
    private const int DmOutBuffer = 2, DmInBuffer = 8, DmOrientation = 0x1, DmPaperSize = 0x2, DmFieldsOffset = 72, DmOrientationOffset = 76;
    private const int DmPaperSizeOffset = 78, DmSpecVersionOffset = 64, DmSizeOffset = 68;
    private const short DmOrientPortrait = 1, DmSpecVersion = 0x0401, DevModeSize = 220;
    private const uint GMemMoveable = 0x2, GMemZeroInit = 0x40;

    [StructLayout(LayoutKind.Sequential)]
    private struct PrintDlgEx
    {
        public uint StructSize;
        public IntPtr Owner;
        public IntPtr DevMode;
        public IntPtr DevNames;
        public IntPtr Dc;
        public uint Flags;
        public uint Flags2;
        public uint ExclusionFlags;
        public uint PageRangeCount;
        public uint MaxPageRanges;
        public IntPtr PageRanges;
        public uint MinPage;
        public uint MaxPage;
        public uint Copies;
        public IntPtr Instance;
        public IntPtr PrintTemplateName;
        public IntPtr Callback;
        public uint PropertyPageCount;
        public IntPtr PropertyPages;
        public uint StartPage;
        public uint ResultAction;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DocInfo
    {
        public int Size;
        public IntPtr DocName;
        public IntPtr Output;
        public IntPtr DataType;
        public uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PointL(int x, int y)
    {
        public readonly int X = x;
        public readonly int Y = y;
    }

    [DllImport("comdlg32.dll", ExactSpelling = true)]
    private static extern int PrintDlgExW(ref PrintDlgEx dialog);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenPrinterW(string name, out IntPtr printer, IntPtr defaults);


    [DllImport("winspool.drv", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClosePrinter(IntPtr printer);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int DocumentPropertiesW(IntPtr window, IntPtr printer, string device, IntPtr output, IntPtr input, int mode);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr CreateDCW(string? driver, string device, string? output, IntPtr devMode);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int GetDeviceCaps(IntPtr dc, int index);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int StartDocW(IntPtr dc, ref DocInfo doc);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int StartPage(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int EndPage(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int EndDoc(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int AbortDoc(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int SetGraphicsMode(IntPtr dc, int mode);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int SetMapMode(IntPtr dc, int mode);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowExtEx(IntPtr dc, int x, int y, IntPtr previous);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetViewportExtEx(IntPtr dc, int x, int y, IntPtr previous);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetViewportOrgEx(IntPtr dc, int x, int y, IntPtr previous);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern IntPtr GetStockObject(int index);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern IntPtr SelectObject(IntPtr dc, IntPtr gdiObject);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr gdiObject);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern uint SetDCBrushColor(IntPtr dc, uint colour);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Polygon(IntPtr dc, PointL[] points, int count);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int SetPolyFillMode(IntPtr dc, int mode);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BeginPath(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndPath(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FillPath(IntPtr dc);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Ellipse(IntPtr dc, int left, int top, int right, int bottom);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline,
        uint strikeOut, uint charSet, uint outputPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern uint SetTextColor(IntPtr dc, uint colour);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int SetBkMode(IntPtr dc, int mode);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern uint SetTextAlign(IntPtr dc, uint align);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TextOutW(IntPtr dc, int x, int y, string text, int length);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GlobalAlloc(uint flags, nuint bytes);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GlobalLock(IntPtr memory);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr memory);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GlobalFree(IntPtr memory);
}
