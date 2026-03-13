using UnityEngine;

[ExecuteAlways]
public class UIFitBelowMap : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform mapRoot;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Canvas canvas;

    [Header("Padding (px)")]
    [SerializeField] private float paddingHorizontal = 12f; // 좌우
    [SerializeField] private float paddingTop = 8f;          // 맵 아래와 UI 사이
    [SerializeField] private float paddingBottom = 0f;       // 화면 바닥 여백(원하면)

    [Header("Behavior")]
    [SerializeField] private bool hideIfNoSpace = true;
    [SerializeField] private float minHeightToShow = 10f;

    private Bounds mapBounds;

    private int lastW, lastH;
    private Vector3 lastCamPos;
    private float lastOrtho;
    private Rect lastPixelRect;
    private float lastAspect;
    private float lastCanvasScale;
    private bool dirty = true;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (panel == null) panel = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        CaptureViewportState();
    }

    private void Start()
    {
        RebuildBounds();
        ApplyLayout();
        dirty = false;
    }

    private void LateUpdate()
    {
        // 1) Screen 변경
        if (Screen.width != lastW || Screen.height != lastH)
            dirty = true;

        // 2) Camera 변경(이동/줌 + pixelRect/aspect)
        if (cam != null)
        {
            if (cam.transform.position != lastCamPos) dirty = true;
            if (cam.orthographic && !Mathf.Approximately(cam.orthographicSize, lastOrtho)) dirty = true;

            if (cam.pixelRect != lastPixelRect) dirty = true;
            if (!Mathf.Approximately(cam.aspect, lastAspect)) dirty = true;
        }

        // 3) Canvas scaleFactor 변경(Canvas Scaler 영향)
        if (canvas != null)
        {
            float curScale = canvas.isRootCanvas ? canvas.scaleFactor : 1f;
            if (!Mathf.Approximately(curScale, lastCanvasScale)) dirty = true;
        }

        if (!dirty) return;

        RebuildBounds();
        ApplyLayout();
        CaptureViewportState();
        dirty = false;
    }

    /// 외부(맵 리빌드/줌 변경 등)에서 호출
    public void MarkDirty() => dirty = true;

    private void CaptureViewportState()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        if (cam != null)
        {
            lastCamPos = cam.transform.position;
            lastOrtho = cam.orthographic ? cam.orthographicSize : 0f;
            lastPixelRect = cam.pixelRect;
            lastAspect = cam.aspect;
        }

        if (canvas != null)
            lastCanvasScale = canvas.isRootCanvas ? canvas.scaleFactor : 1f;
    }

    private void RebuildBounds()
    {
        if (mapRoot == null) return;
        mapBounds = CalculateBounds(mapRoot);
    }

    private void ApplyLayout()
    {
        if (cam == null || panel == null || canvas == null) return;
        if (mapRoot == null) return;

        // 맵의 스크린 rect 구하기
        GetScreenRect(mapBounds, out float minX, out float maxX, out float minY);

        // 패딩 적용
        minX += paddingHorizontal;
        maxX -= paddingHorizontal;
        minY -= paddingTop;

        // 화면 밖으로 나가면 clamp
        minX = Mathf.Clamp(minX, 0f, Screen.width);
        maxX = Mathf.Clamp(maxX, 0f, Screen.width);
        minY = Mathf.Clamp(minY, 0f, Screen.height);

        // 아래바는 "화면 바닥~맵 아래"까지 채우는 높이
        float desiredWidthPx = Mathf.Max(0f, maxX - minX);
        float desiredHeightPx = Mathf.Max(0f, minY - paddingBottom);

        if (hideIfNoSpace && desiredHeightPx < minHeightToShow)
        {
            if (panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
            return;
        }
        if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);

        // Canvas scaler 대응
        float scale = canvas.isRootCanvas ? canvas.scaleFactor : 1f;
        float desiredWidth = desiredWidthPx / scale;
        float desiredHeight = desiredHeightPx / scale;

        // ✅ 아래바는 하단 중앙 고정이 가장 안전
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);

        // 맵의 화면 중앙 x에 맞춰서, 아래(y=paddingBottom)로 붙임
        float centerXPx = (minX + maxX) * 0.5f;

        var canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 screenPoint = new Vector2(centerXPx, paddingBottom);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out var localPoint
        );

        panel.anchoredPosition = new Vector2(localPoint.x, paddingBottom / scale);

        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredWidth);
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredHeight);
    }

    private void GetScreenRect(Bounds b, out float minX, out float maxX, out float minY)
    {
        Vector3 c = b.center;
        Vector3 e = b.extents;

        Vector3[] corners =
        {
            c + new Vector3(-e.x, -e.y, -e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z),
            c + new Vector3(-e.x,  e.y,  e.z),
            c + new Vector3( e.x,  e.y, -e.z),
            c + new Vector3( e.x,  e.y,  e.z),
        };

        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        minY = float.PositiveInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            var sp = cam.WorldToScreenPoint(corners[i]);
            if (sp.z < 0f) continue;

            minX = Mathf.Min(minX, sp.x);
            maxX = Mathf.Max(maxX, sp.x);
            minY = Mathf.Min(minY, sp.y);
        }

        if (float.IsInfinity(minX))
        {
            minX = 0f;
            maxX = 0f;
            minY = 0f;
        }
    }

    private Bounds CalculateBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.zero);

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return b;
    }
}
