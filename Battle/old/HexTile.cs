using UnityEngine;

public enum CoverType { None, Half, Full }
public enum TileKind { Floor, Wall }

[RequireComponent(typeof(Collider))]
public class HexTile : MonoBehaviour
{
    [Header("Data")]
    public HexCoord coord;
    public TileKind kind = TileKind.Floor;
    public CoverType cover = CoverType.None;

    [Header("Refs")]
    [SerializeField] private Renderer rend;

    private Color baseColor;
    private static readonly int ColorProp = Shader.PropertyToID("_BaseColor"); // URP Lit
    private MaterialPropertyBlock mpb;

    void Awake()
    {
        if (!rend) rend = GetComponentInChildren<Renderer>();
        baseColor = rend ? rend.sharedMaterial.color : Color.white;
        mpb = new MaterialPropertyBlock();
        ApplyColor(baseColor);
    }

    public void ApplyColor(Color c)
    {
        if (!rend) return;
        rend.GetPropertyBlock(mpb);
        // URP Lit uses _BaseColor, Standard uses _Color. We'll set both safely.
        mpb.SetColor(ColorProp, c);
        mpb.SetColor("_Color", c);
        rend.SetPropertyBlock(mpb);
    }

    public void SetSelected(bool selected)
    {
        ApplyColor(selected ? Color.yellow : baseColor);
    }

    public void SetHighlight(Color c)
    {
        ApplyColor(c);
    }

    public void ResetVisual()
    {
        ApplyColor(baseColor);
    }
}
