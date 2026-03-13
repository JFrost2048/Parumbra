using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TacticsGrid.UI
{
    public class AttackButtonUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private GameObject selectedFrame;

        private int uiIndex;                 // "합쳐진 공격 리스트" 기준 인덱스
        private GridController controller;
        private AttackDef boundDef;          // ✅ 바인딩된 공격(리로드 판별용)

        /// <summary>
        /// AttackBarUI에서 호출
        /// </summary>
        public void Bind(
            GridController controller,
            int index0,
            AttackDef def,
            bool selected,
            Sprite fallbackIcon
        )
        {
            this.controller = controller;
            this.uiIndex = index0;
            this.boundDef = def;

            // 숫자키 표시 (1~9)
            if (numberText != null)
                numberText.text = (index0 + 1).ToString();

            // 아이콘
            var icon = (def != null && def.icon != null) ? def.icon : fallbackIcon;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = (icon != null);
            }

            // 선택 프레임
            if (selectedFrame != null)
                selectedFrame.SetActive(selected);
        }

        /// <summary>
        /// Button.onClick 에 연결
        /// </summary>
        public void OnClick()
        {
            if (controller == null) return;

            // 안전: 선택 유닛/공격 목록 확인
            var u = controller.Selected;
            if (u == null) return;

            var list = u.GetUIAttacks();
            if (list == null || uiIndex < 0 || uiIndex >= list.Count) return;

            // 바인딩된 def와 실제 리스트 def가 다를 수 있으니 실제 것을 우선
            var item = list[uiIndex];
            var atk = item.def;
            if (atk == null) return;

            // ✅ 리로드는 "즉시 실행" (타겟팅 모드로 안 들어감)
            if (atk.isReload)
            {
                // 우선 활성 공격으로 세팅(선택 프레임 등 일관성)
                controller.SelectAttackByIndex(uiIndex);

                // 바로 실행
                controller.DoReload();
                return;
            }

            // ✅ 일반 공격은 기존대로 선택 → 공격모드 진입
            controller.SelectAttackByIndex(uiIndex);
        }
    }
}