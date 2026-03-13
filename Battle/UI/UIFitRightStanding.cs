using UnityEngine;

[ExecuteAlways]
public class UIFillSideOfMap : MonoBehaviour
{
    public enum Side
    {
        Left,
        Right
    }

    [Header("Side")]
    [SerializeField] private Side side = Side.Right;

    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform mapRoot;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Canvas canvas;

    [Header("Padding (px)")]
    [SerializeField] private float paddingFromMap = 12f;   // 맵과 패널 사이
    [SerializeField] private float paddingSide = 12f;      // 화면 좌/우 여백
    [SerializeField] private float paddingTop = 12f;
    [SerializeField] private float paddingBottom = 12f;

    [Header("Behavior")]
    [SerializeField] private bool hideIfNoSpace = true;
    [SerializeField] private float minWidthToShow = 10f;

    private Bounds mapBounds;
    private bool dirty = true;
    private int lastW, lastH;
    private Vector3 lastCamPos;
    private float lastOrtho;

    private Rect lastPixelRect;
    private float lastAspect;
    private float lastCanvasScale;

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
        if (Screen.width != lastW || Screen.height != lastH) dirty = true;

        if (cam != null)
        {
            if (cam.transform.position != lastCamPos) dirty = true;
            if (cam.orthographic && !Mathf.Approximately(cam.orthographicSize, lastOrtho)) dirty = true;

            // ✅ 추가: 해상도 안 바뀌어도 화면 매핑이 바뀌는 케이스 잡기
            if (cam.pixelRect != lastPixelRect) dirty = true;
            if (!Mathf.Approximately(cam.aspect, lastAspect)) dirty = true;
        }

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

        GetScreenRect(mapBounds, out float mapMinX, out float mapMaxX, out _, out _);

        float leftPx, rightPx;

        if (side == Side.Right)
        {
            leftPx = Mathf.Clamp(mapMaxX + paddingFromMap, 0f, Screen.width);
            rightPx = Mathf.Clamp(Screen.width - paddingSide, 0f, Screen.width);
        }
        else // Left
        {
            leftPx = Mathf.Clamp(paddingSide, 0f, Screen.width);
            rightPx = Mathf.Clamp(mapMinX - paddingFromMap, 0f, Screen.width);
        }

        float widthPx = rightPx - leftPx;

        float topPx = Mathf.Clamp(Screen.height - paddingTop, 0f, Screen.height);
        float bottomPx = Mathf.Clamp(paddingBottom, 0f, Screen.height);
        float heightPx = topPx - bottomPx;

        if (hideIfNoSpace && widthPx < minWidthToShow)
        {
            if (panel.gameObject.activeSelf)
                panel.gameObject.SetActive(false);
            return;
        }

        if (!panel.gameObject.activeSelf)
            panel.gameObject.SetActive(true);

        float scale = canvas.isRootCanvas ? canvas.scaleFactor : 1f;

        if (side == Side.Right)
        {
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 0.5f);
            panel.pivot = new Vector2(1f, 0.5f);
            panel.anchoredPosition = new Vector2(-(paddingSide / scale), 0f);
        }
        else // Left
        {
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = new Vector2(paddingSide / scale, 0f);
        }

        panel.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            widthPx / scale
        );

        panel.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            heightPx / scale
        );
    }

    private void GetScreenRect(
        Bounds b,
        out float minX,
        out float maxX,
        out float minY,
        out float maxY
    )
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
        maxY = float.NegativeInfinity;

        foreach (var corner in corners)
        {
            var sp = cam.WorldToScreenPoint(corner);
            if (sp.z < 0f) continue;

            minX = Mathf.Min(minX, sp.x);
            maxX = Mathf.Max(maxX, sp.x);
            minY = Mathf.Min(minY, sp.y);
            maxY = Mathf.Max(maxY, sp.y);
        }

        if (float.IsInfinity(minX))
            minX = maxX = minY = maxY = 0f;
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
