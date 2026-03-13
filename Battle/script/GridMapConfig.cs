using UnityEngine;

[CreateAssetMenu(menuName = "Tactics/Grid Map Config", fileName = "GridMapConfig")]
public class GridMapConfig : ScriptableObject
{
    [Header("Grid Size")]
    [Min(1)] public int columns = 10;
    [Min(1)] public int rows = 10;

    [Header("Tile Prefab")]
    public GameObject tilePrefab;

    [Header("Tile Placement")]
    public Vector2 tileSize = new Vector2(1f, 1f);   // 한 타일의 가로/세로 간격(월드 단위)
    public Vector2 spacing = Vector2.zero;          // 타일 사이 추가 간격
    public bool pivotIsCenter = true;               // 프리팹 피벗이 타일 중앙인지?

    [Header("Transform")]
    public Vector3 origin = Vector3.zero;           // 맵 시작 위치
    public float uniformScale = 1f;                 // 타일 프리팹 일괄 스케일

    [Header("Optional: Height / Randomness")]
    public bool randomizeHeight = false;
    public float heightRange = 0.2f;
    public int randomSeed = 1234;
}
