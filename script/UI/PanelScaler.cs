using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PanelScaler : MonoBehaviour
{
    [Header("Target")]
    public RectTransform target;                 // 키울 Panel (미지정 시 자기 자신)
    public bool useLayoutElement = false;        // LayoutGroup 안이면 체크
    public LayoutElement layoutElement;          // useLayoutElement가 true면 연결

    [Header("Sizes")]
    public Vector2 collapsedSize = new Vector2(600, 90);   // 접힘 크기
    public Vector2 expandedSize  = new Vector2(1100, 120); // 펼침 크기

    [Header("Animation")]
    [Range(0.05f, 2f)] public float duration = 0.25f;
    [Tooltip("0=직선, 1=부드럽게, 2=Back(살짝 튕김)")]
    [Range(0, 2)] public int easing = 2;

    Coroutine running;
    bool isExpanded = false;

    void Reset()
    {
        target = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();
    }

    public void Toggle()
    {
        if (isExpanded) Collapse();
        else Expand();
    }

    public void Expand()
    {
        isExpanded = true;
        Play(collapsedSize, expandedSize);
    }

    public void Collapse()
    {
        isExpanded = false;
        Play(expandedSize, collapsedSize);
    }

    void Play(Vector2 from, Vector2 to)
    {
        if (target == null) target = GetComponent<RectTransform>();
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(AnimateSize(from, to, duration));
    }

    IEnumerator AnimateSize(Vector2 from, Vector2 to, float t)
    {
        float time = 0f;

        // 시작값 세팅(중간에 끊고 다시 눌러도 자연스럽게)
        Vector2 start = useLayoutElement ? GetCurrentLayoutSize() : target.sizeDelta;
        if ((start - to).sqrMagnitude > (start - from).sqrMagnitude) start = from;

        while (time < t)
        {
            time += Time.unscaledDeltaTime;  // UI는 보통 unscaled 추천
            float u = Mathf.Clamp01(time / t);
            u = Ease(u, easing);
            Vector2 v = Vector2.LerpUnclamped(start, to, u);

            if (useLayoutElement && layoutElement != null)
            {
                layoutElement.preferredWidth = v.x;
                layoutElement.preferredHeight = v.y;
            }
            else
            {
                target.sizeDelta = v;
            }

            yield return null;
        }

        // 최종 스냅
        if (useLayoutElement && layoutElement != null)
        {
            layoutElement.preferredWidth = to.x;
            layoutElement.preferredHeight = to.y;
        }
        else
        {
            target.sizeDelta = to;
        }
        running = null;
    }

    Vector2 GetCurrentLayoutSize()
    {
        if (layoutElement == null) return target.sizeDelta;
        float w = layoutElement.preferredWidth  > 0 ? layoutElement.preferredWidth  : target.sizeDelta.x;
        float h = layoutElement.preferredHeight > 0 ? layoutElement.preferredHeight : target.sizeDelta.y;
        return new Vector2(w, h);
    }

    // 0: Linear, 1: SmoothStep, 2: EaseOutBack(살짝 튕김)
    static float Ease(float x, int mode)
    {
        switch (mode)
        {
            case 1: return x * x * (3f - 2f * x);
            case 2:
                float s = 1.70158f; // overshoot
                x -= 1f;
                return (x * x * ((s + 1f) * x + s) + 1f);
            default: return x;
        }
    }
}
