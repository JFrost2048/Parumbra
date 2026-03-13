using UnityEngine;

public class GridController : MonoBehaviour
{
    public GameObject playerUnit;
    public float tileWidth = 1f;
    public float tileHeight = 1f;

    public void MoveUnitTo(Vector2Int gridPos)
    {
        Vector3 worldPos = new Vector3(
            (gridPos.x - gridPos.y) * tileWidth * 0.70f,
            (gridPos.x + gridPos.y) * tileHeight * 0.70f,
            -10f
        );

        if (playerUnit != null)
            playerUnit.transform.position = worldPos;
    }
}
