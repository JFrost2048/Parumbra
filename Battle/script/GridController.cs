using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TacticsGrid.UI;

namespace TacticsGrid
{
    public class GridController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GridMapGenerator generator; // 타일 조회 원천
        [SerializeField] private Transform unitsParent;

        [SerializeField] private TacticsGrid.UI.StandingPanelUI standingUI;

        [Header("Party (Max 3)")]
        [Tooltip("파티 슬롯(최대 3). 비워두면 그 슬롯은 스폰 안 함.")]

        [Header("Unit Placement")]
        [SerializeField] private float unitYOffset = 0.5f;

        [Tooltip("Unit에 오프셋이 없을 때 fallback(거의 안 씀).")]
        [SerializeField] private Vector3 defaultUnitTileOffset = new Vector3(0f, 0f, -0.5f);

        [Tooltip("Unit 오프셋 적용 방식 fallback(거의 안 씀).")]
        [SerializeField] private bool defaultOffsetInTileSpace = false;

        [Tooltip("Unit 생성 시 회전값")]
        [SerializeField] private Vector3 unitEulerRotation = new Vector3(30f, 0f, 0f);

        [SerializeField] private AttackBarUI attackBarUI;

        [Header("Move Tuning (Global)")]
        [SerializeField] private float unitMoveSpeed = 8f;
        public float UnitMoveSpeed => unitMoveSpeed;

        [SerializeField] private TacticsGrid.UI.APIndicatorUI apUI;
        [SerializeField] private TacticsGrid.UI.UnitListUI unitListUI;
        [SerializeField] private TacticsGrid.UI.WeaponPanelUI weaponPanelUI;

        [Header("Party Spawn")]
        [SerializeField] private List<Unit> fallbackPartyPrefabs = new();

        [SerializeField]
        private List<Vector2Int> partySpawnCoords = new()
{
    new Vector2Int(0, 0),
    new Vector2Int(1, 0),
    new Vector2Int(2, 0),
    new Vector2Int(0, 1),
    new Vector2Int(1, 1),
};

        // 호환용(기존 코드에서 Player 참조할 수 있어 유지) : 첫 파티원
        private Unit player;
        public Unit Player => player;

        public bool IsAttackMode => attackMode;

        private Unit selected;
        public Unit Selected => selected;

        // ===== Highlight (Overlay) =====
        private readonly List<Tile> highlighted = new();
        private readonly Color blue = new Color(0.25f, 0.55f, 1f, 0.35f);
        private readonly Color yellow = new Color(1f, 0.85f, 0.2f, 0.35f);
        private readonly Color attackRed = new Color(1f, 0.2f, 0.2f, 0.35f);

        // 공격 모드 상태
        private bool attackMode;
        private HashSet<Vector2Int> attackTiles = new HashSet<Vector2Int>();
        private readonly List<Tile> attackHighlighted = new();

        // 좌표 -> 유닛 레지스트리
        private readonly Dictionary<Vector2Int, TacticsGrid.Unit> unitAt = new();
        // ✅ 유닛이 현재 점유중인 칸(유닛 점유는 1칸만 허용)
        private readonly Dictionary<Unit, Vector2Int> occupiedByUnit = new();


        // 마지막으로 계산한 이동 범위 캐시(오버레이와 동일)
        private Dictionary<Vector2Int, int> moveDist;

        bool CanTraverse(Tile t) => t.Walkable && !t.BlocksTraversal;
        bool CanStop(Tile t) => t.Walkable && !t.BlocksOccupancy;

        private bool battleEnded = false;

        // ✅ 이제 playerUnits = 파티(플레이어 진영 유닛들)로 사용
        private List<Unit> playerUnits = new();

        // 턴 제어
        private int currentUnitIndex = 0;
        [SerializeField] private EnemyTurnManager enemyTurnManager;
        private void Awake()
        {
            if (unitsParent == null)
                Debug.LogWarning("[GridController] unitsParent가 비어있음 (선택 사항이지만 추천)");
        }

        private void Start()
        {
            StartCoroutine(CoInit());
        }

        private void Update()
        {

        }


        private void CheckBattleEnd()
        {
            if (battleEnded) return;

            var players = GetUnitsByFaction(Faction.Player);
            var enemies = GetUnitsByFaction(Faction.Enemy);

            bool playerAllDead = players == null || players.Count == 0;
            bool enemyAllDead = enemies == null || enemies.Count == 0;

            if (!playerAllDead && !enemyAllDead)
                return;

            battleEnded = true;
            SetInputEnabled(false);
            ClearSelectionAndOverlays();

            if (enemyTurnManager != null)
                enemyTurnManager.StopAllCoroutines();

            if (playerAllDead)
            {
                Debug.Log("[Battle] All player units eliminated -> Defeat");
                Debug.Log($"[Battle] RunManager exists? {GameRunManager.Instance != null}");

                if (GameRunManager.Instance != null)
                {
                    Debug.Log($"[Battle] World exists? {GameRunManager.Instance.World != null}");
                    Debug.Log($"[Battle] PendingBattleRoomId = {GameRunManager.Instance.PendingBattleRoomId}");
                    SaveCurrentPartyStateToRunManager();
                    GameRunManager.Instance.EndBattle(BattleResult.PlayerDefeat);
                }
            }
            else if (enemyAllDead)
            {
                Debug.Log("[Battle] All enemy units eliminated -> Victory");
                Debug.Log($"[Battle] RunManager exists? {GameRunManager.Instance != null}");

                if (GameRunManager.Instance != null)
                {
                    Debug.Log($"[Battle] World exists? {GameRunManager.Instance.World != null}");
                    Debug.Log($"[Battle] PendingBattleRoomId = {GameRunManager.Instance.PendingBattleRoomId}");
                    SaveCurrentPartyStateToRunManager();
                    GameRunManager.Instance.EndBattle(BattleResult.PlayerVictory);
                }
            }
        }

        private IEnumerator CoInit()
        {
            yield return null;



            Vector2Int initCoord = (partySpawnCoords != null && partySpawnCoords.Count > 0)
                ? partySpawnCoords[0]
                : new Vector2Int(0, 0);

            if (generator == null)
                generator = FindBestGeneratorFor(initCoord);

            if (generator == null || !generator.TryGetTile(initCoord, out _))
            {
                var fixedGen = FindBestGeneratorFor(initCoord);
                if (fixedGen != null && fixedGen != generator)
                {
                    Debug.LogWarning($"[GridController] Generator 자동 교정: {generator?.name} -> {fixedGen.name}");
                    generator = fixedGen;
                }
            }

            if (generator == null)
            {
                Debug.LogError("[GridController] GridMapGenerator를 찾지 못했음. generator 슬롯에 연결해줘.");
                yield break;
            }
            // ✅ 파티 스폰
            SpawnOrSnapParty();

            // ✅ (중요) 초기화 1회: 타일 카운트 싹 0으로 정리
            ResetAllTilesBlocksOnce();

            // ✅ 레지스트리 재구축(유닛 위치 스캔)
            RebuildUnitRegistryAndOccupancyFromScene();

            // ✅ 초기 점유는 unitAt 기준으로 딱 1번만 적용
            ApplyInitialOccupancyFromRegistry();

            BuildPlayerUnitsListFromScene();



            ClearSelection();

            // ✅ 첫 파티원 자동 선택
            if (playerUnits.Count > 0)
                SelectUnit(playerUnits[0]);
        }

        private List<Vector3> BuildWorldWaypoints(Unit unit, IList<Vector2Int> pathCoords)
        {
            var wps = new List<Vector3>(pathCoords.Count);

            foreach (var c in pathCoords)
            {
                if (!generator.TryGetTileWorldPos(c, out var tileWorldPos, unitYOffset))
                    continue;

                wps.Add(ComputeUnitWorldPos(c, tileWorldPos, unit));
            }

            return wps;
        }

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

        // =========================================================
        // Party Spawn (Max 3)
        // =========================================================

        private void SpawnOrSnapParty()
        {
            if (TrySpawnPartyFromRunManager())
                return;

            SpawnFallbackParty();
        }

        private bool TrySpawnPartyFromRunManager()
        {
            if (GameRunManager.Instance == null)
                return false;

            var party = GameRunManager.Instance.CurrentParty;
            if (party == null || party.Count == 0)
                return false;

            int spawnedCount = 0;

            for (int i = 0; i < party.Count; i++)
            {
                var member = party[i];
                if (member == null || member.unitPrefab == null) continue;
                if (member.isDead) continue;
                if (spawnedCount >= partySpawnCoords.Count) break;

                SpawnPartyMember(member.unitPrefab, partySpawnCoords[spawnedCount], member);
                spawnedCount++;
            }

            return spawnedCount > 0;
        }

        private void SpawnFallbackParty()
        {
            if (fallbackPartyPrefabs == null || fallbackPartyPrefabs.Count == 0)
                return;

            int spawnedCount = 0;

            for (int i = 0; i < fallbackPartyPrefabs.Count; i++)
            {
                var prefab = fallbackPartyPrefabs[i];
                if (prefab == null) continue;
                if (spawnedCount >= partySpawnCoords.Count) break;

                SpawnPartyMember(prefab, partySpawnCoords[spawnedCount], null);
                spawnedCount++;
            }
        }
        private Unit SpawnPartyMember(Unit prefab, Vector2Int coord, PartyMemberRuntimeData runtimeData)
        {
            if (prefab == null)
                return null;

            if (!generator.TryGetTileWorldPos(coord, out var tileWorldPos, unitYOffset))
                return null;

            var spawned = Instantiate(
                prefab,
                tileWorldPos,
                Quaternion.Euler(unitEulerRotation),
                unitsParent
            );

            var unitWorldPos = ComputeUnitWorldPos(coord, tileWorldPos, spawned);
            spawned.SnapTo(coord, unitWorldPos);

            if (runtimeData != null)
                ApplyRuntimeDataToSpawnedUnit(spawned, runtimeData);

            return spawned;
        }
        private void ApplyRuntimeDataToSpawnedUnit(Unit unit, PartyMemberRuntimeData data)
        {
            if (unit == null || data == null) return;

            var health = unit.Health;
            if (health != null)
                health.SetHP(data.currentHP);

            var resources = unit.GetComponent<TacticsGrid.UnitResources>();
            if (resources != null)
                resources.SetCurrentAP(data.currentAP);

            ApplyRuntimeEquipmentToSpawnedUnit(unit, data);
        }

        private void ApplyRuntimeEquipmentToSpawnedUnit(Unit unit, PartyMemberRuntimeData data)
        {
            if (unit == null || data == null) return;

            int maxSlots = unit.MaxWeaponSlots;
            EnsureWeaponRuntimeSlots(unit, maxSlots);

            var primary = unit.GetWeaponRuntimeBySlot(0);
            ApplyWeaponSlot(primary, data.weaponPrimary, data.weaponPrimaryAmmo);
            if (primary != null)
                unit.SetWeaponRuntime(primary);

            var secondary = unit.GetWeaponRuntimeBySlot(1);
            if (maxSlots > 1)
                ApplyWeaponSlot(secondary, data.weaponSecondary, data.weaponSecondaryAmmo);
            else if (secondary != null)
                ClearWeaponSlot(secondary);

            unit.MarkUIAttacksDirty();
        }

        private void EnsureWeaponRuntimeSlots(Unit unit, int requiredSlots)
        {
            if (unit == null) return;
            requiredSlots = Mathf.Clamp(requiredSlots, 1, 2);

            for (int i = 0; i < requiredSlots; i++)
            {
                var slot = unit.GetWeaponRuntimeBySlot(i);
                if (slot != null) continue;

                var created = unit.gameObject.AddComponent<WeaponRuntime>();
                created.slotIndex = i;
                if (i == 0)
                    unit.SetWeaponRuntime(created);
            }
        }

        private void ApplyWeaponSlot(WeaponRuntime slot, WeaponDef def, int ammo)
        {
            if (slot == null)
                return;

            slot.def = def;
            if (def == null)
            {
                slot.SetCurrentAmmo(0);
                return;
            }

            int max = def.magazineSize;
            int finalAmmo = ammo >= 0 ? Mathf.Clamp(ammo, 0, max) : max;
            slot.SetCurrentAmmo(finalAmmo);
        }

        private void ClearWeaponSlot(WeaponRuntime slot)
        {
            if (slot == null) return;
            slot.def = null;
            slot.SetCurrentAmmo(0);
        }

        public void SaveCurrentPartyStateToRunManager()
        {
            if (GameRunManager.Instance == null)
                return;

            var party = GameRunManager.Instance.CurrentParty;
            if (party == null || party.Count == 0)
                return;

            var players = GetUnitsByFaction(Faction.Player);

            int aliveIndex = 0;

            for (int i = 0; i < party.Count; i++)
            {
                var member = party[i];
                if (member == null) continue;

                if (aliveIndex >= players.Count)
                {
                    member.currentHP = 0;
                    member.isDead = true;
                    continue;
                }

                var unit = players[aliveIndex];
                if (unit == null)
                {
                    member.currentHP = 0;
                    member.isDead = true;
                    continue;
                }

                var health = unit.Health;
                var resources = unit.GetComponent<TacticsGrid.UnitResources>();

                if (health != null)
                    member.currentHP = health.CurrentHP;

                if (resources != null)
                    member.currentAP = resources.CurrentAP;

                var primary = unit.GetWeaponRuntimeBySlot(0);
                member.weaponPrimary = primary != null ? primary.def : null;
                member.weaponPrimaryAmmo = primary != null ? primary.CurrentAmmo : 0;

                if (unit.MaxWeaponSlots > 1)
                {
                    var secondary = unit.GetWeaponRuntimeBySlot(1);
                    member.weaponSecondary = secondary != null ? secondary.def : null;
                    member.weaponSecondaryAmmo = secondary != null ? secondary.CurrentAmmo : 0;
                }
                else
                {
                    member.weaponSecondary = null;
                    member.weaponSecondaryAmmo = 0;
                }

                member.isDead = (health != null && health.IsDead);

                aliveIndex++;
            }
        }
        private void BuildPlayerUnitsListFromScene()
        {
            playerUnits.Clear();

            var units = (unitsParent != null)
                ? unitsParent.GetComponentsInChildren<Unit>(true)
                : FindObjectsByType<Unit>(FindObjectsSortMode.None);

            foreach (var u in units)
            {
                if (u == null) continue;
                if (u.faction == Faction.Player)
                    playerUnits.Add(u);
            }

            // player는 첫 파티원(호환)
            player = (playerUnits.Count > 0) ? playerUnits[0] : null;

            unitListUI?.Build(this, playerUnits);
            unitListUI?.RefreshAll();

            Debug.Log($"[UnitScan] found total={(unitsParent ? unitsParent.GetComponentsInChildren<Unit>(true).Length : -1)}");
            foreach (var u in units)
                Debug.Log($"[UnitScan] {u.name} faction={u.faction}");
            Debug.Log($"[UnitScan] playerUnits={playerUnits.Count}");
        }

        /// <summary>
        /// 씬에 존재하는 모든 Unit을 기준으로 unitAt을 재구축하고, 타일 점유(Occupy/Block)도 일괄 반영한다.
        /// ※ 초기화 시점(점유 카운트가 0일 때)에서만 쓰는 걸 권장.
        /// </summary>
        private void RebuildUnitRegistryAndOccupancyFromScene()
        {
            unitAt.Clear();
            occupiedByUnit.Clear();

            var units = (unitsParent != null)
                ? unitsParent.GetComponentsInChildren<Unit>(true)
                : FindObjectsByType<Unit>(FindObjectsSortMode.None);

            foreach (var u in units)
            {
                if (u == null) continue;

                HookDeath(u);

                var origin = u.transform.position + Vector3.up * 2f;
                if (Physics.Raycast(origin, Vector3.down, out var hit, 10f))
                {
                    var tile = hit.collider.GetComponentInParent<Tile>();
                    if (tile != null)
                    {
                        u.SnapTo(tile.Coord, u.transform.position);
                        unitAt[tile.Coord] = u;

                    }
                }
            }
        }

        public void ClearSelection()
        {
            if (selected != null) selected.ClearActiveAttack();
            selected = null;
            ExitAttackMode();
            ClearMoveRange();

            attackBarUI?.Refresh();
            weaponPanelUI?.Refresh(null);
        }

        public void SelectUnit(Unit u)
        {
            if (u == null) { ClearSelection(); return; }
            if (u.faction != Faction.Player) return;

            if (IsBusy) return;

            selected = u;
            selected.ClearActiveAttack();

            // ✅ 여기 추가
            int idx = playerUnits != null ? playerUnits.IndexOf(u) : -1;
            if (idx >= 0) currentUnitIndex = idx;

            attackBarUI?.Refresh();
            weaponPanelUI?.Refresh(selected);

            if (TryGetResources(selected, out var res))
                apUI?.Refresh(res);
            else
                apUI?.Refresh(null);

            ExitAttackMode();

            // ✅ 이번 턴 이미 이동했으면 이동범위 오버레이 안 띄움
            if (selected != null && selected.HasMovedThisTurn)
                ClearMoveRange();
            else
                ShowMoveRange(selected);

            unitListUI?.SetSelected(selected);
            unitListUI?.RefreshAll();
            standingUI?.Show(u);
        }

        public void ToggleSelectUnit(Unit u)
        {
            if (u == null) { ClearSelection(); return; }
            if (Selected == u) { ClearSelection(); return; }
            SelectUnit(u);
        }

        public bool TryGetUnitAt(Vector2Int coord, out Unit unit)
        {
            unit = null;
            if (!unitAt.TryGetValue(coord, out var u) || u == null) return false;
            if (!u.gameObject.activeInHierarchy) return false;
            if (u.Health != null && u.Health.IsDead) return false;

            unit = u;
            return true;
        }

        // =========================================================
        // Move Range Overlay
        // =========================================================

        private bool TryGetLiveUnitAt(Vector2Int coord, out Unit u)
        {
            u = null;
            if (!unitAt.TryGetValue(coord, out var found) || found == null) return false;
            if (!found.gameObject.activeInHierarchy) return false;
            if (found.Health != null && found.Health.IsDead) return false;
            u = found;
            return true;
        }

        private bool HasEnemy(Unit mover, Vector2Int coord)
        {
            if (mover == null) return false;
            if (!TryGetLiveUnitAt(coord, out var other)) return false;
            return other != null && other != mover && other.faction != mover.faction;
        }

        private bool HasAlly(Unit mover, Vector2Int coord)
        {
            if (mover == null) return false;
            if (!TryGetLiveUnitAt(coord, out var other)) return false;
            return other != null && other != mover && other.faction == mover.faction;
        }

        // ✅ 타일 통과 가능? (장애물은 막고, 유닛은 아군이면 통과 허용)
        private bool CanTraverseFor(Unit mover, Vector2Int coord, Tile tile)
        {
            if (tile == null) return false;
            if (!tile.Walkable) return false;

            // 적 유닛이 있으면 무조건 통과 불가
            if (HasEnemy(mover, coord)) return false;

            // BlocksTraversal이면 원칙적으로 막힘.
            // 단, 그 자리에 "아군 유닛이 실제로 있다"면 유닛이 만든 block일 가능성이 크니 통과 허용.
            if (tile.BlocksTraversal)
                return HasAlly(mover, coord);

            return true;
        }

        // ✅ 여기서 멈출 수 있나? (유닛 있으면 멈춤 불가)
        private bool CanStopFor(Unit mover, Vector2Int coord, Tile tile)
        {
            if (tile == null) return false;
            if (!tile.Walkable) return false;
            if (tile.BlocksOccupancy) return false;

            // 아군/적 모두 해당 타일 점유 중이면 도착 불가
            if (TryGetLiveUnitAt(coord, out var other) && other != null && other != mover)
                return false;

            return true;
        }

        private Dictionary<Vector2Int, int> ComputeReachableByCost(Unit mover, Vector2Int origin, int maxCost)
        {
            var dist = new Dictionary<Vector2Int, int>(256);
            var pq = new SimpleMinHeap();

            dist[origin] = 0;
            pq.Push(origin, 0);

            while (pq.Count > 0)
            {
                var (cur, curCost) = pq.Pop();
                if (curCost > maxCost) continue;
                if (dist.TryGetValue(cur, out var known) && curCost != known) continue;

                var neighbors = new Vector2Int[]
                {
                    new(cur.x+1, cur.y),
                    new(cur.x-1, cur.y),
                    new(cur.x, cur.y+1),
                    new(cur.x, cur.y-1),
                };

                foreach (var nb in neighbors)
                {
                    if (!generator.TryGetTile(nb, out var nbTile) || nbTile == null) continue;

                    // ✅ 아군 통과 허용/적 차단/장애물 차단
                    if (!CanTraverseFor(mover, nb, nbTile)) continue;

                    int next = curCost + 1;
                    if (next > maxCost) continue;

                    if (!dist.TryGetValue(nb, out var old) || next < old)
                    {
                        dist[nb] = next;
                        pq.Push(nb, next);
                    }
                }
            }

            return dist;
        }

        private bool TryFindPathAStar(Unit mover, Vector2Int start, Vector2Int goal, out List<Vector2Int> path)
        {
            path = null;

            if (generator == null) return false;
            if (mover == null) return false;

            if (!generator.TryGetTile(start, out var startTile) || startTile == null) return false;
            if (!generator.TryGetTile(goal, out var goalTile) || goalTile == null) return false;

            // 목적지는 반드시 "도착 가능"해야 함 (유닛 점유 타일이면 불가)
            if (!CanStopFor(mover, goal, goalTile)) return false;

            int Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

            var open = new SimpleMinHeap();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>(256);
            var gScore = new Dictionary<Vector2Int, int>(256);

            gScore[start] = 0;
            open.Push(start, Heuristic(start, goal));

            while (open.Count > 0)
            {
                var (cur, _) = open.Pop();

                if (cur == goal)
                {
                    var rev = new List<Vector2Int>(64) { cur };
                    while (cameFrom.TryGetValue(cur, out var prev))
                    {
                        cur = prev;
                        rev.Add(cur);
                    }
                    rev.Reverse();
                    path = rev;
                    return true;
                }

                var neighbors = new Vector2Int[]
                {
            new(cur.x+1, cur.y),
            new(cur.x-1, cur.y),
            new(cur.x, cur.y+1),
            new(cur.x, cur.y-1),
                };

                foreach (var nb in neighbors)
                {
                    if (!generator.TryGetTile(nb, out var nbTile) || nbTile == null) continue;

                    // ✅ mover 기준으로 통과 판정
                    if (!CanTraverseFor(mover, nb, nbTile)) continue;

                    int tentativeG = gScore[cur] + 1;

                    if (!gScore.TryGetValue(nb, out var oldG) || tentativeG < oldG)
                    {
                        cameFrom[nb] = cur;
                        gScore[nb] = tentativeG;

                        int f = tentativeG + Heuristic(nb, goal);
                        open.Push(nb, f);
                    }
                }
            }

            return false;
        }

        public void ShowMoveRange(Unit u)
        {
            ClearMoveRange();
            if (generator == null || u == null) return;

            int blueMax = u.MoveBlue;
            int yellowMax = u.MoveYellow;

            moveDist = ComputeReachableByCost(u, u.Coord, maxCost: yellowMax);

            foreach (var kv in moveDist)
            {
                int d = kv.Value;
                if (d == 0) continue;

                if (!generator.TryGetTile(kv.Key, out var tile) || tile == null) continue;

                // ✅ 여기서 멈출 수 있는 타일만 색칠 (아군 위 타일은 색칠 X)
                if (!CanStopFor(u, kv.Key, tile)) continue;

                if (d <= blueMax) tile.ShowOverlay(blue);
                else if (d <= yellowMax) tile.ShowOverlay(yellow);
                else continue;

                highlighted.Add(tile);
            }
        }

        public void ClearMoveRange()
        {
            foreach (var t in highlighted)
                if (t != null) t.HideOverlay();
            highlighted.Clear();
            moveDist = null;
        }

        // =========================================================
        // Unit placement (per-unit offset)
        // =========================================================

        private Vector3 ComputeUnitWorldPos(Vector2Int coord, Vector3 tileWorldPos, Unit unit)
        {
            Vector3 offset = (unit != null) ? unit.TileOffset : defaultUnitTileOffset;
            bool useTileSpace = (unit != null) ? unit.OffsetInTileSpace : defaultOffsetInTileSpace;

            if (!useTileSpace)
                return tileWorldPos + offset;

            if (generator != null && generator.TryGetTile(coord, out var tile) && tile != null)
                return tileWorldPos + tile.transform.TransformVector(offset);

            return tileWorldPos + offset;
        }

        // =========================================================
        // Occupancy Rules (3 types)
        // =========================================================

        private void ApplyOccupancy(Unit unit, Vector2Int coord, bool enter)
        {
            if (unit == null || generator == null) return;
            if (!generator.TryGetTile(coord, out var tile) || tile == null) return;

            // 스웜/비점유 유닛은 아예 영향 없음
            if (unit.occupyRule == Unit.OccupyRule.NoOccupyNoBlock)
                return;

            // ==============================
            // ✅ ENTER: 유닛당 1칸 점유를 강제
            // ==============================
            if (enter)
            {
                // 이미 이 유닛이 점유중인 칸이 있다면
                if (occupiedByUnit.TryGetValue(unit, out var prev))
                {
                    // 같은 칸이면 중복 ENTER → 무시
                    if (prev == coord) return;

                    // 다른 칸이면 이전 칸을 먼저 LEAVE 처리 (균형 유지)
                    ApplyOccupancy(unit, prev, enter: false);
                }

                // 이제 이 칸을 점유 등록
                occupiedByUnit[unit] = coord;

                // 실제 타일 카운트 반영
                switch (unit.occupyRule)
                {
                    case Unit.OccupyRule.BlockAndOccupy:
                        tile.AddOccupy(+1);
                        tile.AddBlock(+1);
                        break;

                    case Unit.OccupyRule.OccupyButNoBlock:
                        tile.AddOccupy(+1);
                        break;
                }

                return;
            }

            // ==============================
            // ✅ LEAVE: "내가 점유중인 칸"일 때만 내림
            // ==============================
            if (occupiedByUnit.TryGetValue(unit, out var curOcc))
            {
                // 다른 좌표를 LEAVE 하라고 들어오면(꼬인 호출) → 무시
                if (curOcc != coord) return;

                occupiedByUnit.Remove(unit);
            }
            else
            {
                // 기록이 없는데 LEAVE가 들어오면 → 무시(중복 LEAVE 방지)
                return;
            }

            switch (unit.occupyRule)
            {
                case Unit.OccupyRule.BlockAndOccupy:
                    tile.AddOccupy(-1);
                    tile.AddBlock(-1);
                    break;

                case Unit.OccupyRule.OccupyButNoBlock:
                    tile.AddOccupy(-1);
                    break;
            }
        }



        // =========================================================
        // Spawn compatibility + Move
        // =========================================================

        public void SpawnOrSnapPlayer(Vector2Int coord)
        {
            // ✅ 호환용: "player 1명" 기준 호출이 남아있을 때를 대비
            if (generator == null)
            {
                Debug.LogError("[GridController] generator is null");
                return;
            }

            if (playerUnits == null || playerUnits.Count == 0)
            {
                Debug.LogWarning("[GridController] SpawnOrSnapPlayer called, but no party units exist.");
                return;
            }

            var u = playerUnits[0];

            if (!generator.TryGetTileWorldPos(coord, out var tileWorldPos, unitYOffset))
            {
                Debug.LogError($"[GridController] Snap failed. Tile not found: {coord} (gen={generator.name})");
                return;
            }

            var old = u.Coord;
            ApplyOccupancy(u, old, enter: false);
            unitAt.Remove(old);

            ApplyOccupancy(u, coord, enter: true);
            unitAt[coord] = u;

            var unitWorldPos = ComputeUnitWorldPos(coord, tileWorldPos, u);
            u.SnapTo(coord, unitWorldPos);
        }

        public void MoveSelectedTo(Vector2Int coord)
        {
            if (selected == null) return;
            MoveUnitTo(selected, coord);
        }

        public void MoveUnitTo(Unit unit, Vector2Int coord)
        {
            if (unit == null) return;

            Vector2Int start = occupiedByUnit.TryGetValue(unit, out var occ) ? occ : unit.Coord;

            if (!TryFindPathAStar(unit, start, coord, out var path))
            {
                Debug.Log($"[GridController] no path: {start} -> {coord}");
                return;
            }

            if (path.Count > 0 && path[0] == start) path.RemoveAt(0);

            // ✅ 안전 ApplyOccupancy가 이전칸/중복을 처리함
            ApplyOccupancy(unit, start, enter: false);
            ApplyOccupancy(unit, coord, enter: true);

            var worldPath = BuildWorldWaypoints(unit, path);

            unitAt.Remove(start);
            unitAt[coord] = unit;

            unit.MarkMoved();
            unit.MoveAlongPath(coord, worldPath, unitMoveSpeed);

            StartCoroutine(CoShowRangeAfterMove(unit));
        }


        private IEnumerator CoShowRangeAfterMove(Unit movingUnit)
        {
            yield return null;
            while (movingUnit != null && movingUnit.IsMoving) yield return null;

            if (selected != null)
            {
                // ✅ 이동 끝난 직후엔 moved 플래그가 true일 것이므로 오버레이 숨김
                if (selected.HasMovedThisTurn) ClearMoveRange();
                else ShowMoveRange(selected);
            }
            else
            {
                ClearMoveRange();
            }
        }

        private class SimpleMinHeap
        {
            private readonly List<(Vector2Int node, int cost)> data = new();
            public int Count => data.Count;

            public void Push(Vector2Int node, int cost)
            {
                data.Add((node, cost));
                int i = data.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (data[p].cost <= data[i].cost) break;
                    (data[p], data[i]) = (data[i], data[p]);
                    i = p;
                }
            }

            public (Vector2Int node, int cost) Pop()
            {
                var root = data[0];
                var last = data[^1];
                data.RemoveAt(data.Count - 1);
                if (data.Count == 0) return root;

                data[0] = last;
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1;
                    int r = l + 1;
                    if (l >= data.Count) break;

                    int m = (r < data.Count && data[r].cost < data[l].cost) ? r : l;
                    if (data[i].cost <= data[m].cost) break;

                    (data[i], data[m]) = (data[m], data[i]);
                    i = m;
                }

                return root;
            }
        }

        /// <summary>
        /// 적 턴 관련 기능
        /// </summary>
        private bool inputEnabled = true;

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        public void ClearSelectionAndOverlays()
        {
            ExitAttackMode();
            ClearMoveRange();
            ClearSelection();
        }
        public List<Unit> GetUnitsByFaction(Faction f)
        {
            var list = new List<Unit>();

            var units = (unitsParent != null)
                ? unitsParent.GetComponentsInChildren<Unit>(true)
                : FindObjectsByType<Unit>(FindObjectsSortMode.None);

            foreach (var u in units)
            {
                if (u == null) continue;
                if (!u.gameObject.activeInHierarchy) continue;
                if (u.Health != null && u.Health.IsDead) continue;
                if (u.faction == f) list.Add(u);
            }
            return list;
        }
        public Dictionary<Vector2Int, int> GetMoveDist(Unit mover)
        {
            if (mover == null) return null;
            return ComputeReachableByCost(mover, mover.Coord, mover.MoveYellow);
        }
        public bool CanStopAt(Unit mover, Vector2Int coord)
        {
            if (generator == null) return false;
            if (!generator.TryGetTile(coord, out var tile) || tile == null) return false;
            return CanStopFor(mover, coord, tile);
        }
        public IEnumerator CoMoveUnit(Unit unit, Vector2Int dest)
        {
            if (unit == null) yield break;

            MoveUnitTo(unit, dest);

            while (unit != null && unit.IsMoving)
                yield return null;
        }

        public bool CanEnemyAttack(Unit attacker, Unit target, int attackKey = 1)
        {
            if (attacker == null || target == null) { Debug.LogWarning("[AI CAN] null"); return false; }
            if (attacker.Health != null && attacker.Health.IsDead) { Debug.Log($"[AI CAN] attacker dead {attacker.name}"); return false; }
            if (target.Health != null && target.Health.IsDead) { Debug.Log($"[AI CAN] target dead {target.name}"); return false; }
            if (attacker.faction == target.faction) { Debug.Log($"[AI CAN] same faction"); return false; }

            if (!attacker.SetActiveAttackByNumberKey(attackKey))
            {
                Debug.Log($"[AI CAN] {attacker.name} has no attack at key={attackKey} (ui list count={attacker.GetUIAttacks()?.Count})");
                return false;
            }

            var atk = attacker.ActiveAttack;
            if (atk == null) { Debug.Log($"[AI CAN] {attacker.name} ActiveAttack null"); return false; }
            if (atk.isReload) { Debug.Log($"[AI CAN] {attacker.name} attack is reload -> blocked"); return false; }

            if (!TryGetResources(attacker, out var res) || res == null)
            {
                Debug.Log($"[AI CAN] {attacker.name} no UnitResources");
                return false;
            }

            if (res.CurrentAP < atk.apCost)
            {
                Debug.Log($"[AI CAN] {attacker.name} AP 부족 need={atk.apCost} have={res.CurrentAP}");
                return false;
            }

            HashSet<Vector2Int> tiles = atk.targetPoint
                ? BuildManhattanRange(attacker.Coord, atk.range)
                : BuildAttackTiles(attacker.Coord, atk);

            tiles.Remove(attacker.Coord);

            bool inRange = tiles.Contains(target.Coord);
            if (!inRange)
                Debug.Log($"[AI CAN] {attacker.name} out of range for {atk.displayName} (range={atk.range}) attacker={attacker.Coord} target={target.Coord}");

            return inRange;
        }



        public bool TryEnemyAttack(Unit attacker, Unit target, int attackKey = 1)
        {
            if (!CanEnemyAttack(attacker, target, attackKey))
            {
                Debug.Log($"[AI ATK] CanEnemyAttack=false attacker={(attacker ? attacker.name : "null")} target={(target ? target.name : "null")}");
                return false;
            }

            var atk = attacker.ActiveAttack;

            if (!TryGetResources(attacker, out var res) || res == null)
            {
                Debug.Log($"[AI ATK] no resources {attacker.name}");
                return false;
            }

            Debug.Log($"[AI ATK] {attacker.name} uses {atk.displayName} dmg={atk.damage} apCost={atk.apCost} -> {target.name}");

            // ✅ AP 소모
            res.TrySpendAP(atk.apCost);
            attacker.MarkAttacked();

            HookDeath(target);

            // ✅ HP BEFORE/AFTER 로그 (Health에 CurrentHP가 없을 수도 있으니 IsDead 위주로)
            Debug.Log($"[AI ATK] target before dead={(target.Health ? target.Health.IsDead : false)}");

            target.Health?.TakeDamage(atk.damage, attacker);

            Debug.Log($"[AI ATK] target after dead={(target.Health ? target.Health.IsDead : false)}");

            if (target.Health != null && target.Health.IsDead)
                HandleUnitDied(target, attacker);

            // UI 갱신
            if (selected != null && selected.faction == Faction.Player)
            {
                if (TryGetResources(selected, out var selRes)) apUI?.Refresh(selRes);
                unitListUI?.RefreshAll();
                attackBarUI?.Refresh();
                weaponPanelUI?.Refresh(selected);
            }

            return true;
        }



        public void SelectFirstAlivePlayerUnit()
        {
            if (playerUnits == null || playerUnits.Count == 0)
            {
                BuildPlayerUnitsListFromScene();
            }

            for (int i = 0; i < playerUnits.Count; i++)
            {
                var u = playerUnits[i];
                if (u == null) continue;
                if (u.Health != null && u.Health.IsDead) continue;

                SelectUnit(u);
                return;
            }

            ClearSelection();
        }
        // =========================
        // 공격 제어
        // =========================




        // 죽음 이벤트 중복 구독 방지
        private readonly HashSet<Unit> deathHooked = new();

        private void HookDeath(Unit u)
        {
            if (u == null || u.Health == null) return;
            if (!deathHooked.Add(u)) return;

            // Health.OnDied 시점에 점유/레지스트리 해제
            u.Health.OnDied += (src) => HandleUnitDied(u, src);
        }

        private void HandleUnitDied(Unit u, object source)
        {
            if (u == null) return;

            // ✅ unitAt 기준으로 "실제로 점유 중인 좌표"를 찾아서 전부 해제
            var coordsToFree = new List<Vector2Int>(2);
            foreach (var kv in unitAt)
                if (kv.Value == u)
                    coordsToFree.Add(kv.Key);

            foreach (var c in coordsToFree)
            {
                ApplyOccupancy(u, c, enter: false);
                unitAt.Remove(c);
            }

            // (혹시 registry에 없었더라도) 안전망으로 u.Coord도 한번 해제 시도
            // ※ 이동 중 사망 같은 특이 케이스에서 도움됨
            if (coordsToFree.Count == 0)
                ApplyOccupancy(u, u.Coord, enter: false);

            // 선택 중 죽었으면 선택 해제
            if (selected == u)
                ClearSelection();

            // 파티 리스트에서도 제거 (UI/턴 꼬임 방지)
            if (playerUnits != null)
                playerUnits.Remove(u);

            unitListUI?.RefreshAll();
            attackBarUI?.Refresh();
            weaponPanelUI?.Refresh(selected);

            occupiedByUnit.Remove(u); // ✅ 사망 시 잔존 점유 추적 제거

            CheckBattleEnd(); // 사망 처리 후 전투 종료 조건 체크

        }

        private void ShowAttackRange(HashSet<Vector2Int> coords, Color color)
        {
            ClearAttackRange();
            foreach (var c in coords)
            {
                if (!generator.TryGetTile(c, out var tile) || tile == null) continue;
                tile.ShowOverlay(color);
                attackHighlighted.Add(tile);
            }
        }

        private void ClearAttackRange()
        {
            foreach (var t in attackHighlighted)
                if (t != null) t.HideOverlay();
            attackHighlighted.Clear();
        }

        private HashSet<Vector2Int> BuildManhattanRange(Vector2Int center, int range)
        {
            var result = new HashSet<Vector2Int>();

            for (int dx = -range; dx <= range; dx++)
            {
                int rem = range - Mathf.Abs(dx);
                for (int dy = -rem; dy <= rem; dy++)
                {
                    var c = new Vector2Int(center.x + dx, center.y + dy);
                    if (!generator.TryGetTile(c, out _)) continue;
                    result.Add(c);
                }
            }

            return result;
        }

        private bool TryGetResources(Unit u, out UnitResources res)
        {
            res = null;
            if (u == null) return false;
            res = u.GetComponent<UnitResources>();
            return res != null;
        }

        private void RegisterOrMoveUnit(Unit u, Vector2Int coord)
        {
            unitAt.Remove(u.Coord);
            unitAt[coord] = u;
        }

        private void SetUnitAt(Unit u, Vector2Int coord)
        {
            unitAt.Remove(u.Coord);
            unitAt[coord] = u;
        }

        public void ToggleAttackMode()
        {
            if (attackMode) ExitAttackMode();
            else EnterAttackMode();
        }

        public void ExitAttackMode()
        {
            if (!attackMode) return;

            ClearAttackRange();
            attackTiles.Clear();
            attackMode = false;

            if (selected != null)
                selected.ClearActiveAttack();

            if (selected != null)
            {
                if (selected.HasMovedThisTurn) ClearMoveRange();
                else ShowMoveRange(selected);
            }
            else
            {
                ClearMoveRange();
            }

            attackBarUI?.Refresh();
        }

        public void OnTileClicked(Vector2Int coord)
        {

            if (!inputEnabled) return;
            if (selected == null) return;
            if (attackMode)
            {
                TryAttackAt(coord);
                return;
            }
            if (selected.HasMovedThisTurn)
            {
                // ✅ 이번 턴 이미 이동했으면 이동 입력 자체 무시
                // (선택 해제하고 싶으면 여기서 ClearSelection() 하면 됨)
                return;
            }

            if (moveDist == null || !moveDist.TryGetValue(coord, out var cost) || cost <= 0)
            {
                ClearSelection();
                return;
            }

            if (cost > selected.MoveYellow)
            {
                ClearSelection();
                return;
            }

            MoveSelectedTo(coord);
        }

        public void DoReload()
        {
            Debug.Log("[Reload] DoReload() called");

            if (selected == null)
            {
                Debug.LogWarning("[Reload] selected is null");
                return;
            }

            var active = selected.ActiveUIAttackItem;
            if (active == null)
            {
                Debug.LogWarning("[Reload] ActiveUIAttackItem is null");
                return;
            }

            var atk = active.Value.def;
            if (atk == null)
            {
                Debug.LogWarning("[Reload] atk is null");
                return;
            }

            Debug.Log($"[Reload] active atk = {atk.displayName}, isReload={atk.isReload}, apCost={atk.apCost}");

            if (!atk.isReload)
            {
                Debug.LogWarning("[Reload] Active attack is NOT reload");
                return;
            }

            if (!TryGetResources(selected, out var res) || res == null)
            {
                Debug.LogWarning("[Reload] UnitResources 없음");
                return;
            }

            Debug.Log($"[Reload] AP before: {res.CurrentAP}");

            if (res.CurrentAP < atk.apCost)
            {
                Debug.LogWarning($"[Reload] AP 부족: need {atk.apCost}, have {res.CurrentAP}");
                return;
            }

            var weapon = (active.Value.source == Unit.AttackSource.Weapon)
                ? selected.GetWeaponRuntimeBySlot(active.Value.weaponSlotIndex)
                : selected.GetPrimaryWeaponRuntime();
            if (weapon == null)
            {
                Debug.LogWarning("[Reload] WeaponRuntime component missing");
                return;
            }

            Debug.Log($"[Reload] weapon.def={(weapon.def ? weapon.def.name : "NULL")} cur={weapon.CurrentAmmo} max={weapon.MaxAmmo}");

            // 비용 지불
            res.TrySpendAP(atk.apCost);

            // 실제 리로드
            weapon.Reload();

            Debug.Log($"[Reload] weapon AFTER cur={weapon.CurrentAmmo} max={weapon.MaxAmmo} | AP after: {res.CurrentAP}");

            weaponPanelUI?.Refresh(selected);
            apUI?.Refresh(res);
            unitListUI?.RefreshAll();

            selected.MarkAttacked();

            ExitAttackMode();
            attackBarUI?.Refresh();
        }
        private void TryAttackAt(Vector2Int targetCoord)
        {
            if (!attackTiles.Contains(targetCoord)) return;

            if (selected == null)
            {
                ExitAttackMode();
                return;
            }

            var active = selected.ActiveUIAttackItem;
            if (active == null)
            {
                Debug.LogWarning("[Attack] 선택된 공격이 없음");
                ExitAttackMode();
                return;
            }

            var atk = active.Value.def;
            if (atk == null)
            {
                Debug.LogWarning("[Attack] AttackDef가 null");
                ExitAttackMode();
                return;
            }

            if (!TryGetResources(selected, out var res))
            {
                Debug.LogWarning("[Attack] UnitResources 없음");
                ExitAttackMode();
                return;
            }

            // 0) AP 체크
            if (res.CurrentAP < atk.apCost)
            {
                Debug.Log($"[Attack] AP 부족: need {atk.apCost}, have {res.CurrentAP}");
                ExitAttackMode();
                return;
            }

            // 1) 무기 공격이면 탄약 체크(지점/범위든 단일이든 동일)
            WeaponRuntime weapon = null;
            int weaponAtkIndex = -1;

            if (active.Value.source == Unit.AttackSource.Weapon)
            {
                weapon = selected.GetWeaponRuntimeBySlot(active.Value.weaponSlotIndex);
                if (weapon == null || weapon.def == null)
                {
                    Debug.LogWarning("[Attack] WeaponRuntime/def가 없음");
                    ExitAttackMode();
                    return;
                }

                weaponAtkIndex = active.Value.weaponAttackIndex;

                if (!weapon.CanUseAttack(weaponAtkIndex))
                {
                    Debug.Log($"[Attack] 무기 탄약 부족: {weapon.CurrentAmmo}/{weapon.MaxAmmo}");
                    ExitAttackMode();
                    return;
                }
            }

            // 2) 비용 지불(AP + 탄약)
            res.TrySpendAP(atk.apCost);
            if (weapon != null && weaponAtkIndex >= 0)
            {
                weapon.TryConsumeForAttack(weaponAtkIndex);
                weaponPanelUI?.Refresh(selected);
            }

            apUI?.Refresh(res);
            unitListUI?.RefreshAll();

            selected.MarkAttacked();

            // =========================================================
            // 3) 데미지 적용
            //    - 지점 폭발(aoe) / 단일 공격 모두 처리
            // =========================================================

            // ✅ 지점 선택 + 폭발 반경이 있으면: 유닛이 없는 지점도 가능
            if (atk.targetPoint && atk.aoeRadius > 0)
            {
                // 중심 포함하고 싶으면 Add(targetCoord)
                var blast = BuildRadiusRange(targetCoord, atk.aoeRadius);
                blast.Add(targetCoord);

                foreach (var c in blast)
                {
                    // 타일에 유닛이 없으면 스킵
                    if (!TryGetUnitAt(c, out var t) || t == null) continue;

                    // 아군 제외(원하면 옵션으로 뺄 수 있음)
                    if (t.faction == selected.faction) continue;

                    HookDeath(t);

                    t.Health?.TakeDamage(atk.damage, selected);

                    if (t.Health != null && t.Health.IsDead)
                        HandleUnitDied(t, selected);
                }

                ExitAttackMode();
                attackBarUI?.Refresh();
                return;
            }

            // ✅ (옵션) "유닛 기준 범위기"도 지원하고 싶으면 여기서 처리 가능
            // 예: atk.targetPoint==false && atk.aoeRadius>0 이면 selected.Coord 기준 폭발
            // if (!atk.targetPoint && atk.aoeRadius > 0)
            // {
            //     var blast = BuildRadiusRange(selected.Coord, atk.aoeRadius);
            //     blast.Remove(selected.Coord); // 자기 자신 제외하고 싶으면
            //     foreach (var c in blast) { ... 위와 동일 ... }
            //     ExitAttackMode(); attackBarUI?.Refresh(); return;
            // }

            // ✅ 기본 단일 공격: "타겟 타일에 적 유닛이 있어야 함"
            if (!TryGetUnitAt(targetCoord, out var target) || target == null)
            {
                // 지점 공격이 아닌데도 유닛이 없으면 공격 불발
                ExitAttackMode();
                return;
            }

            if (target.faction == selected.faction)
            {
                ExitAttackMode();
                return;
            }

            HookDeath(target);

            target.Health?.TakeDamage(atk.damage, selected);

            if (target.Health != null && target.Health.IsDead)
                HandleUnitDied(target, selected);

            ExitAttackMode();
            attackBarUI?.Refresh();
        }
        private void SyncUnitsFromScene()
        {
            // ✅ 기존 함수명 호환 유지: 이제 레지스트리 + 점유까지 함께 재구축
            RebuildUnitRegistryAndOccupancyFromScene();
        }

        private HashSet<Vector2Int> BuildAttackTiles(Vector2Int origin, AttackDef atk)
        {
            if (atk == null) return new HashSet<Vector2Int>();

            switch (atk.pattern)
            {
                case AttackPattern.Manhattan:
                    return BuildManhattanRange(origin, atk.range);
                case AttackPattern.Line4:
                    return BuildLine4Range(origin, atk.range);
                case AttackPattern.Radius:
                    return BuildRadiusRange(origin, atk.range);
                default:
                    return BuildManhattanRange(origin, atk.range);
            }
        }

        private HashSet<Vector2Int> BuildLine4Range(Vector2Int center, int range)
        {
            var result = new HashSet<Vector2Int>();
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var d in dirs)
            {
                for (int i = 1; i <= range; i++)
                {
                    var c = center + d * i;
                    if (!generator.TryGetTile(c, out _)) break;
                    result.Add(c);
                }
            }

            return result;
        }

        private HashSet<Vector2Int> BuildRadiusRange(Vector2Int center, int range)
        {
            var result = new HashSet<Vector2Int>();
            int r2 = range * range;

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (dx * dx + dy * dy > r2) continue;

                    var c = new Vector2Int(center.x + dx, center.y + dy);
                    if (!generator.TryGetTile(c, out _)) continue;
                    result.Add(c);
                }
            }

            return result;
        }

        public void EnterAttackMode()
        {
            if (selected == null) return;

            var atk = selected.ActiveAttack;
            if (atk == null) return;

            // ✅ 리로드는 공격모드 없음
            if (atk.isReload) return;

            ClearMoveRange();
            attackMode = true;

            // ✅ 1) 지점선택 공격이면: "찍을 수 있는 지점"만 칠함
            if (atk.targetPoint)
            {
                attackTiles = BuildManhattanRange(selected.Coord, atk.range); // 지점 선택 사거리
                attackTiles.Remove(selected.Coord);
                ShowAttackRange(attackTiles, atk.overlayColor);
                return;
            }

            // ✅ 2) 기존 방식 (유닛 기준 범위)
            attackTiles = BuildAttackTiles(selected.Coord, atk);
            attackTiles.Remove(selected.Coord);
            ShowAttackRange(attackTiles, atk.overlayColor);
        }

        public void SelectAttackByIndex(int index0)
        {
            var sel = Selected;
            if (sel == null) return;

            int key = index0 + 1;
            if (!sel.SetActiveAttackByNumberKey(key)) return;

            // ✅ 여기: 리로드면 공격모드로 들어가지 말고 즉시 실행
            var atk = sel.ActiveAttack;
            if (atk != null && atk.isReload)
            {
                DoReload();                 // 즉시 실행
                attackBarUI?.Refresh();
                return;
            }

            EnterAttackMode();
            attackBarUI?.Refresh();

            Debug.Log($"[Attack] Active = {sel.ActiveAttack.displayName} (#{key})");
        }


        private void ResetAllTilesBlocksOnce()
        {
            var tiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            foreach (var t in tiles)
            {
                if (t == null) continue;
                t.ResetBlocks();
            }
        }

        private void ApplyInitialOccupancyFromRegistry()
        {
            foreach (var kv in unitAt)
            {
                var c = kv.Key;
                var u = kv.Value;
                if (u == null) continue;
                if (u.Health != null && u.Health.IsDead) continue;

                ApplyOccupancy(u, c, enter: true);
            }
        }




        public bool IsBusy
        {
            get
            {
                // ✅ 파티원 중 누구라도 이동 중이면 입력/선택을 막는다
                if (playerUnits != null)
                {
                    for (int i = 0; i < playerUnits.Count; i++)
                    {
                        var u = playerUnits[i];
                        if (u != null && u.IsMoving) return true;
                    }
                }

                return false;
            }
        }

        public void EndTurn()
        {
            if (IsBusy) return;

            // 모든 전투/입력 상태 정리
            ExitAttackMode();
            ClearMoveRange();

            // ✅ 플레이어 유닛 전원 AP 초기화
            foreach (var u in playerUnits)
            {
                if (u == null) continue;
                if (u.Health != null && u.Health.IsDead) continue;

                u.ResetTurnFlags();

                var res = u.GetComponent<UnitResources>();
                if (res != null)
                    res.RefillAP();
            }




            // ✅ 선택 상태 완전 초기화
            ClearSelection();

            // ✅ 인덱스 의미 없음 (자동 선택 안 할 거니까)
            currentUnitIndex = -1;

            // UI 갱신
            unitListUI?.RefreshAll();
            attackBarUI?.Refresh();

            // 👉 여기서 적 턴 시작 or 턴 매니저에게 제어 넘김
            // TurnManager.Instance.StartEnemyTurn();

            enemyTurnManager.StartEnemyTurn();
        }
    }


}
