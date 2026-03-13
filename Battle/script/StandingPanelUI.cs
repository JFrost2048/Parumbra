using UnityEngine;
using UnityEngine.UI;

namespace TacticsGrid.UI
{
    public class StandingPanelUI : MonoBehaviour
    {
        [SerializeField] private Image standingImage;

        // 선택된 유닛의 스탠딩을 표시
        public void Show(Unit unit)
        {
            if (!standingImage) return;

            var sprite = unit ? unit.StandingSprite : null;

            standingImage.sprite = sprite;
            standingImage.enabled = (sprite != null);
            standingImage.preserveAspect = true; // 비율 유지
        }

        public void Clear()
        {
            if (!standingImage) return;
            standingImage.sprite = null;
            standingImage.enabled = false;
        }
    }
}