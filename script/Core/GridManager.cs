#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject[] tilePrefabs;
    public GridController controller;
    public GameObject unitPrefab;

    public int width = 8;
    public int height = 8;
    public float tileWidth = 1f;
    public float tileHeight = 1f;

    void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        GenerateGrid(); // 이제 실행 중일 때만 생성됨
    }


    public void GenerateGrid()
    {
        Transform tileParent = new GameObject("TileParent").transform;
        tileParent.parent = this.transform;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(
                    (x - y) * tileWidth * 0.70f,
                    (x + y) * tileHeight * 0.70f,
                    0f
                );

                int randomIndex = Random.Range(0, tilePrefabs.Length);
                GameObject tile = Instantiate(tilePrefabs[randomIndex], pos, Quaternion.Euler(0, 0, -45), tileParent);
                tile.name = $"Tile_{x}_{y}";

                Tile tileScript = tile.GetComponent<Tile>();
                tileScript.gridPos = new Vector2Int(x, y);
                tileScript.controller = controller;

                if (x == 0 && y == 0)
                {
                    Vector3 unitWorldPos = pos + new Vector3(0, 0, -10);
                    GameObject unit = Instantiate(unitPrefab, unitWorldPos, Quaternion.identity);
                    unit.name = "PlayerUnit";
                    controller.playerUnit = unit;
                }
            }
        }
    }
    /*
    #if UNITY_EDITOR
        [ContextMenu("Generate Grid (Editor)")]
        void GenerateGridEditor()
        {
            ClearGrid();
            GenerateGrid(); // 👉 이제 Start()가 아니라 별도 메서드를 호출
        }

        void ClearGrid()
        {
            Transform existing = transform.Find("TileParent");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            var existingUnit = GameObject.Find("PlayerUnit");
            if (existingUnit != null)
            {
                DestroyImmediate(existingUnit);
            }
        }
    #endif
    */
}
