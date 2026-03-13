using UnityEngine;

public class Unit : MonoBehaviour
{
    public Vector2Int gridPos;
    
    public void MoveTo(Vector2Int newGridPos)
    {
        gridPos = newGridPos;
        transform.position = new Vector3(gridPos.x, gridPos.y, 0); // 타일 위치로 이동
    }
}
