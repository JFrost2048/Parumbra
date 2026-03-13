using UnityEngine;
using TMPro;

public class StatBinderText : MonoBehaviour
{
    public GameStats stats;
    public TextMeshProUGUI targetText;
    [TextArea] public string template = "구현력 : [point] / 체력 : [hp]";

    void Update()
    {
        if (!stats || !targetText) return;

        string t = template;
        t = t.Replace("[point]", stats.point.ToString());
        t = t.Replace("[hp]", $"{stats.hp}/{stats.maxHp}");
        t = t.Replace("[affection_Eileen]", stats.affection_Eileen.ToString());

        targetText.text = t;
    }
}
