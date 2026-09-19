using Avalonia.Controls;
using Avalonia.Media;

namespace GroupLab.App.Theme;

/// <summary>
/// The application's icons, NOTES-FROM-PLANNING.md entry 109 section 2: the tools, the view controls, the rail's destinations and the header's
/// overflow, each drawn as a small filled geometry on a 16 pixel square in the foreground colour, so an icon never depends on a font having a
/// glyph for it. A path beginning "F1" fills by the nonzero rule, so a ring is an outer circle one way and an inner circle the other.
/// </summary>
public static class Icons
{
    public const string Pan = "M8,0 L11,3 H9 V7 H13 V5 L16,8 L13,11 V9 H9 V13 H11 L8,16 L5,13 H7 V9 H3 V11 L0,8 L3,5 V7 H7 V3 H5 Z";

    public const string Length = "M0,4 H2 V7 H14 V4 H16 V12 H14 V9 H2 V12 H0 Z";

    public const string Rectangle = "M1,3 H15 V13 H1 Z M2.5,4.5 V11.5 H13.5 V4.5 Z";

    public const string Aim = "F1 M8,2 A6,6 0 1 1 7.99,2 Z M8,3.5 A4.5,4.5 0 1 0 8.01,3.5 Z M7.25,0 H8.75 V5 H7.25 Z M7.25,11 H8.75 V16 H7.25 Z M0,7.25 H5 V8.75 H0 Z M11,7.25 H16 V8.75 H11 Z";

    public const string Impact = "F1 M8,1.5 A6.5,6.5 0 1 1 7.99,1.5 Z M8,3 A5,5 0 1 0 8.01,3 Z M8,5.5 A2.5,2.5 0 1 1 7.99,5.5 Z";

    public const string Select = "M3,1 L13,9 L8.6,9.6 L11.2,14.6 L9.2,15.6 L6.6,10.6 L3,13.6 Z";

    public const string Undo = "M5,2 L0,6.5 L5,11 V8 H10 A3,3 0 0 1 10,14 H7 V16 H10 A5,5 0 0 0 10,6 H5 Z";

    public const string Redo = "M11,2 L16,6.5 L11,11 V8 H6 A3,3 0 0 0 6,14 H9 V16 H6 A5,5 0 0 1 6,6 H11 Z";

    public const string ZoomIn = "F1 M6.5,1 A5.5,5.5 0 1 1 6.49,1 Z M6.5,2.5 A4,4 0 1 0 6.51,2.5 Z M10.6,11.6 L11.6,10.6 L15.5,14.5 L14.5,15.5 Z M5.75,4 H7.25 V5.75 H9 V7.25 H7.25 V9 H5.75 V7.25 H4 V5.75 H5.75 Z";

    public const string ZoomOut = "F1 M6.5,1 A5.5,5.5 0 1 1 6.49,1 Z M6.5,2.5 A4,4 0 1 0 6.51,2.5 Z M10.6,11.6 L11.6,10.6 L15.5,14.5 L14.5,15.5 Z M4,5.75 H9 V7.25 H4 Z";

    public const string Fit = "M1,1 H6 V2.5 H2.5 V6 H1 Z M10,1 H15 V6 H13.5 V2.5 H10 Z M1,10 H2.5 V13.5 H6 V15 H1 Z M13.5,10 H15 V15 H10 V13.5 H13.5 Z";

    public const string RotateLeft = "M8,2 A6,6 0 1 1 2.8,11 L4.1,10.25 A4.5,4.5 0 1 0 8,3.5 V5.5 L4.5,2.75 L8,0 Z";

    public const string RotateRight = "M8,2 A6,6 0 1 0 13.2,11 L11.9,10.25 A4.5,4.5 0 1 1 8,3.5 V5.5 L11.5,2.75 L8,0 Z";

    public const string Settings = "F1 M7,0 H9 V3 H7 Z M7,13 H9 V16 H7 Z M0,7 H3 V9 H0 Z M13,7 H16 V9 H13 Z M2.1,3.5 L3.5,2.1 L5.6,4.2 L4.2,5.6 Z M10.4,11.8 L11.8,10.4 L13.9,12.5 L12.5,13.9 Z M12.5,2.1 L13.9,3.5 L11.8,5.6 L10.4,4.2 Z M4.2,10.4 L5.6,11.8 L3.5,13.9 L2.1,12.5 Z M8,2.5 A5.5,5.5 0 1 1 7.99,2.5 Z M8,5.5 A2.5,2.5 0 1 0 8.01,5.5 Z";

    public const string More = "M3.5,8 A1.5,1.5 0 1 1 3.49,8 Z M9.5,8 A1.5,1.5 0 1 1 9.49,8 Z M15.5,8 A1.5,1.5 0 1 1 15.49,8 Z";

    public const string Print = "M4,1 H12 V5 H4 Z M1,6 H15 V12 H12 V10 H4 V12 H1 Z M5,11 H11 V15 H5 Z";

    public const string Library = "M1,1 H7 V7 H1 Z M9,1 H15 V7 H9 Z M1,9 H7 V15 H1 Z M9,9 H15 V15 H9 Z";

    public const string Records = "M1,2 H15 V4 H1 Z M1,7 H15 V9 H1 Z M1,12 H10 V14 H1 Z";

    public const string Reports = "M1,14 H15 V15.5 H1 Z M2,8 H5 V13 H2 Z M6.5,4 H9.5 V13 H6.5 Z M11,9 H14 V13 H11 Z";

    /// <summary>A trajectory's arc over the ground, for the Ballistics screen.</summary>
    public const string Ballistics = "M1,5 C6,1.5 11,3 15,11.5 L13.6,12.2 C10.2,4.9 6,3.4 1.6,6.4 Z M1,14 H15 V15.5 H1 Z";

    /// <summary>An icon as a control, at 16 pixels, drawn in the foreground of whatever holds it.</summary>
    public static PathIcon Draw(string geometry, double size = 16) => new() { Data = StreamGeometry.Parse(geometry), Width = size, Height = size };
}
