using UnityEngine;

namespace TacticsGrid
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GridMapGenerator generator;
        [SerializeField] private Transform unitsParent;

        [Header("Enemy")]
        [SerializeField] private Unit enemyPrefab;
        [SerializeField] private Vector2Int spawnCoord = new Vector2Int(3, 3);
        [SerializeField] private float enemyYOffset = 0.5f;

        [Tooltip("Enemy에 오프셋이 없을 때 fallback(거의 안 씀).")]
        [SerializeField] private Vector3 defaultEnemyTileOffset = new Vector3(0f, 0f, -0.5f);

        [Tooltip("Enemy 오프셋 적용 방식 fallback(거의 안 씀).")]
        [SerializeField] private bool defaultOffsetInTileSpace = false;

        [Tooltip("Enemy 생성 시 회전값(각도 오프셋)")]
        [SerializeField] private Vector3 enemyEulerRotation = new Vector3(30f, 0f, 0f);

        private Unit spawnedEnemy;

        private void Start()
        {
            SpawnOrSnapEnemy(spawnCoord);
        }

        // GridController의 FindBestGeneratorFor랑 동일한 의도
        private GridMapGenerator FindBestGeneratorFor(Vector2Int coord)
        {
            var gens = FindObjectsByType<GridMapGenerator>(FindObjectsSortMode.None);

            GridMapGenerator fallback = null;

            foreach (var g in gens)
            {
                if (g == null) continue;

                if (g.TryGetTile(coord, out _))
                    return g;

                if (fallback == null) fallback = g;
            }

            return fallback;
        }

        private Vector3 ComputeUnitWorldPos(Vector2Int coord, Vector3 tileWorldPos, Unit unit)
        {
            // Unit에 세팅된 값 우선
            Vector3 offset = (unit != null) ? unit.TileOffset : defaultEnemyTileOffset;
            bool useTileSpace = (unit != null) ? unit.OffsetInTileSpace : defaultOffsetInTileSpace;

            if (!useTileSpace)
                return tileWorldPos + offset;

            // 타일 로컬축 기준 오프셋 적용
            if (generator != null && generator.TryGetTile(coord, out var tile) && tile != null)
                return tile.transform.TransformPoint(offset);

            return tileWorldPos + offset;
        }

        private void ApplyOccupancyForSpawn(Unit unit, Vector2Int coord, bool enter)
        {
            if (unit == null || generator == null) return;
            if (!generator.TryGetTile(coord, out var tile) || tile == null) return;

            int delta = enter ? 1 : -1;

            switch (unit.occupyRule)
            {
                case Unit.OccupyRule.BlockAndOccupy:
                    tile.AddOccupy(delta);
                    tile.AddBlock(delta);
                    break;

                case Unit.OccupyRule.OccupyButNoBlock:
                    tile.AddOccupy(delta);
                    break;

                case Unit.OccupyRule.NoOccupyNoBlock:
                    break;
            }
        }


        public void SpawnOrSnapEnemy(Vector2Int coord)
        {
            if (generator == null)
                generator = FindBestGeneratorFor(coord);

            if (generator == null)
            {
                Debug.LogError("[EnemySpawner] GridMapGenerator를 찾지 못했음. generator 슬롯에 연결해줘.");
                return;
            }

            if (enemyPrefab == null)
            {
                Debug.LogError("[EnemySpawner] enemyPrefab이 비어있음");
                return;
            }

            // 타일 월드 위치 (GridController와 동일)
            if (!generator.TryGetTileWorldPos(coord, out var tileWorldPos, enemyYOffset))
            {
                Debug.LogError($"[EnemySpawner] Spawn failed. Tile not found: {coord} (gen={generator.name})");
                return;
            }

            // 타일 점유 체크 (네 Tile 기준 Walkable/Occupied)
            if (generator.TryGetTile(coord, out var tile) && tile != null)
            {
                if (!tile.Walkable || tile.BlocksMovement)
                {
                    Debug.LogWarning($"[EnemySpawner] Blocked tile: {coord} walkable={tile.Walkable} occupied={tile.Occupied}");
                    return;
                }
            }

            if (spawnedEnemy == null)
            {
                var rot = Quaternion.Euler(enemyEulerRotation);   // ✅ 각도 오프셋 적용
                spawnedEnemy = Instantiate(enemyPrefab, tileWorldPos, rot, unitsParent);
            }

            var unitWorldPos = ComputeUnitWorldPos(coord, tileWorldPos, spawnedEnemy);
            spawnedEnemy.SnapTo(coord, unitWorldPos);

            // 점유 처리
            if (generator.TryGetTile(coord, out var t) && t != null)
                ApplyOccupancyForSpawn(spawnedEnemy, coord, enter: true);

        }
    }
}
