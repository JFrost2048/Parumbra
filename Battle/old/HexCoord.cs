using System;
using UnityEngine;

/// <summary>
/// Axial coords (q, r) for pointy-top hex.
/// </summary>
[Serializable]
public struct HexCoord : IEquatable<HexCoord>
{
    public int q; // column
    public int r; // row

    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public int s => -q - r;

    public bool Equals(HexCoord other) => q == other.q && r == other.r;
    public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
    public override int GetHashCode() => (q, r).GetHashCode();
    public override string ToString() => $"({q},{r})";

    // 6 directions (pointy-top axial)
    private static readonly HexCoord[] dirs =
    {
        new HexCoord(+1,  0),
        new HexCoord(+1, -1),
        new HexCoord( 0, -1),
        new HexCoord(-1,  0),
        new HexCoord(-1, +1),
        new HexCoord( 0, +1),
    };

    public HexCoord Neighbor(int dirIndex)
    {
        dirIndex = (dirIndex % 6 + 6) % 6;
        var d = dirs[dirIndex];
        return new HexCoord(q + d.q, r + d.r);
    }

    public static int Distance(HexCoord a, HexCoord b)
    {
        // cube distance
        return (Mathf.Abs(a.q - b.q) + Mathf.Abs(a.r - b.r) + Mathf.Abs(a.s - b.s)) / 2;
    }
}
