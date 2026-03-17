using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TacticsGrid;
using UVoK.Inventory;

public enum BattleResult
{
    None,
    PlayerVictory,
    PlayerDefeat
}

public class GameRunManager : MonoBehaviour
{
    public static GameRunManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string worldSceneName = "WorldScene";
    [SerializeField] private string battleSceneName = "BattleScene";

    [Header("Run Setup")]
    [SerializeField] private WorldLayoutSO worldLayout;
    [SerializeField] private RoomPoolSO mandatoryPool;
    [SerializeField] private RoomPoolSO optionalPool;

    [Header("Party")]

    [SerializeField] private int defaultDeployCount = 3;
    [SerializeField] private List<TacticsGrid.Unit> UnitPrefabs = new();
    public List<PartyMemberRuntimeData> CurrentParty = new();

    [Header("Inventory (Shared)")]
    [SerializeField] private List<ItemData> startingItems = new();


    [SerializeField] private int inventoryGridWidth = 8;
    [SerializeField] private int inventoryGridHeight = 8;
    [SerializeField] private int quickSlotCount = 4;
    public RunInventory Inventory { get; private set; } = new RunInventory();
    public List<QuickSlotRuntimeData> QuickSlots { get; private set; } = new();

    [Header("Debug")]
    [SerializeField] private bool autoCreateIfMissingWorld = false;
    [SerializeField] private bool logEnabled = true;

    public WorldRunGraph World { get; private set; }

    public bool HasActiveRun => World != null;
    public string CurrentRoomId => World != null ? World.currentRoomId : null;

    // 전투 진입 직전/직후 추적용
    public string PendingBattleRoomId { get; private set; }

    // 필요하면 나중에 보상/이벤트 전달용으로 확장
    public BattleResult LastBattleResult { get; private set; } = BattleResult.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Run Lifecycle

    public void CreateNewRun()
    {
        if (worldLayout == null)
        {
            Debug.LogError("[RunManager] worldLayout is missing.");
            return;
        }

        World = new WorldRunGraph();
        World.Initialize(worldLayout, mandatoryPool, optionalPool);

        InitializeInventory(true);
        InitializeStartingParty();

        PendingBattleRoomId = null;
        LastBattleResult = BattleResult.None;

        Log($"CreateNewRun -> startId={World.startId}, currentRoomId={World.currentRoomId}");

        // 시작 방 진입 처리
        if (World.TryGetNode(World.currentRoomId, out var startNode) && startNode != null)
        {
            startNode.OnEnter();
            startNode.visited = true;
        }

        SceneManager.LoadScene(worldSceneName);
    }

    public void EndRunAndReturnToMain()
    {
        Log("EndRunAndReturnToMain");

        World = null;
        PendingBattleRoomId = null;
        LastBattleResult = BattleResult.None;

        SceneManager.LoadScene(mainSceneName);
    }

    #endregion




    #region World Access

    public bool TryGetCurrentNode(out RoomNodeRuntime node)
    {
        node = null;

        if (World == null || string.IsNullOrEmpty(World.currentRoomId))
            return false;

        return World.TryGetNode(World.currentRoomId, out node);
    }

    public bool TryGetNode(string nodeId, out RoomNodeRuntime node)
    {
        node = null;
        if (World == null || string.IsNullOrEmpty(nodeId))
            return false;

        return World.TryGetNode(nodeId, out node);
    }

    public bool IsCurrentRoom(string nodeId)
    {
        return World != null && World.currentRoomId == nodeId;
    }

    #endregion

    #region Unit Management
    public int GetCurrentPartySize()
    {
        int count = 0;

        for (int i = 0; i < CurrentParty.Count; i++)
        {
            var m = CurrentParty[i];
            if (m != null && !m.isDead)
                count++;
        }

        return count;
    }

    public float GetEncounterMultiplier()
    {
        int size = GetCurrentPartySize();

        switch (size)
        {
            case 0: return 1f;
            case 1: return 0.75f;
            case 2: return 0.9f;
            case 3: return 1.0f;
            case 4: return 1.25f;
            default: return 1.5f;
        }
    }

    private void InitializeStartingParty()
    {
        CurrentParty.Clear();

        for (int i = 0; i < UnitPrefabs.Count; i++)
        {
            var prefab = UnitPrefabs[i];
            if (prefab == null) continue;

            var hp = prefab.GetComponent<Health>();
            var res = prefab.GetComponent<TacticsGrid.UnitResources>();
            var weapons = prefab.GetComponents<WeaponRuntime>();
            WeaponRuntime primary = null;
            WeaponRuntime secondary = null;

            if (weapons != null && weapons.Length > 0)
            {
                System.Array.Sort(weapons, (a, b) => a.slotIndex.CompareTo(b.slotIndex));
                primary = weapons[0];
                if (weapons.Length > 1) secondary = weapons[1];
            }

            if (prefab.MaxWeaponSlots < 2)
                secondary = null;

            CurrentParty.Add(new PartyMemberRuntimeData
            {
                memberId = prefab.name,
                unitPrefab = prefab,
                currentHP = hp != null ? hp.MaxHP : 1,
                maxHP = hp != null ? hp.MaxHP : 1,
                currentAP = res != null ? res.MaxAP : 2,
                isDead = false,

                weaponPrimaryItem = FindItemDataByWeaponDef(primary != null ? primary.def : null),
                weaponPrimaryAmmo = primary != null && primary.def != null ? primary.def.magazineSize : 0,

                weaponSecondaryItem = FindItemDataByWeaponDef(secondary != null ? secondary.def : null),
                weaponSecondaryAmmo = secondary != null && secondary.def != null ? secondary.def.magazineSize : 0,

                armorItem = null,
                accessoryItem = null
            });
        }

        Debug.Log($"CurrentParty Count = {GameRunManager.Instance.CurrentParty.Count}");
    }

    private ItemData FindItemDataByWeaponDef(WeaponDef def)
    {
        if (def == null || startingItems == null)
            return null;

        for (int i = 0; i < startingItems.Count; i++)
        {
            var item = startingItems[i];
            if (item != null && item.weaponDef == def)
                return item;
        }

        return null;
    }
    private void InitializeInventory(bool force)
    {
        if (Inventory == null)
            Inventory = new RunInventory();

        Inventory.EnsureGrid(inventoryGridWidth, inventoryGridHeight);

        if (!force && Inventory.Items != null && Inventory.Items.Count > 0)
            return;

        Inventory.Clear();

        if (startingItems != null)
        {
            for (int i = 0; i < startingItems.Count; i++)
            {
                var item = startingItems[i];
                if (item == null)
                    continue;

                Inventory.AddItem(item, 1);
            }
        }

        InitializeQuickSlots();
    }
    private void InitializeQuickSlots()
    {
        if (QuickSlots == null)
            QuickSlots = new List<QuickSlotRuntimeData>();

        if (quickSlotCount < 1) quickSlotCount = 1;
        while (QuickSlots.Count < quickSlotCount)
            QuickSlots.Add(new QuickSlotRuntimeData());
        if (QuickSlots.Count > quickSlotCount)
            QuickSlots.RemoveRange(quickSlotCount, QuickSlots.Count - quickSlotCount);
    }

    public bool TryEquipWeapon(string memberId, ItemData item, int slotIndex)
    {
        if (string.IsNullOrEmpty(memberId) || item == null || item.weaponDef == null)
            return false;

        var member = FindPartyMember(memberId);
        if (member == null)
            return false;

        int maxSlots = GetMaxWeaponSlots(member);
        if (slotIndex < 0 || slotIndex >= maxSlots)
            return false;

        if (!Inventory.HasItem(item, 1))
            return false;

        var oldItem = (slotIndex == 0) ? member.weaponPrimaryItem : member.weaponSecondaryItem;

        if (!Inventory.RemoveItem(item, 1))
            return false;

        if (oldItem != null)
            Inventory.AddItem(oldItem, 1);

        SetWeaponSlot(member, slotIndex, item, item.weaponDef.magazineSize);
        return true;
    }

    public bool TryUnequipWeapon(string memberId, int slotIndex)
    {
        if (string.IsNullOrEmpty(memberId))
            return false;

        var member = FindPartyMember(memberId);
        if (member == null)
            return false;

        int maxSlots = GetMaxWeaponSlots(member);
        if (slotIndex < 0 || slotIndex >= maxSlots)
            return false;

        var item = (slotIndex == 0) ? member.weaponPrimaryItem : member.weaponSecondaryItem;
        if (item == null)
            return false;

        Inventory.AddItem(item, 1);
        SetWeaponSlot(member, slotIndex, null, 0);
        return true;
    }

    public bool TryEquipArmor(string memberId, ItemData item)
    {
        if (string.IsNullOrEmpty(memberId) || item == null || item.armorDef == null) return false;
        var member = FindPartyMember(memberId);
        if (member == null) return false;

        if (!Inventory.RemoveItem(item, 1)) return false;

        if (member.armorItem != null)
            Inventory.AddItem(member.armorItem, 1);

        member.armorItem = item;
        return true;
    }

    public bool TryUnequipArmor(string memberId)
    {
        if (string.IsNullOrEmpty(memberId)) return false;
        var member = FindPartyMember(memberId);
        if (member == null || member.armorItem == null) return false;

        Inventory.AddItem(member.armorItem, 1);
        member.armorItem = null;
        return true;
    }

    public bool TryEquipAccessory(string memberId, ItemData item)
    {
        if (string.IsNullOrEmpty(memberId) || item == null || item.accessoryDef == null)
            return false;

        var member = FindPartyMember(memberId);
        if (member == null)
            return false;

        if (!Inventory.RemoveItem(item, 1))
            return false;

        if (member.accessoryItem != null)
            Inventory.AddItem(member.accessoryItem, 1);

        member.accessoryItem = item;
        return true;
    }

    public bool TryUnequipAccessory(string memberId)
    {
        if (string.IsNullOrEmpty(memberId))
            return false;

        var member = FindPartyMember(memberId);
        if (member == null || member.accessoryItem == null)
            return false;

        Inventory.AddItem(member.accessoryItem, 1);
        member.accessoryItem = null;
        return true;
    }
    private PartyMemberRuntimeData FindPartyMember(string memberId)
    {
        if (CurrentParty == null) return null;
        for (int i = 0; i < CurrentParty.Count; i++)
        {
            var m = CurrentParty[i];
            if (m != null && m.memberId == memberId)
                return m;
        }
        return null;
    }

    private int GetMaxWeaponSlots(PartyMemberRuntimeData member)
    {
        if (member == null || member.unitPrefab == null) return 1;
        return member.unitPrefab.MaxWeaponSlots;
    }

    private void SetWeaponSlot(PartyMemberRuntimeData member, int slotIndex, ItemData item, int ammo)
    {
        if (member == null)
            return;

        if (slotIndex == 0)
        {
            member.weaponPrimaryItem = item;
            member.weaponPrimaryAmmo = ammo;
        }
        else
        {
            member.weaponSecondaryItem = item;
            member.weaponSecondaryAmmo = ammo;
        }
    }
    #endregion

    #region Movement

    public bool CanMoveTo(string targetRoomId, bool restrictToNeighbors = true)
    {
        if (World == null)
            return false;

        if (string.IsNullOrEmpty(targetRoomId))
            return false;

        if (World.currentRoomId == targetRoomId)
            return false;

        if (!World.TryGetNode(targetRoomId, out var targetNode) || targetNode == null)
            return false;

        if (!restrictToNeighbors)
            return true;

        if (!World.TryGetNode(World.currentRoomId, out var currentNode) || currentNode == null)
            return false;

        return currentNode.neighbors != null && currentNode.neighbors.Contains(targetRoomId);
    }

    public bool TryMoveTo(string targetRoomId, bool restrictToNeighbors = true)
    {
        Debug.Log($"[RunManager] TryMoveTo called -> target={targetRoomId}");

        if (!CanMoveTo(targetRoomId, restrictToNeighbors))
        {
            Debug.LogWarning($"[RunManager] CanMoveTo failed -> target={targetRoomId}");
            return false;
        }

        string prev = World.currentRoomId;
        World.currentRoomId = targetRoomId;

        if (!World.TryGetNode(targetRoomId, out var node))
        {
            Debug.LogWarning($"[RunManager] Target node not found -> {targetRoomId}");
            return false;
        }

        bool firstVisit = !node.visited;

        Debug.Log($"[RunManager] Before enter -> node={node.id}, visited={node.visited}, cleared={node.cleared}, firstVisit={firstVisit}");

        node.visited = true;
        node.OnEnter();

        Debug.Log($"[RunManager] Move {prev} -> {targetRoomId}");

        var evt = DecideEvent(node, firstVisit);

        Debug.Log($"[RunManager] DecideEvent -> {evt}");

        if (evt != RunEventType.None)
        {
            TriggerEvent(evt, node);
        }

        return true;
    }

    private void TriggerEvent(RunEventType evt, RoomNodeRuntime node)
    {
        switch (evt)
        {
            case RunEventType.Battle:

                Debug.Log($"[RunManager] Battle triggered at {node.id}");

                StartBattleForNode(node.id);

                break;
        }
    }
    private RunEventType DecideEvent(RoomNodeRuntime node, bool firstVisit)
    {
        if (!firstVisit)
            return RunEventType.None;

        if (node.cleared)
            return RunEventType.None;

        // 지금은 테스트니까 전부 전투
        return RunEventType.Battle;
    }


    private bool ShouldStartBattleOnEnter(RoomNodeRuntime node, bool firstVisit)
    {
        if (node == null)
            return false;

        // 지금은 단순 규칙:
        // "새로운 타일 들어갔을 때 전투"
        // 단, 이미 클리어된 방은 제외
        if (!firstVisit)
            return false;

        if (node.cleared)
            return false;

        // 나중에 RoomDef.type으로 상점/이벤트/휴식 분기 가능
        return true;
    }

    #endregion

    #region Battle Flow

    public void StartBattleForCurrentRoom()
    {
        if (World == null)
        {
            Debug.LogWarning("[RunManager] StartBattleForCurrentRoom failed: World is null.");
            return;
        }

        StartBattleForNode(World.currentRoomId);
    }

    public void StartBattleForNode(string nodeId)
    {
        if (World == null)
        {
            Debug.LogWarning("[RunManager] StartBattleForNode failed: World is null.");
            return;
        }

        if (string.IsNullOrEmpty(nodeId))
        {
            Debug.LogWarning("[RunManager] StartBattleForNode failed: nodeId is null or empty.");
            return;
        }

        if (!World.TryGetNode(nodeId, out var node) || node == null)
        {
            Debug.LogWarning($"[RunManager] StartBattleForNode failed: node not found ({nodeId}).");
            return;
        }

        PendingBattleRoomId = nodeId;
        node.battleInProgress = true;
        LastBattleResult = BattleResult.None;

        Log($"StartBattleForNode -> {nodeId}");

        SceneManager.LoadScene(battleSceneName);
    }

    public void EndBattle(BattleResult result)
    {
        if (World == null)
        {
            Debug.LogWarning("[RunManager] EndBattle failed: World is null.");
            return;
        }

        if (string.IsNullOrEmpty(PendingBattleRoomId))
        {
            Debug.LogWarning("[RunManager] EndBattle failed: PendingBattleRoomId is empty.");
            return;
        }

        if (!World.TryGetNode(PendingBattleRoomId, out var node) || node == null)
        {
            Debug.LogWarning($"[RunManager] EndBattle failed: node not found ({PendingBattleRoomId}).");
            PendingBattleRoomId = null;
            SceneManager.LoadScene(worldSceneName);
            return;
        }

        node.battleInProgress = false;
        LastBattleResult = result;

        switch (result)
        {
            case BattleResult.PlayerVictory:
                node.cleared = true;
                Log($"Battle ended -> PlayerVictory at {node.id}");
                SceneManager.LoadScene(worldSceneName);
                break;

            case BattleResult.PlayerDefeat:
                Log($"Battle ended -> PlayerDefeat at {node.id}");
                // 지금은 일단 메인으로 되돌림.
                // 테스트용으로 월드 복귀시키고 싶으면 worldSceneName으로 바꿔도 됨.
                EndRunAndReturnToMain();
                break;

            default:
                Log($"Battle ended -> None at {node.id}");
                SceneManager.LoadScene(worldSceneName);
                break;
        }

        PendingBattleRoomId = null;
    }

    #endregion

    #region Scene Hooks

    public void EnsureWorldExists()
    {
        if (World != null)
            return;

        if (autoCreateIfMissingWorld)
        {
            Debug.LogWarning("[RunManager] World missing. Auto creating new run.");
            CreateNewRun();
            return;
        }

        Debug.LogWarning("[RunManager] World is missing.");
    }

    #endregion

    #region Debug / Utility

    private void Log(string msg)
    {
        if (logEnabled)
            Debug.Log($"[RunManager] {msg}");
    }


    public void EnsureWorldGraphInitialized()
    {
        if (World != null)
            return;

        if (worldLayout == null)
        {
            Debug.LogError("[RunManager] worldLayout is missing.");
            return;
        }

        World = new WorldRunGraph();
        World.Initialize(worldLayout, mandatoryPool, optionalPool);

        InitializeInventory(false);
        if (CurrentParty == null || CurrentParty.Count == 0)
            InitializeStartingParty();

        PendingBattleRoomId = null;
        LastBattleResult = BattleResult.None;

        Log($"EnsureWorldGraphInitialized -> startId={World.startId}, currentRoomId={World.currentRoomId}");

        if (World.TryGetNode(World.currentRoomId, out var startNode) && startNode != null)
        {
            startNode.OnEnter();
            startNode.visited = true;
        }

        Debug.Log($"CurrentParty Count = {GameRunManager.Instance.CurrentParty.Count}");
    }
    #endregion
}
