using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HorizontalSnap : MonoBehaviour, IEndDragHandler
{
    public ScrollRect scrollRect;
    public RectTransform content;
    public int totalPages = 3;        // 총 페이지 수 (아이콘/화면 수)
    private float[] pagePositions;    // 각 페이지의 정규화된 위치

    void Start()
    {
        // 페이지 위치 계산 (0~1 사이)
        pagePositions = new float[totalPages];
        for (int i = 0; i < totalPages; i++)
            pagePositions[i] = (float)i / (totalPages - 1);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 현재 위치와 가장 가까운 페이지 찾기
        float pos = scrollRect.horizontalNormalizedPosition;
        float closest = float.MaxValue;
        int targetIndex = 0;

        for (int i = 0; i < pagePositions.Length; i++)
        {
            float dist = Mathf.Abs(pagePositions[i] - pos);
            if (dist < closest)
            {
                closest = dist;
                targetIndex = i;
            }
        }

        // 부드럽게 스냅 이동
        StopAllCoroutines();
        StartCoroutine(SmoothMove(pagePositions[targetIndex]));
    }

    System.Collections.IEnumerator SmoothMove(float target)
    {
        while (Mathf.Abs(scrollRect.horizontalNormalizedPosition - target) > 0.001f)
        {
            scrollRect.horizontalNormalizedPosition =
                Mathf.Lerp(scrollRect.horizontalNormalizedPosition, target, Time.deltaTime * 10f);
            yield return null;
        }
        scrollRect.horizontalNormalizedPosition = target;
    }
}
