using UnityEngine;
using UnityEngine.UI;

namespace TacticsGrid.UI
{
    public class AmmoPipUI : MonoBehaviour
    {
        [SerializeField] private Image image;

        public void Set(Sprite sprite, bool filled, bool rotate90 = false)
        {
            image.sprite = sprite;
            image.enabled = true;

            // 빈 탄 반투명
            image.color = filled ? Color.white : new Color(1f, 1f, 1f, 0.25f);

            // ✅ 옆으로 눕히기
            var rt = (RectTransform)image.transform;
            rt.localRotation = rotate90 ? Quaternion.Euler(0, 0, -90f) : Quaternion.identity;
        }
    }
}
