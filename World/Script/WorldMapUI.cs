using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WorldLayoutSO layout;

    [SerializeField] private RectTransform content;   // ScrollRect Content
    [SerializeField] private RectTransform linesRoot;
    [SerializeField] private RectTransform nodesRoot;

    [Header("Prefabs")]
    [SerializeField] private WorldMapNodeButton nodeButtonPrefab;
    [SerializeField] private UILine linePrefab;

    [Header("Tuning")]
    [SerializeField] private float layoutToUiScale = 120f;
    [SerializeField] private float padding = 200f;
    [SerializeField] private float lineThickness = 6f;

    [Header("Info Panel")]
    [SerializeField] private TMP_Text roomIdText;
    [SerializeField] private TMP_Text roomKindText;

    [Header("Movement Rule")]
    [SerializeField] private bool restrictToNeighbors = true;

    private readonly Dictionary<string, WorldMapNodeButton> spawnedNodes = new();
    private readonly List<UILine> spawnedLines = new();

    public System.Action<string> OnNodeClicked;

    private WorldRunGraph boundWorld;

    private void Start()
    {
    }

    public void BindWorld(WorldRunGraph world)
    {
        boundWorld = world;
        Build();
        RefreshFromWorld(boundWorld);
    }

    private bool CanMoveToNode(string fromId, string toId) //인접 노드 이동 제한 여부 확인
    {
        if (!restrictToNeighbors)
            return true;

        if (boundWorld == null)
            return false;

        if (!boundWorld.TryGetNode(fromId, out var fromNode) || fromNode == null)
            return false;

        return fromNode.neighbors != null && fromNode.neighbors.Contains(toId);
    }

    public void SetRestrictToNeighbors(bool value) //인접 노드 이동 제한 설정
    {
        restrictToNeighbors = value;
        Debug.Log($"[WorldMapUI] restrictToNeighbors = {restrictToNeighbors}");
    }

    public void Build()
    {
        ClearAll();

        if (layout == null || content == null || nodesRoot == null || linesRoot == null)
        {
            Debug.LogError("[WorldMapUI] refs missing");
            return;
        }
        if (nodeButtonPrefab == null || linePrefab == null)
        {
            Debug.LogError("[WorldMapUI] prefabs missing");
            return;
        }

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (var ln in layout.nodes)
        {
            Vector2 uiPos = GetLayoutNodeUiPosition(ln);

            min = Vector2.Min(min, uiPos);
            max = Vector2.Max(max, uiPos);

            var btn = Instantiate(nodeButtonPrefab, nodesRoot);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = uiPos;

            btn.Bind(ln.id, ln.id, HandleNodeClicked);

            spawnedNodes[ln.id] = btn;
        }

        Vector2 size = (max - min) + Vector2.one * padding * 2f;
        if (size.x < 500f) size.x = 500f;
        if (size.y < 500f) size.y = 500f;

        content.sizeDelta = size;

        Vector2 center = (min + max) * 0.5f;
        foreach (var kv in spawnedNodes)
        {
            var rt = (RectTransform)kv.Value.transform;
            rt.anchoredPosition -= center;
        }

        DrawLines();

        content.anchoredPosition = Vector2.zero;

        if (boundWorld != null)
            RefreshFromWorld(boundWorld);
    }

    private void HandleNodeClicked(string id)
    {
        Debug.Log($"[WorldMapUI] Node clicked: {id}");

        OnNodeClicked?.Invoke(id);

        if (GameRunManager.Instance == null)
        {
            Debug.LogError("[WorldMapUI] GameRunManager.Instance is null");
            return;
        }

        if (string.IsNullOrEmpty(id) || GameRunManager.Instance.CurrentRoomId == id)
            return;

        bool moved = GameRunManager.Instance.TryMoveTo(id);

        Debug.Log($"[WorldMapUI] TryMoveTo result: {moved}");

        if (!moved)
            return;

        boundWorld = GameRunManager.Instance.World;
        RefreshFromWorld(boundWorld);
    }

    private void DrawLines()
    {
        foreach (var link in layout.links)
        {
            if (!spawnedNodes.TryGetValue(link.a, out var aBtn)) continue;
            if (!spawnedNodes.TryGetValue(link.b, out var bBtn)) continue;

            var line = Instantiate(linePrefab, linesRoot);
            line.SetThickness(lineThickness);

            Vector2 aPos = ((RectTransform)aBtn.transform).anchoredPosition;
            Vector2 bPos = ((RectTransform)bBtn.transform).anchoredPosition;

            line.SetPoints(aPos, bPos);
            spawnedLines.Add(line);
        }
    }

    public void RefreshFromWorld(WorldRunGraph world)
    {
        if (world == null) return;

        foreach (var kv in spawnedNodes)
        {
            string id = kv.Key;
            var btn = kv.Value;

            btn.SetCurrent(id == world.currentRoomId);
            btn.SetRoomId(id);

            if (!world.TryGetNode(id, out var node) || node == null)
            {
                btn.SetRoomKind("???");
                Debug.LogWarning($"[WorldMapUI] Node missing in world: {id}");
                continue;
            }

            string kindText = "???";
            if (node.IsDiscovered && node.def != null)
            {
                kindText = node.def.displayName;
            }

            btn.SetRoomKind(kindText);

            string defInfo = (node.def != null)
                ? $"{node.def.roomId}/{node.def.displayName}/{node.def.type}"
                : "NULL_DEF";

            Debug.Log($"[WorldMapUI] Refresh {id} -> discovered={node.IsDiscovered}, def={defInfo}, UIKind={kindText}");
        }

        RefreshInfoPanel(world);
    }

    // layout 데이터용 좌표 계산
    private Vector2 GetLayoutNodeUiPosition(WorldLayoutSO.LayoutNode node)
    {
        Vector2 pos;

        pos.x = node.position.x * layout.nodeSpacing.x;
        pos.y = node.position.y * layout.nodeSpacing.y;

        pos.y += node.floor * layout.floorVerticalSpacing;

        return pos * layoutToUiScale;
    }

    // runtime 노드용 좌표 계산
    private Vector2 GetWorldMapPosition(RoomNodeRuntime node)
    {
        float floorOffsetY = node.floor * layout.floorVerticalSpacing;
        Vector2 finalPos = new Vector2(node.position.x, node.position.y + floorOffsetY);
        return finalPos * layoutToUiScale;
    }

    private void ClearAll()
    {
        foreach (var l in spawnedLines)
            if (l) Destroy(l.gameObject);
        spawnedLines.Clear();

        foreach (var kv in spawnedNodes)
            if (kv.Value) Destroy(kv.Value.gameObject);
        spawnedNodes.Clear();
    }

    private void RefreshInfoPanel(WorldRunGraph world)
    {
        if (world == null) return;

        string id = world.currentRoomId;

        if (roomIdText)
            roomIdText.text = id;

        if (roomKindText)
        {
            if (world.TryGetNode(id, out var node) && node != null && node.IsDiscovered && node.def != null)
                roomKindText.text = node.def.displayName;
            else
                roomKindText.text = "???";
        }
    }
}