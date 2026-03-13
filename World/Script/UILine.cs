using UnityEngine;
using UnityEngine.UI;

public class UILine : MonoBehaviour
{
    [SerializeField] private Image img;
    [SerializeField] private float thickness = 6f;

    public void SetPoints(Vector2 a, Vector2 b)
    {
        if (img == null) img = GetComponent<Image>();
        var rt = img.rectTransform;

        Vector2 dir = (b - a);
        float len = dir.magnitude;
        if (len < 0.01f) len = 0.01f;

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = (a + b) * 0.5f;
        rt.sizeDelta = new Vector2(len, thickness);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetThickness(float t)
    {
        thickness = t;
        if (img != null)
        {
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, thickness);
        }
    }
}
