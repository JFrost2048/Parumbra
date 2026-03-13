using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticsGrid
{
    public enum Faction
    {
        Player,
        Enemy,
        Neutral
    }

    public class Unit : MonoBehaviour
    {
        public enum TilePresence
        {
            Solid,
            PassOnly,
            Swarm,
            Hazard
        }

        [Header("Info")]
        [SerializeField] private string displayName;

        public string DisplayName => displayName;

        public enum OccupyRule
        {
            BlockAndOccupy,
            OccupyButNoBlock,
            NoOccupyNoBlock
        }

        [Header("Tile Occupancy")]
        public TilePresence presence = TilePresence.Solid;
        public OccupyRule occupyRule = OccupyRule.BlockAndOccupy;

        public Vector2Int Coord { get; private set; }

        [Header("Faction")]
        public Faction faction = Faction.Player;

        [Header("UI Sprites")]
        [SerializeField] private Sprite portraitSprite;   // 왼쪽 카드용
        [SerializeField] private Sprite standingSprite;   // 오른쪽 스탠딩 패널용

        public Sprite PortraitSprite => portraitSprite;
        public Sprite StandingSprite => standingSprite;

        [Header("Placement (per prefab)")]
        [Tooltip("타일 중심 기준으로 유닛을 얼마나 옮길지(월드 기준). 예: z=-0.25")]
        [SerializeField] private Vector3 tileOffset = new Vector3(0f, 0f, -0.5f);

        [Tooltip("true면 타일 로컬축 기준 오프셋")]
        [SerializeField] private bool offsetInTileSpace = false;

        public Vector3 TileOffset => tileOffset;
        public bool OffsetInTileSpace => offsetInTileSpace;

        // =========================
        // Attacks (Unit + Weapon)
        // =========================
        [Header("Attacks (Unit innate)")]
        [SerializeField] private List<AttackDef> attacks = new List<AttackDef>();
        public IReadOnlyList<AttackDef> Attacks => attacks; // (레거시/참고용) UI는 아래 GetUIAttacks를 사용

        [Header("Weapon Runtime")]
        [SerializeField] private WeaponRuntime weaponRuntime; // 상태/탄약은 여기서 관리
        public WeaponRuntime WeaponRuntime => weaponRuntime;

        [Header("Equipment")]
        [SerializeField] private int maxWeaponSlots = 1;
        public int MaxWeaponSlots => Mathf.Clamp(maxWeaponSlots, 1, 2);

        public enum AttackSource { Unit, Weapon }

        public struct UIAttackItem
        {
            public int uiSlot;                 // 0-based slot
            public AttackDef def;
            public AttackSource source;

            // Weapon 전용
            public int weaponAttackIndex;      // WeaponRuntime.def.attacks index
            public WeaponAttackEntry weaponEntry;
            public int weaponSlotIndex;        // 0=primary, 1=secondary
        }

        private readonly List<UIAttackItem> _uiAttacksCache = new();
        private bool _uiAttacksDirty = true;
        private readonly List<WeaponRuntime> _weaponRuntimes = new();

        // 공격 및 이동 플래그
        [Header("Turn Flags")]
        [SerializeField] private bool hasMovedThisTurn = false;
        [SerializeField] private bool hasAttackedThisTurn = false;

        public bool HasMovedThisTurn => hasMovedThisTurn;
        public bool HasAttackedThisTurn => hasAttackedThisTurn;

        public void MarkMoved() => hasMovedThisTurn = true;
        public void MarkAttacked() => hasAttackedThisTurn = true;

        public void ResetTurnFlags()
        {
            hasMovedThisTurn = false;
            hasAttackedThisTurn = false;
        }

        public void ClearMovedForUndo()
        {
            hasMovedThisTurn = false;
        }
        // 0-based (1번키=0)
        [SerializeField] private int activeAttackIndex = -1;

        public void MarkUIAttacksDirty() => _uiAttacksDirty = true;

        /// <summary>
        /// 무기 런타임 교체(무기 바꾸기) 시 호출용.
        /// </summary>
        public void SetWeaponRuntime(WeaponRuntime wr)
        {
            weaponRuntime = wr;
            if (weaponRuntime != null)
                weaponRuntime.slotIndex = 0;
            _uiAttacksDirty = true;
        }

        ///  ✅ 스킬/특전 보유 여부

        [Header("Skills / Perks")]
        [SerializeField] private List<string> skillIds = new();
        public IReadOnlyList<string> SkillIds => skillIds;

        public bool HasSkill(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return skillIds != null && skillIds.Contains(id);
        }

        // 런타임에서 스킬 얻거나 잃을 때 사용
        public void AddSkill(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (skillIds == null) skillIds = new List<string>();
            if (!skillIds.Contains(id)) skillIds.Add(id);
            MarkUIAttacksDirty();
        }
        public void RemoveSkill(string id)
        {
            if (string.IsNullOrEmpty(id) || skillIds == null) return;
            if (skillIds.Remove(id)) MarkUIAttacksDirty();
        }

        private bool IsSkillValidForWeapon(string skillId, WeaponType weaponType)
        {
            if (string.IsNullOrEmpty(skillId)) return true;

            // "pistol.xxx" 같은 prefix 규칙
            if (skillId.StartsWith("pistol.")) return weaponType == WeaponType.Pistol;
            if (skillId.StartsWith("rifle.")) return weaponType == WeaponType.Rifle;
            if (skillId.StartsWith("shotgun.")) return weaponType == WeaponType.Shotgun;
            if (skillId.StartsWith("melee.")) return weaponType == WeaponType.Melee;
            if (skillId.StartsWith("heavy.")) return weaponType == WeaponType.Heavy;

            // prefix 없는 스킬은 전 무기 공통 처리
            return true;
        }

        /// <summary>
        /// ✅ 유닛 고유 공격 + 무기 공격을 합쳐 "uiSlot" 기준으로 정렬한 UI 리스트
        /// </summary>
public IReadOnlyList<UIAttackItem> GetUIAttacks()
{
    if (!_uiAttacksDirty) return _uiAttacksCache;

    _uiAttacksCache.Clear();

    // 1) 유닛 고유 공격(무기 타입 무관)
    if (attacks != null)
    {
        for (int i = 0; i < attacks.Count; i++)
        {
            var a = attacks[i];
            if (a == null) continue;

            // (선택) 유닛 고유 공격도 스킬 잠금 걸고 싶으면 아래 주석 해제
            // if (a.requiresSkill && !HasSkill(a.requiredSkillId)) continue;

            _uiAttacksCache.Add(new UIAttackItem
            {
                uiSlot = Mathf.Max(0, a.uiSlot),
                def = a,
                source = AttackSource.Unit,
                weaponAttackIndex = -1,
                weaponEntry = null,
                weaponSlotIndex = -1
            });
        }
    }

    // 2) 무기 공격 (WeaponRuntime.def 기준)
    /*
    var wr = weaponRuntime != null ? weaponRuntime : GetComponent<WeaponRuntime>();
    if (wr != null && wr.def != null && wr.def.attacks != null)
    {
        var wDef = wr.def;
        var wType = wDef.weaponType; // ✅ WeaponDef에 weaponType 있어야 함

        for (int i = 0; i < wDef.attacks.Count; i++)
        {
            var entry = wDef.attacks[i];
            if (entry == null || entry.attack == null) continue;

            var atk = entry.attack;

            // ✅ 스킬 잠금 공격이면:
            // 1) 이 스킬이 현재 무기 타입에서 유효한지(prefix 규칙)
            // 2) 유닛이 해당 스킬을 가지고 있는지
            if (atk.requiresSkill)
            {
                if (!IsSkillValidForWeapon(atk.requiredSkillId, wType))
                    continue;

                if (!HasSkill(atk.requiredSkillId))
                    continue;
            }

            _uiAttacksCache.Add(new UIAttackItem
            {
                uiSlot = Mathf.Max(0, entry.uiSlot),
                def = atk,
                source = AttackSource.Weapon,
                weaponAttackIndex = i,
                weaponEntry = entry
            });
        }
    }

    // 3) 정렬: slot -> source(Unit 먼저) -> name (안정)
    */

    // 2) Weapon attacks (multiple slots)
    var wRuntimes = GetWeaponRuntimes();
    for (int w = 0; w < wRuntimes.Count; w++)
    {
        var wr = wRuntimes[w];
        if (wr == null || wr.slotIndex >= MaxWeaponSlots || wr.def == null || wr.def.attacks == null) continue;

        var wDef = wr.def;
        var wType = wDef.weaponType;

        for (int i = 0; i < wDef.attacks.Count; i++)
        {
            var entry = wDef.attacks[i];
            if (entry == null || entry.attack == null) continue;

            var atk = entry.attack;

            if (atk.requiresSkill)
            {
                if (!IsSkillValidForWeapon(atk.requiredSkillId, wType))
                    continue;

                if (!HasSkill(atk.requiredSkillId))
                    continue;
            }

            _uiAttacksCache.Add(new UIAttackItem
            {
                uiSlot = Mathf.Max(0, entry.uiSlot),
                def = atk,
                source = AttackSource.Weapon,
                weaponAttackIndex = i,
                weaponEntry = entry,
                weaponSlotIndex = wr.slotIndex
            });
        }
    }

    _uiAttacksCache.Sort((a, b) =>
    {
        int c = a.uiSlot.CompareTo(b.uiSlot);
        if (c != 0) return c;

        c = a.source.CompareTo(b.source); // Unit(0) 먼저
        if (c != 0) return c;

        c = a.weaponSlotIndex.CompareTo(b.weaponSlotIndex);
        if (c != 0) return c;

        return string.Compare(a.def.displayName, b.def.displayName, StringComparison.Ordinal);
});

    _uiAttacksDirty = false;

    // activeAttackIndex 안전 보정
    if (_uiAttacksCache.Count == 0) activeAttackIndex = -1;
    else if (activeAttackIndex >= _uiAttacksCache.Count) activeAttackIndex = _uiAttacksCache.Count - 1;

    return _uiAttacksCache;
}

        public AttackDef ActiveAttack
        {
            get
            {
                var list = GetUIAttacks();
                if (list == null || list.Count == 0) return null;
                if (activeAttackIndex < 0 || activeAttackIndex >= list.Count) return null;
                return list[activeAttackIndex].def;
            }
        }

        public UIAttackItem? ActiveUIAttackItem
        {
            get
            {
                var list = GetUIAttacks();
                if (list == null || list.Count == 0) return null;
                if (activeAttackIndex < 0 || activeAttackIndex >= list.Count) return null;
                return list[activeAttackIndex];
            }
        }

        public void ClearActiveAttack() => activeAttackIndex = -1;

        public bool SetActiveAttackByNumberKey(int numberKey1to9)
        {
            int idx = numberKey1to9 - 1;
            var list = GetUIAttacks();
            if (list == null) return false;
            if (idx < 0 || idx >= list.Count) return false;

            activeAttackIndex = idx;
            return true;
        }

        // 나중에 무기/아이템에서 편하게 쓰려고
        public void AddAttack(AttackDef def)
        {
            if (def == null) return;
            if (attacks == null) attacks = new List<AttackDef>();
            if (!attacks.Contains(def)) attacks.Add(def);

            _uiAttacksDirty = true;

            var ui = GetUIAttacks();
            if (activeAttackIndex >= ui.Count) activeAttackIndex = ui.Count - 1;
        }

        public void RemoveAttack(AttackDef def)
        {
            if (def == null || attacks == null) return;

            attacks.Remove(def);
            _uiAttacksDirty = true;

            var ui = GetUIAttacks();
            if (ui.Count == 0)
            {
                activeAttackIndex = -1;
                return;
            }
            if (activeAttackIndex >= ui.Count) activeAttackIndex = ui.Count - 1;
        }

        // =========================
        // Movement
        // =========================
        [Header("Move Stats")]
        [SerializeField] private int moveBlue = 4;
        [SerializeField] private int moveYellow = 8;

        public int MoveBlue => moveBlue;
        public int MoveYellow => moveYellow;

        private float currentMoveSpeed;

        private Vector3 targetWorldPos;
        private bool moving;

        private readonly Queue<Vector3> worldPath = new Queue<Vector3>(32);
        private Vector2Int pendingFinalCoord;
        private bool hasPendingFinalCoord;

        public bool IsMoving => moving;

        // =========================
        // Health
        // =========================
        public Health Health { get; private set; }

        private void Awake()
        {
            targetWorldPos = transform.position;

            // WeaponRuntime 자동 연결(인스펙터 미할당 대비)
            if (weaponRuntime == null)
                weaponRuntime = GetWeaponRuntimeBySlot(0);
            if (weaponRuntime != null)
                weaponRuntime.slotIndex = 0;

            Health = GetComponent<Health>();
            if (Health != null)
                Health.OnDied += HandleDied;
        }

        private void Update()
        {
            if (!moving) return;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWorldPos,
                currentMoveSpeed * Time.deltaTime
            );

            if ((transform.position - targetWorldPos).sqrMagnitude < 0.0001f)
            {
                transform.position = targetWorldPos;

                if (worldPath.Count > 0)
                {
                    targetWorldPos = worldPath.Dequeue();
                }
                else
                {
                    moving = false;

                    if (hasPendingFinalCoord)
                    {
                        Coord = pendingFinalCoord;
                        hasPendingFinalCoord = false;
                    }
                }
            }
        }

        public void SnapTo(Vector2Int coord, Vector3 worldPos)
        {
            Coord = coord;
            transform.position = worldPos;
            targetWorldPos = worldPos;

            worldPath.Clear();
            moving = false;
        }

        public void MoveAlongPath(Vector2Int finalCoord, IList<Vector3> pathWorld, float speed)
        {
            pendingFinalCoord = finalCoord;
            hasPendingFinalCoord = true;
            currentMoveSpeed = speed;

            worldPath.Clear();

            if (pathWorld == null || pathWorld.Count == 0)
            {
                moving = false;
                hasPendingFinalCoord = false;
                return;
            }

            int startIdx = 0;
            if ((transform.position - pathWorld[0]).sqrMagnitude < 0.0001f)
                startIdx = 1;

            for (int i = startIdx; i < pathWorld.Count; i++)
                worldPath.Enqueue(pathWorld[i]);

            if (worldPath.Count == 0)
            {
                moving = false;
                hasPendingFinalCoord = false;
                return;
            }

            targetWorldPos = worldPath.Dequeue();
            moving = true;
        }

        private void HandleDied(object source)
        {
            Debug.Log($"[Unit] {name} died");
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            worldPath.Clear();
            moving = false;
        }

        public IReadOnlyList<WeaponRuntime> GetWeaponRuntimes()
        {
            _weaponRuntimes.Clear();
            GetComponents(_weaponRuntimes);

            _weaponRuntimes.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));
            return _weaponRuntimes;
        }

        public WeaponRuntime GetWeaponRuntimeBySlot(int slotIndex)
        {
            var list = GetWeaponRuntimes();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].slotIndex == slotIndex)
                    return list[i];
            }
            return null;
        }

        public WeaponRuntime GetPrimaryWeaponRuntime()
        {
            return GetWeaponRuntimeBySlot(0);
        }

        public WeaponRuntime GetActiveWeaponRuntime()
        {
            var active = ActiveUIAttackItem;
            if (active.HasValue && active.Value.source == AttackSource.Weapon)
                return GetWeaponRuntimeBySlot(active.Value.weaponSlotIndex);
            return GetPrimaryWeaponRuntime();
        }
    }
}
