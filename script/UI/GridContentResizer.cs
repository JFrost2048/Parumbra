using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGrid : MonoBehaviour
{
    public Vector2 baseCellSize = new Vector2(200, 300); // 최소 셀 크기
    public float spacing = 10f;
    public int minColumns = 1; // 최소 열 수 (보통 1)

    private RectTransform rectTransform;
    private GridLayoutGroup grid;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        grid = GetComponent<GridLayoutGroup>();
    }

    void Start()
    {
        UpdateGrid();
    }

    void OnRectTransformDimensionsChange()
    {
        UpdateGrid();
    }

void UpdateGrid()
{
    if (rectTransform == null || grid == null) return;

    float parentWidth = ((RectTransform)rectTransform.parent).rect.width;
    float availableWidth = parentWidth - grid.padding.left - grid.padding.right;

    float unitWidth = baseCellSize.x + spacing;
    int columns = Mathf.Max(minColumns, Mathf.FloorToInt((availableWidth + spacing) / unitWidth));

    float totalSpacing = spacing * (columns - 1);
    float scaledWidth = (availableWidth - totalSpacing) / columns;
    float scaleFactor = scaledWidth / baseCellSize.x;
    float scaledHeight = baseCellSize.y * scaleFactor;

    // 설정 적용
    grid.cellSize = new Vector2(scaledWidth, scaledHeight);
    grid.spacing = new Vector2(spacing, spacing);

    // ✅ Content 높이 수동 계산 후 적용
    int childCount = rectTransform.childCount;
    int rows = Mathf.CeilToInt((float)childCount / columns);

    float totalHeight = grid.padding.top + grid.padding.bottom + rows * scaledHeight + (rows - 1) * spacing-1000;

    Vector2 sizeDelta = rectTransform.sizeDelta;
    sizeDelta.y = totalHeight;
    rectTransform.sizeDelta = sizeDelta;
}

}
