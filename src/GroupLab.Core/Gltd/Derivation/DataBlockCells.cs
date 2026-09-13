using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

/// <summary>A field cell or the reserved square of a data block, in dmm from the page top-left (TARGET-SCHEMA.md section 3.10).</summary>
public sealed record DataFieldCell(string Key, int X, int Y, int Width, int Height);

/// <summary>Field cells in row-major field order, and the reserved square, which is absent when <c>reserve</c> is 0.</summary>
public sealed record DataBlockGeometry(IReadOnlyList<DataFieldCell> Cells, DataFieldCell? Reserve);

/// <summary>The derived data block layouts of TARGET-SCHEMA.md section 3.10.</summary>
public static class DataBlockCells
{
    /// <summary>Fixed by <c>fields-3x3-1</c> and <c>fields-3x2-1</c> between the fields and the reserved square.</summary>
    public const int Gap = 20;

    /// <summary>Padding above and below the rows, docs/SPEC-ERRATA.md C6.</summary>
    public const int Padding = 5;

    public const int Columns = 3;

    public static DataBlockGeometry Derive(DataBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        int rows = block.Layout switch
        {
            DataBlockLayout.Fields3x3 => 3,
            DataBlockLayout.Fields3x2 => 2,
            _ => throw new ArgumentException("Explicit data block layouts have no derivation.", nameof(block)),
        };

        var keys = InstanceCodec.FieldKeys(block.FieldSet);
        int contentWidth = block.Reserve > 0 ? block.Width - block.Reserve - Gap : block.Width;
        int contentHeight = block.Height - (2 * Padding);

        var cells = new List<DataFieldCell>(rows * Columns);
        for (int r = 0; r < rows; r++)
        {
            int top = block.Y + Padding + DerivedRounding.Boundary(contentHeight, r, rows);
            int bottom = block.Y + Padding + DerivedRounding.Boundary(contentHeight, r + 1, rows);
            for (int c = 0; c < Columns && cells.Count < keys.Count; c++)
            {
                int left = block.X + DerivedRounding.Boundary(contentWidth, c, Columns);
                int right = block.X + DerivedRounding.Boundary(contentWidth, c + 1, Columns);
                cells.Add(new DataFieldCell(keys[cells.Count], left, top, right - left, bottom - top));
            }
        }

        DataFieldCell? reserve = block.Reserve > 0
            ? new DataFieldCell("reserve", block.X + block.Width - block.Reserve,
                block.Y + DerivedRounding.Boundary(block.Height - block.Reserve, 1, 2), block.Reserve, block.Reserve)
            : null;

        return new DataBlockGeometry(cells, reserve);
    }
}
