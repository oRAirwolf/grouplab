namespace GroupLab.Cli.Spike;

/// <summary>
/// The committed Phase 0 sample set, PHASE0-SPIKE-BRIEF.md section 3, with the definition and tile each image shows.
/// <see cref="Sample.Gated"/> marks the images a gate of section 2 applies to: the 600 DPI scan of each of the ten
/// printed sheets, and each photograph that contains the whole sheet. The 300 DPI scans and the rotated rescan are
/// measured and reported, not gated; <see cref="Sample.Excluded"/> names why a photograph is reported but not measured
/// against the gate. DESIGN.md section 21 [r5] splits the photograph gate: <see cref="PhotographGate.Flat"/> is Phase 0's,
/// <see cref="PhotographGate.Mounted"/> is Phase 1's and expected to fail until a surface model exists.
/// </summary>
public static class SampleSet
{
    public const string CentreFire = "GL-CF25-LTR.gltd.json";

    public const string LoadBlock = "GL-CF25-LTR-D.gltd.json";

    public const string Tile = "GL-LR300-T.gltd.json";

    public static IReadOnlyList<Sample> All { get; } =
    [
        new("gl-cf25-ltr-1-600-dpi.png", CentreFire, 0, 600, SampleKind.Scan, true, "sheet 1"),
        new("gl-cf25-ltr-2-600-dpi.png", CentreFire, 0, 600, SampleKind.Scan, true, "sheet 2"),
        new("gl-cf25-ltr-3-600-dpi.png", CentreFire, 0, 600, SampleKind.Scan, true, "sheet 3, the control"),
        new("gl-cf25-ltr-96.2-600-dpi.png", CentreFire, 0, 600, SampleKind.Scan, true, "printed at 96.2 percent"),
        new("gl-cf25-ltr-d-blank-600-dpi.png", LoadBlock, 0, 600, SampleKind.Scan, true, "load block, blank"),
        new("gl-cf25-ltr-d-filled-600-dpi.png", LoadBlock, 0, 600, SampleKind.Scan, true, "load block, filled"),
        new("gl-lr300-t-1-600-dpi.png", Tile, 0, 600, SampleKind.Scan, true, "tile 1"),
        new("gl-lr300-t-2-600-dpi.png", Tile, 1, 600, SampleKind.Scan, true, "tile 2"),
        new("gl-lr300-t-3-600-dpi.png", Tile, 2, 600, SampleKind.Scan, true, "tile 3"),
        new("gl-lr300-t-4-600-dpi.png", Tile, 3, 600, SampleKind.Scan, true, "tile 4"),
        new("gl-cf25-ltr-2-600-dpi-rot180.png", CentreFire, 0, 600, SampleKind.Scan, false, "sheet 2 rotated 180 degrees on the platen"),
        new("gl-cf25-ltr-1-300-dpi.png", CentreFire, 0, 300, SampleKind.Scan, false, "sheet 1"),
        new("gl-cf25-ltr-2-300-dpi.png", CentreFire, 0, 300, SampleKind.Scan, false, "sheet 2"),
        new("gl-cf25-ltr-3-300-dpi.png", CentreFire, 0, 300, SampleKind.Scan, false, "sheet 3, the control"),
        new("gl-cf25-ltr-96.2-300-dpi.png", CentreFire, 0, 300, SampleKind.Scan, false, "printed at 96.2 percent"),
        new("gl-cf25-ltr-d-blank-300-dpi.png", LoadBlock, 0, 300, SampleKind.Scan, false, "load block, blank"),
        new("gl-cf25-ltr-d-filled-300-dpi.png", LoadBlock, 0, 300, SampleKind.Scan, false, "load block, filled"),
        new("gl-lr300-t-1-300-dpi.png", Tile, 0, 300, SampleKind.Scan, false, "tile 1"),
        new("gl-lr300-t-2-300-dpi.png", Tile, 1, 300, SampleKind.Scan, false, "tile 2"),
        new("gl-lr300-t-3-300-dpi.png", Tile, 2, 300, SampleKind.Scan, false, "tile 3"),
        new("gl-lr300-t-4-300-dpi.png", Tile, 3, 300, SampleKind.Scan, false, "tile 4"),
        new("20260913_130543.jpg", CentreFire, 0, null, SampleKind.Photograph, false, "sheet 1 lying loose on a table"),
        new("20260913_130550.jpg", CentreFire, 0, null, SampleKind.Photograph, false, "sheet 1 lying loose on a table"),
        new("20260913_130554.jpg", CentreFire, 0, null, SampleKind.Photograph, false, "sheet 1 lying loose on a table"),
        new("20260913_130559.jpg", CentreFire, 0, null, SampleKind.Photograph, false, "sheet 1 lying loose on a table"),
        new("ultrawide1.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 hanging from a pin", Gate: PhotographGate.Mounted),
        new("ultrawide2.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 hanging from a pin", Gate: PhotographGate.Mounted),
        new("ultrawide3.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 hanging from a pin", Gate: PhotographGate.Mounted),
        new("main1.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 hanging from a pin", Gate: PhotographGate.Mounted),
        new("main2.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 hanging from a pin", Gate: PhotographGate.Mounted),
        new("main3.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 hanging from a pin", Gate: PhotographGate.Mounted),
        new("telephoto1.jpg", CentreFire, 0, null, SampleKind.Photograph, false, "sheet 3 hanging from a pin", "the sheet overflows the frame (NOTES-FROM-PLANNING.md entry 6)", PhotographGate.Mounted),
        new("telephoto2.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 hanging from a pin", Gate: PhotographGate.Mounted),
        new("telephoto3.jpg", CentreFire, 0, null, SampleKind.Photograph, false, "sheet 3 hanging from a pin", "the sheet overflows the frame (NOTES-FROM-PLANNING.md entry 6)", PhotographGate.Mounted),
        new("main_flat1.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 lying flat", Gate: PhotographGate.Flat),
        new("main_flat2.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 lying flat", Gate: PhotographGate.Flat),
        new("main_flat3.jpg", CentreFire, 0, null, SampleKind.Photograph, true, "sheet 3 lying flat", Gate: PhotographGate.Flat),
    ];

    public enum SampleKind
    {
        Scan,
        Photograph,
    }

    /// <summary>Which half of the DESIGN.md section 21 [r5] photograph gate a photograph is measured against.</summary>
    public enum PhotographGate
    {
        None,
        Flat,
        Mounted,
    }

    public sealed record Sample(string File, string Definition, int Tile, int? Dpi, SampleKind Kind, bool Gated, string Description, string? Excluded = null, PhotographGate Gate = PhotographGate.None);
}
