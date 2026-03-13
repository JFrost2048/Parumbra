using System.Collections.Generic;
using UnityEngine;


namespace TacticsGrid
{
    [ExecuteAlways]
    public class GridMapGenerator : MonoBehaviour
    {
        [Header("Config")]
        public GridMapConfig config;

        [Header("Generated Tiles Parent (auto)")]
        [SerializeField] private Transform tilesParent;


        // ✅ 단일 진실: 좌표 -> Tile 컴포넌트
        private readonly Dictionary<Vector2Int, Tile> tileMap = new();

        private void Start()
        {
            EnsureParent();
            RebuildFromChildren();
        }

        private void EnsureParent()
        {
            if (tilesParent != null) return;

            var t = transform.Find("Tiles");
            if (t != null) tilesParent = t;
        }

        // ✅ 핵심: 자식들의 이름(Tile_x_y)을 파싱해서 tileMap을 확정적으로 구성
        [ContextMenu("Rebuild From Children")]
        public void RebuildFromChildren()
        {
            tileMap.Clear();

            if (tilesParent == null)
            {
                Debug.LogError("[Gen] tilesParent is null. (Tiles 오브젝트를 자식으로 두거나 tilesParent를 연결해줘)");
                return;
            }

            int childCount = tilesParent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var tr = tilesParent.GetChild(i);
                var tile = tr.GetComponent<Tile>();
                if (tile == null) continue;

                if (!TryParseCoord(tr.name, out var c))
                {
                    Debug.LogWarning($"[Gen] coord parse failed: {tr.name}");
                    continue;
                }

                tile.Init(c);          // Coord 세팅 확정
                tileMap[c] = tile;     // ✅ tileMap에 저장
            }

            Debug.Log($"[Gen] RebuildFromChildren tileMap.Count={tileMap.Count} children={childCount}");

            // 디버그: (0,0) 존재 여부 바로 확인
            if (tileMap.ContainsKey(new Vector2Int(0, 0)))
                Debug.Log("[Gen] tileMap has (0,0)");
            else
                Debug.LogWarning("[Gen] tileMap does NOT have (0,0) (좌표 오프셋/파싱 확인 필요)");
        }

        // ✅ GridController가 사용할 “공식 API”
        public bool TryGetTile(Vector2Int coord, out Tile tile)
        {
            // 혹시 Start 이전 호출 대비: tileMap 비었으면 한 번 빌드 시도
            if (tileMap.Count == 0) RebuildFromChildren();

            return tileMap.TryGetValue(coord, out tile) && tile != null;
        }

        public bool TryGetTileWorldPos(Vector2Int coord, out Vector3 worldPos, float yOffset = 0f)
        {
            worldPos = default;

            if (!TryGetTile(coord, out var tile))
                return false;

            worldPos = tile.transform.position;
            worldPos.y += yOffset;
            return true;
        }

        // ===== util =====

        private bool TryParseCoord(string name, out Vector2Int coord)
        {
            // 기대 포맷: "Tile_x_y"
            coord = default;
            if (string.IsNullOrEmpty(name)) return false;

            // "Tile_4_3"
            var parts = name.Split('_');
            if (parts.Length < 3) return false;

            if (!int.TryParse(parts[1], out int x)) return false;
            if (!int.TryParse(parts[2], out int y)) return false;

            coord = new Vector2Int(x, y);
            return true;
        }

        // (x,y) -> tile transform
        private readonly Dictionary<Vector2Int, Transform> tiles = new Dictionary<Vector2Int, Transform>();

        public GridMapConfig Config => config;

        public void GenerateOrResize()
        {

            if (config == null)
            {
                Debug.LogWarning("[GridMapGenerator] Config is null.");
                return;
            }
            if (config.tilePrefab == null)
            {
                Debug.LogWarning("[GridMapGenerator] Tile Prefab is null.");
                return;
            }

            EnsureParent();

            // 필요한 타일 생성/유지
            var need = new HashSet<Vector2Int>();
            for (int y = 0; y < config.rows; y++)
                for (int x = 0; x < config.columns; x++)
                    need.Add(new Vector2Int(x, y));

            // 1) 없어야 하는 타일 제거
            var toRemove = new List<Vector2Int>();
            foreach (var kv in tiles)
            {
                if (!need.Contains(kv.Key))
                    toRemove.Add(kv.Key);
            }

            foreach (var key in toRemove)
            {
                if (tiles.TryGetValue(key, out var tr) && tr != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(tr.gameObject);
                    else Destroy(tr.gameObject);
#else
                Destroy(tr.gameObject);
#endif
                }
                tiles.Remove(key);
            }

            // 2) 필요한 타일은 생성 또는 위치 갱신
            var rng = new System.Random(config.randomSeed);

            for (int y = 0; y < config.rows; y++)
            {
                for (int x = 0; x < config.columns; x++)
                {
                    var key = new Vector2Int(x, y);

                    if (!tiles.TryGetValue(key, out var tileTr) || tileTr == null)
                    {
                        var go = InstantiatePrefab(config.tilePrefab, tilesParent);
                        go.name = $"Tile_{x}_{y}";
                        tileTr = go.transform;
                        tiles[key] = tileTr;
                    }

                    var tile = tileTr.GetComponent<TacticsGrid.Tile>();
                    if (tile == null)
                        tile = tileTr.gameObject.AddComponent<TacticsGrid.Tile>();

                    tile.Init(key);

                    // 위치 계산
                    Vector3 pos = ComputeTilePosition(x, y);

                    if (config.randomizeHeight)
                    {
                        // -range ~ +range
                        float h = (float)(rng.NextDouble() * 2.0 - 1.0) * config.heightRange;
                        pos.y += h;
                    }

                    tileTr.position = pos;
                    tileTr.localScale = Vector3.one * Mathf.Max(0.0001f, config.uniformScale);

                    // 타일이 어떤 좌표인지 컴포넌트로 저장하고 싶으면 여기서 붙이면 됨
                    // e.g., tileTr.GetComponent<Tile>()?.Init(key);
                }
            }
        }
        public void Clear()
        {
            if (tilesParent != null)
            {
                for (int i = tilesParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(tilesParent.GetChild(i).gameObject);
                }
            }
            tiles.Clear();
        }


        public void ClearAll()
        {
            EnsureParent();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = tilesParent.childCount - 1; i >= 0; i--)
                    DestroyImmediate(tilesParent.GetChild(i).gameObject);
            }
            else
            {
                for (int i = tilesParent.childCount - 1; i >= 0; i--)
                    Destroy(tilesParent.GetChild(i).gameObject);
            }
#else
        for (int i = tilesParent.childCount - 1; i >= 0; i--)
            Destroy(tilesParent.GetChild(i).gameObject);
#endif

            tiles.Clear();
        }

        private Vector3 ComputeTilePosition(int x, int y)
        {
            float stepX = config.tileSize.x + config.spacing.x;
            float stepZ = config.tileSize.y + config.spacing.y;

            // pivot이 center가 아니고 "좌하단"이라면 한 칸 절반 보정(원하는 방식에 맞게)
            float pivotOffsetX = config.pivotIsCenter ? 0f : (stepX * 0.5f);
            float pivotOffsetZ = config.pivotIsCenter ? 0f : (stepZ * 0.5f);

            return config.origin + new Vector3(x * stepX + pivotOffsetX, 0f, y * stepZ + pivotOffsetZ);
        }


        private GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Prefab 연결 유지(에디터에서 생성)
                var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
                return go != null ? go : Instantiate(prefab, parent);
            }
#endif
            return Instantiate(prefab, parent);
        }


        public Dictionary<Vector2Int, Tile> GetTileMap()
        {
            var map = new Dictionary<Vector2Int, Tile>();
            foreach (var kv in tiles)
            {
                if (kv.Value == null) continue;
                var tile = kv.Value.GetComponent<Tile>();
                if (tile != null)
                    map[kv.Key] = tile;
            }
            return map;
        }



    }
}
