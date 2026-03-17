using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TacticsGrid;

namespace UVoK.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 6;
        [SerializeField] private bool keepCellsSquare = true;

        [Header("Refs")]
        [SerializeField] private RectTransform cellRoot;
        [SerializeField] private RectTransform itemLayer;
        [SerializeField] private InventoryCellUI cellPrefab;
        [SerializeField] private InventoryItemUI itemPrefab;
        [SerializeField] private Canvas canvas;

        [Header("Equipment")]
        [SerializeField] private CharacterEquipmentPanel equipmentPanel;

        [Header("Debug")]
        [SerializeField] private bool useTestSpawn = false;


        [Header("Test Spawn")]
        [SerializeField] private List<TestSpawnEntry> testSpawnItems = new();


        [System.Serializable]
        public class TestSpawnEntry
        {
            public ItemData itemData;
            public Vector2Int position;
            [Min(1)] public int quantity = 1;
        }

        private InventoryGrid grid;
        private readonly Dictionary<InventoryItem, InventoryItemUI> itemUIMap = new();
        private readonly List<InventoryCellUI> cellUIs = new();

        private readonly List<WorldPartyCardUI> linkedPartyCards = new();
        private PartyMemberRuntimeData selectedMember;

        private InventoryItem draggedItem;
        private InventoryItemUI draggedItemUI;
        private Vector2Int draggedItemOriginalPos;
        private bool draggedItemOriginalRotated;
        private Vector2 dragOffset;

        private RectTransform panelRect;
        private float cellWidth;
        private float cellHeight;
        private float gridRenderWidth;
        private float gridRenderHeight;

        public InventoryGrid Grid => grid;
        public PartyMemberRuntimeData SelectedMember => selectedMember;

        private void Awake()
        {
            panelRect = GetComponent<RectTransform>();
        }

        private void Start()
        {
            grid = new InventoryGrid(gridWidth, gridHeight);
            RebuildLayout();

            if (useTestSpawn)
                SpawnTestItems();
        }

        private void Update()
        {
            HandleDragInput();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying) return;
            if (panelRect == null) return;

            RebuildLayout();
            RefreshAllItemUIs();
        }

        public void RebuildLayout()
        {
            if (panelRect == null)
                panelRect = GetComponent<RectTransform>();

            if (panelRect == null || cellRoot == null || itemLayer == null)
            {
                Debug.LogWarning("[InventoryUI] panelRect / cellRoot / itemLayer missing.");
                return;
            }

            float panelWidth = panelRect.rect.width;
            float panelHeight = panelRect.rect.height;

            if (panelWidth <= 0 || panelHeight <= 0 || gridWidth <= 0 || gridHeight <= 0)
                return;

            if (keepCellsSquare)
            {
                float size = Mathf.Min(panelWidth / gridWidth, panelHeight / gridHeight);
                cellWidth = size;
                cellHeight = size;
            }
            else
            {
                cellWidth = panelWidth / gridWidth;
                cellHeight = panelHeight / gridHeight;
            }

            gridRenderWidth = cellWidth * gridWidth;
            gridRenderHeight = cellHeight * gridHeight;

            SetupLayerRect(cellRoot, 0f, 0f, gridRenderWidth, gridRenderHeight);
            SetupLayerRect(itemLayer, 0f, 0f, gridRenderWidth, gridRenderHeight);

            BuildCells();
        }

        private void SetupLayerRect(RectTransform target, float x, float y, float width, float height)
        {
            if (target == null) return;

            target.anchorMin = new Vector2(0, 1);
            target.anchorMax = new Vector2(0, 1);
            target.pivot = new Vector2(0, 1);
            target.anchoredPosition = new Vector2(x, y);
            target.sizeDelta = new Vector2(width, height);
        }

        private void BuildCells()
        {
            if (cellRoot == null || cellPrefab == null)
            {
                Debug.LogError("[InventoryUI] cellRoot 또는 cellPrefab이 비어 있음.");
                return;
            }

            for (int i = cellRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(cellRoot.GetChild(i).gameObject);
            }
            cellUIs.Clear();

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    InventoryCellUI cell = Instantiate(cellPrefab, cellRoot);
                    RectTransform rt = cell.GetComponent<RectTransform>();

                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(cellWidth, cellHeight);
                    rt.anchoredPosition = GridToAnchoredPosition(new Vector2Int(x, y));

                    cell.SetDefault();
                    cellUIs.Add(cell);
                }
            }
        }

        private void SpawnTestItems()
        {
            if (testSpawnItems == null || testSpawnItems.Count == 0)
                return;

            for (int i = 0; i < testSpawnItems.Count; i++)
            {
                var entry = testSpawnItems[i];
                if (entry == null || entry.itemData == null)
                    continue;

                int remaining = Mathf.Max(1, entry.quantity);

                while (remaining > 0)
                {
                    int stackSize = entry.itemData.stackable
                        ? Mathf.Min(remaining, entry.itemData.maxStack)
                        : 1;

                    InventoryItem item = new InventoryItem(entry.itemData, entry.position, stackSize);

                    if (grid.PlaceItem(item, entry.position))
                    {
                        CreateItemUI(item);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[InventoryUI] 테스트 아이템 배치 실패: {entry.itemData.itemName} " +
                            $"at {entry.position} (qty={stackSize})"
                        );
                        break;
                    }

                    remaining -= stackSize;
                }
            }
        }

        private void CreateItemUI(InventoryItem item)
        {
            if (itemLayer == null || itemPrefab == null)
            {
                Debug.LogError("[InventoryUI] itemLayer 또는 itemPrefab이 비어 있음.");
                return;
            }

            InventoryItemUI ui = Instantiate(itemPrefab, itemLayer);
            ui.Bind(item, grid);

            itemUIMap[item] = ui;
            RefreshItemUI(item);
        }

        private void RefreshAllItemUIs()
        {
            foreach (var pair in itemUIMap)
            {
                RefreshItemUI(pair.Key);
            }
        }

        private void RefreshItemUI(InventoryItem item)
        {
            if (item == null) return;
            if (!itemUIMap.TryGetValue(item, out InventoryItemUI ui)) return;

            RectTransform rt = ui.RectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);

            ui.RefreshVisual(item.Width * cellWidth, item.Height * cellHeight, item.rotated);

            Vector2 centerPos = GridToCenterPosition(item.position, item.Width, item.Height);
            ui.SetCenteredPosition(centerPos);
        }

        private Vector2 GridToAnchoredPosition(Vector2Int gridPos)
        {
            return new Vector2(gridPos.x * cellWidth, -gridPos.y * cellHeight);
        }

        private bool ScreenToLocalPoint(Vector2 screenPos, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;

            if (itemLayer == null) return false;

            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(itemLayer, screenPos, cam, out localPoint);
        }

        private Vector2Int LocalPointToGrid(Vector2 localPoint)
        {
            int x = Mathf.FloorToInt(localPoint.x / cellWidth);
            int y = Mathf.FloorToInt(-localPoint.y / cellHeight);
            return new Vector2Int(x, y);
        }

        private InventoryItemUI FindItemUIUnderMouse()
        {
            Vector2 mouse = Input.mousePosition;

            foreach (var pair in itemUIMap)
            {
                RectTransform rt = pair.Value.RectTransform;
                Camera cam = null;
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    cam = canvas.worldCamera;

                if (RectTransformUtility.RectangleContainsScreenPoint(rt, mouse, cam))
                    return pair.Value;
            }

            return null;
        }

        private void HandleDragInput()
        {
            if (EventSystem.current != null && Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
            {
                TryBeginDrag();
            }

            if (draggedItem != null)
            {
                UpdateDraggedItemPosition();
                UpdatePlacementHighlight();

                if (Input.GetKeyDown(KeyCode.R))
                {
                    TryRotateDraggedItem();
                }

                if (Input.GetMouseButtonUp(0))
                {
                    EndDrag();
                }
            }
            else
            {
                ClearHighlights();
            }
        }

        private void TryBeginDrag()
        {
            InventoryItemUI hitUI = FindItemUIUnderMouse();
            if (hitUI == null) return;

            draggedItemUI = hitUI;
            draggedItem = hitUI.BoundItem;
            draggedItemOriginalPos = draggedItem.position;
            draggedItemOriginalRotated = draggedItem.rotated;

            grid.ClearItemCells(draggedItem);

            if (ScreenToLocalPoint(Input.mousePosition, out Vector2 localPoint))
            {
                Vector2 itemCenter = GridToAnchoredPosition(draggedItem.position)
                                     + new Vector2(draggedItem.Width * cellWidth * 0.5f,
                                                   -draggedItem.Height * cellHeight * 0.5f);

                dragOffset = localPoint - itemCenter;
            }
            else
            {
                dragOffset = Vector2.zero;
            }

            draggedItemUI.transform.SetAsLastSibling();
        }

        public void SetPartyCards(IEnumerable<WorldPartyCardUI> cards)
        {
            linkedPartyCards.Clear();

            if (cards == null)
            {
                RefreshPartyCardSelection();
                return;
            }

            foreach (var card in cards)
            {
                if (card == null)
                    continue;

                linkedPartyCards.Add(card);
                card.OnClicked = SelectMember;
            }

            if (selectedMember == null)
            {
                for (int i = 0; i < linkedPartyCards.Count; i++)
                {
                    var bound = linkedPartyCards[i].BoundData;
                    if (bound != null)
                    {
                        SelectMember(bound);
                        break;
                    }
                }
            }
            else
            {
                RefreshPartyCardSelection();
            }
        }

        private void RefreshPartyCardSelection()
        {
            for (int i = 0; i < linkedPartyCards.Count; i++)
            {
                var card = linkedPartyCards[i];
                if (card == null)
                    continue;

                var bound = card.BoundData;
                bool isSelected = selectedMember != null &&
                                  bound != null &&
                                  bound.memberId == selectedMember.memberId;

                card.SetSelected(isSelected);
            }
        }

        private Vector2 GridToCenterPosition(Vector2Int gridPos, int itemWidth, int itemHeight)
        {
            return GridToAnchoredPosition(gridPos)
                   + new Vector2(itemWidth * cellWidth * 0.5f,
                                 -itemHeight * cellHeight * 0.5f);
        }
        private Vector2Int LocalCenterPointToGrid(Vector2 localCenterPoint, int itemWidth, int itemHeight)
        {
            float topLeftX = localCenterPoint.x - (itemWidth * cellWidth * 0.5f);
            float topLeftY = localCenterPoint.y + (itemHeight * cellHeight * 0.5f);

            int x = Mathf.RoundToInt(topLeftX / cellWidth);
            int y = Mathf.RoundToInt(-topLeftY / cellHeight);

            return new Vector2Int(x, y);
        }

        private void UpdateDraggedItemPosition()
        {
            if (draggedItemUI == null) return;

            if (ScreenToLocalPoint(Input.mousePosition, out Vector2 localPoint))
            {
                Vector2 centerPos = localPoint - dragOffset;
                draggedItemUI.SetCenteredPosition(centerPos);
            }
        }

        private void TryRotateDraggedItem()
        {
            if (draggedItem == null || draggedItem.data == null || !draggedItem.data.rotatable)
                return;

            draggedItem.rotated = !draggedItem.rotated;

            // 점유 공간 자체가 바뀜
            draggedItemUI.RefreshVisual(
                draggedItem.Width * cellWidth,
                draggedItem.Height * cellHeight,
                draggedItem.rotated
            );

            UpdatePlacementHighlight();
        }

        private void UpdatePlacementHighlight()
        {
            ClearHighlights();

            if (equipmentPanel != null)
                equipmentPanel.ClearHighlights();

            if (draggedItem == null)
                return;

            if (equipmentPanel != null && draggedItem.data != null)
                equipmentPanel.HighlightValidSlotFor(draggedItem.data);

            if (!ScreenToLocalPoint(Input.mousePosition, out Vector2 localPoint))
                return;

            Vector2 centerPos = localPoint - dragOffset;
            Vector2Int targetGrid = LocalCenterPointToGrid(centerPos, draggedItem.Width, draggedItem.Height);

            bool canPlace = grid.CanPlaceItem(draggedItem, targetGrid) || CanStackAt(targetGrid);

            for (int y = 0; y < draggedItem.Height; y++)
            {
                for (int x = 0; x < draggedItem.Width; x++)
                {
                    int gx = targetGrid.x + x;
                    int gy = targetGrid.y + y;

                    if (gx < 0 || gy < 0 || gx >= gridWidth || gy >= gridHeight)
                        continue;

                    int index = gy * gridWidth + gx;
                    if (index >= 0 && index < cellUIs.Count)
                        cellUIs[index].SetHighlight(canPlace);
                }
            }
        }
        private bool CanStackAt(Vector2Int targetGrid)
        {
            if (draggedItem == null || !draggedItem.IsStackable)
                return false;

            InventoryItem targetItem = grid.GetItemAt(targetGrid);
            if (targetItem == null || targetItem == draggedItem)
                return false;

            if (!targetItem.CanStackWith(draggedItem))
                return false;

            return targetItem.quantity < targetItem.MaxStack;
        }

        private bool TryStackDraggedItemAt(Vector2Int targetGrid)
        {
            if (!CanStackAt(targetGrid))
                return false;

            InventoryItem targetItem = grid.GetItemAt(targetGrid);
            int remaining = targetItem.AddToStack(draggedItem.quantity);

            if (itemUIMap.TryGetValue(targetItem, out InventoryItemUI targetUI))
                targetUI.RefreshStackText();

            if (remaining == 0)
            {
                grid.RemoveItem(draggedItem);
                itemUIMap.Remove(draggedItem);

                if (draggedItemUI != null)
                    Destroy(draggedItemUI.gameObject);
            }
            else
            {
                draggedItem.quantity = remaining;
                draggedItem.rotated = draggedItemOriginalRotated;
                draggedItem.position = draggedItemOriginalPos;
                grid.ReinsertItem(draggedItem);
                RefreshItemUI(draggedItem);
            }

            return true;
        }

        private void ClearHighlights()
        {
            for (int i = 0; i < cellUIs.Count; i++)
            {
                cellUIs[i].SetDefault();
            }
        }

        private void EndDrag()
        {
            if (draggedItem == null || draggedItemUI == null)
            {
                CancelDragState();
                return;
            }

            if (TryDropToEquipmentSlot())
            {
                CancelDragState();
                return;
            }

            if (ScreenToLocalPoint(Input.mousePosition, out Vector2 localPoint))
            {
                Vector2 centerPos = localPoint - dragOffset;
                Vector2Int targetGrid = LocalCenterPointToGrid(centerPos, draggedItem.Width, draggedItem.Height);

                if (TryStackDraggedItemAt(targetGrid))
                {
                    CancelDragState();
                    return;
                }

                bool placed = grid.PlaceItem(draggedItem, targetGrid);

                if (placed)
                {
                    RefreshItemUI(draggedItem);
                }
                else
                {
                    draggedItem.rotated = draggedItemOriginalRotated;
                    draggedItem.position = draggedItemOriginalPos;
                    grid.ReinsertItem(draggedItem);
                    RefreshItemUI(draggedItem);
                }
            }
            else
            {
                draggedItem.rotated = draggedItemOriginalRotated;
                draggedItem.position = draggedItemOriginalPos;
                grid.ReinsertItem(draggedItem);
                RefreshItemUI(draggedItem);
            }

            CancelDragState();
        }

        private void CancelDragState()
        {
            ClearHighlights();

            if (equipmentPanel != null)
                equipmentPanel.ClearHighlights();

            draggedItem = null;
            draggedItemUI = null;
            dragOffset = Vector2.zero;
        }

        public void SelectMember(PartyMemberRuntimeData member)
        {
            if (member == null)
            {
                Debug.LogWarning("[InventoryUI] SelectMember called with null");
                return;
            }

            selectedMember = member;

            if (equipmentPanel != null)
                equipmentPanel.Bind(member);

            RefreshPartyCardSelection();

            Debug.Log($"[InventoryUI] Selected member: {member.memberId}");
        }


        private bool TryDropToEquipmentSlot()
        {
            if (draggedItem == null || draggedItemUI == null)
                return false;

            if (selectedMember == null || equipmentPanel == null)
                return false;

            EquipmentSlotUI slot = equipmentPanel.GetSlotUnderMouse(Input.mousePosition);
            if (slot == null)
                return false;

            Debug.Log($"[InventoryUI] Equipment drop detected: {draggedItem.data?.itemName} -> {slot.SlotType}");
            return false;
        }
    }
}