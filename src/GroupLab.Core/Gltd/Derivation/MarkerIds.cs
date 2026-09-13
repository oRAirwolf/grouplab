using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

public sealed record MarkerAssignment(IReadOnlyList<Marker> Markers, int AssemblyCount, bool Wrapped);

/// <summary>
/// Marker identifiers, TARGET-SCHEMA.md section 3.7: raster order over the assembly lattice starting at 0,
/// repeating modulo the dictionary size when an assembly needs more, with the tile index disambiguating.
/// </summary>
public static class MarkerIds
{
    public static int DictionarySize(FiducialFamily family) => family switch
    {
        FiducialFamily.Aruco4x4With50 => 50,
        FiducialFamily.Aruco4x4With100 => 100,
        FiducialFamily.Aruco5x5With100 => 100,
        FiducialFamily.Aruco6x6With250 => 250,
        FiducialFamily.AprilTag16h5 => 30,
        FiducialFamily.AprilTag25h9 => 35,
        FiducialFamily.AprilTag36h11 => 587,
        FiducialFamily.AprilTagCircle21h7 => 38,
        _ => 0,
    };

    /// <summary>
    /// Assigns identifiers to one sheet's markers. On a tiled definition the sheet is tile
    /// <paramref name="tileIndex"/>, row-major within the assembly, and identifiers count across every tile.
    /// </summary>
    public static MarkerAssignment Assign(IReadOnlyList<PointDmm> sheetMarkers, Tiling? tiling, int tileIndex, int dictionarySize)
    {
        ArgumentNullException.ThrowIfNull(sheetMarkers);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dictionarySize);
        int cols = tiling?.Cols ?? 1, rows = tiling?.Rows ?? 1;
        ArgumentOutOfRangeException.ThrowIfNegative(tileIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(tileIndex, cols * rows);

        long stepX = tiling is null ? 0 : tiling.SheetWidth - tiling.Overlap;
        long stepY = tiling is null ? 0 : tiling.SheetHeight - tiling.Overlap;
        var assembly = new List<(long Y, long X, int Tile, PointDmm Local)>(sheetMarkers.Count * cols * rows);
        for (int tile = 0; tile < cols * rows; tile++)
        {
            long offsetX = (tile % cols) * stepX, offsetY = (tile / cols) * stepY;
            assembly.AddRange(sheetMarkers.Select(p => (offsetY + p.Y, offsetX + p.X, tile, p)));
        }

        var ordered = assembly.OrderBy(m => m.Y).ThenBy(m => m.X).ToList();
        var markers = new List<Marker>(sheetMarkers.Count);
        for (int index = 0; index < ordered.Count; index++)
        {
            if (ordered[index].Tile == tileIndex)
            {
                markers.Add(new Marker(index % dictionarySize, ordered[index].Local.X, ordered[index].Local.Y));
            }
        }

        return new MarkerAssignment(markers, ordered.Count, ordered.Count > dictionarySize);
    }
}
