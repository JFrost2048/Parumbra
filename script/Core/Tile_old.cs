using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPos;
    public GridController controller;

    private void OnMouseDown()
    {
        controller?.MoveUnitTo(gridPos);
    }
}
