using UnityEngine;
using UnityEngine.EventSystems;

public class MapPanZoom : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    [Header("Refs")]
    [SerializeField] private RectTransform viewport;   // MapViewport
    [SerializeField] private RectTransform content;    // MapContent

    [Header("Pan")]
    [SerializeField] private bool clampToViewport = true;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 0.12f;  // 휠 민감도
    [SerializeField] private float minScale = 0.6f;
    [SerializeField] private float maxScale = 2.2f;

    private Vector2 lastPointerLocal;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (viewport == null || content == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, eventData.position, eventData.pressEventCamera, out lastPointerLocal);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (viewport == null || content == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, eventData.position, eventData.pressEventCamera, out var curLocal))
            return;

        Vector2 delta = curLocal - lastPointerLocal;
        lastPointerLocal = curLocal;

        content.anchoredPosition += delta;

        if (clampToViewport) ClampContent();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (viewport == null || content == null) return;

        // 1) 휠 입력 -> 목표 스케일
        float scroll = eventData.scrollDelta.y;  // 위로 굴리면 +, 아래로 -
        if (Mathf.Abs(scroll) < 0.01f) return;

        float current = content.localScale.x;
        float target = current * (1f + scroll * zoomSpeed);
        target = Mathf.Clamp(target, minScale, maxScale);

        if (Mathf.Approximately(target, current)) return;

        // 2) 커서가 가리키는 viewport 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, eventData.position, eventData.pressEventCamera, out var vpLocal);

        // 3) 줌 전: 커서 지점이 content 기준 어디에 해당하는지 계산
        // viewport 로컬에서 content anchoredPosition을 빼면 content 로컬 좌표가 됨 (스케일 고려)
        Vector2 before = (vpLocal - content.anchoredPosition) / current;

        // 4) 스케일 적용
        content.localScale = new Vector3(target, target, 1f);

        // 5) 줌 후: 같은 content 로컬 지점이 커서 아래에 오도록 anchoredPosition 보정
        content.anchoredPosition = vpLocal - before * target;

        if (clampToViewport) ClampContent();
    }

    private void ClampContent()
    {
        // viewport rect / scaled content rect 기반 clamp
        var vp = viewport.rect;
        var ct = content.rect;

        float s = content.localScale.x;
        float cw = ct.width * s;
        float ch = ct.height * s;

        // content pivot이 0.5,0.5 라는 가정
        float minX = (vp.width - cw) * 0.5f;
        float maxX = (cw - vp.width) * 0.5f;
        float minY = (vp.height - ch) * 0.5f;
        float maxY = (ch - vp.height) * 0.5f;

        // content가 viewport보다 작으면 중앙 고정
        if (cw <= vp.width) { minX = maxX = 0f; }
        if (ch <= vp.height) { minY = maxY = 0f; }

        var p = content.anchoredPosition;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        content.anchoredPosition = p;
    }
}
