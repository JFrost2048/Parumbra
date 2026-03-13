using TMPro;
using UnityEngine;

namespace TacticsGrid.UI
{
    /// <summary>
    /// 초상화 옆에 AP를 "도형 + 숫자"로 표시.
    /// </summary>
    public class APIndicatorUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text pipText;     // 예: ◆◆◇
        [SerializeField] private TMP_Text numberText;  // 예: 2

        [Header("Style")]
        [SerializeField] private string filled = "◆";
        [SerializeField] private string empty = "◇";

        public void Refresh(UnitResources res)
        {
            if (res == null)
            {
                if (pipText) pipText.text = "";
                if (numberText) numberText.text = "";
                return;
            }

            int cur = Mathf.Max(0, res.CurrentAP);
            int max = Mathf.Max(0, res.MaxAP);

            if (pipText)
            {
                // max가 너무 커지면 UI 터질 수 있으니 안전장치(원하면 수정)
                int cappedMax = Mathf.Min(max, 12);
                int cappedCur = Mathf.Min(cur, cappedMax);

                System.Text.StringBuilder sb = new System.Text.StringBuilder(cappedMax);
                for (int i = 0; i < cappedCur; i++) sb.Append(filled);
                for (int i = cappedCur; i < cappedMax; i++) sb.Append(empty);

                pipText.text = sb.ToString();
            }

            if (numberText)
                numberText.text = cur.ToString();
        }
    }
}
