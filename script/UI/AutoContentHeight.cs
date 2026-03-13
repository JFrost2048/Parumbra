using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class AutoContentHeight : MonoBehaviour
{
    public TMP_Text logText;             // Content 자식인 Log Text (TMP)
    public float verticalPadding = 24f;  // 위/아래 여백 합

    RectTransform content;

    void Awake() { content = GetComponent<RectTransform>(); }

    void LateUpdate()
    {
        if (!logText || !content) return;

        // Content의 현재 가용 폭을 기준으로 텍스트 선호 높이 계산
        float w = content.rect.width > 1 ? content.rect.width : ((RectTransform)content.parent).rect.width;
        Vector2 pref = logText.GetPreferredValues(logText.text, w, 0f);
        float targetH = Mathf.Max(pref.y + verticalPadding, 10f);

        var sd = content.sizeDelta;
        if (Mathf.Abs(sd.y - targetH) > 0.5f)
        {
            sd.y = targetH;
            content.sizeDelta = sd;
        }
    }
}
