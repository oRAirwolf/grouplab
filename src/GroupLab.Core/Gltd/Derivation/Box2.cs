namespace GroupLab.Core.Gltd.Derivation;

/// <summary>
/// An axis-aligned box in doubled dmm. A disc of odd diameter or a centred footprint of odd width has
/// half-dmm edges; doubling keeps every edge an integer, so no comparison ever rounds.
/// </summary>
public readonly record struct Box2(long X0, long Y0, long X1, long Y1)
{
    public static Box2 Centred(long centreX, long centreY, long width, long height) =>
        new((2 * centreX) - width, (2 * centreY) - height, (2 * centreX) + width, (2 * centreY) + height);

    public static Box2 Square(long centreX, long centreY, long side) => Centred(centreX, centreY, side, side);

    public static Box2 FromRect(long x, long y, long width, long height) =>
        new(2 * x, 2 * y, 2 * (x + width), 2 * (y + height));

    /// <summary>
    /// The overlap test of <c>tools/layout/layout.py</c> <c>rects_overlap</c>: boxes closer than
    /// <paramref name="gap"/> dmm count as overlapping, and boxes that only touch do not at a gap of 0.
    /// </summary>
    public bool Overlaps(Box2 other, long gap = 0) =>
        !(X1 + (2 * gap) <= other.X0 || other.X1 + (2 * gap) <= X0 || Y1 + (2 * gap) <= other.Y0 || other.Y1 + (2 * gap) <= Y0);

    public bool Within(long width, long height) => X0 >= 0 && Y0 >= 0 && X1 <= 2 * width && Y1 <= 2 * height;
}
