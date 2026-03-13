using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticsGrid.UI
{
    public class UnitCardUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private APIndicatorUI apUI;
        [SerializeField] private Button button;

        [Header("Selection Highlight (optional)")]
        [SerializeField] private GameObject selectedFrame;

        private Unit bound;
        private GridController controller;

        public Unit BoundUnit => bound;

        private void Awake()
        {
            // 버튼 슬롯 안 꽂아도 자동으로 찾아줌(루트에 Button 달았을 때)
            if (button == null) button = GetComponent<Button>();
        }

        public void Bind(GridController gc, Unit unit)
        {
            controller = gc;
            bound = unit;

            // 버튼 리스너는 항상 초기화(중복 방지)
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
                button.interactable = (unit != null);
            }

            if (unit == null)
            {
                if (nameText) nameText.text = "";
                if (portraitImage)
                {
                    portraitImage.sprite = null;
                    portraitImage.enabled = false;
                }
                apUI?.Refresh(null);
                SetSelected(false);
                return;
            }

            // ✅ 표시 이름 우선, 없으면 (Clone) 제거한 오브젝트 이름
            if (nameText)
            {
                var dn = unit.DisplayName;
                if (string.IsNullOrWhiteSpace(dn))
                    dn = unit.gameObject.name.Replace("(Clone)", "").Trim();

                nameText.text = dn;
            }

            // ✅ 유닛이 가진 포트레잇 사용
            if (portraitImage)
            {
                portraitImage.sprite = unit.PortraitSprite;
                portraitImage.enabled = (portraitImage.sprite != null);
                portraitImage.preserveAspect = true;
            }

            Refresh();
        }

        private void OnClick()
        {
            if (controller == null || bound == null) return;
            controller.SelectUnit(bound);
            // 하이라이트 갱신은 GridController에서 UnitListUI.SetSelected를 호출하거나(추천),
            // 이벤트로 연결하는 방식으로 처리하는 게 깔끔함. (아래 3번 참고)
        }

        public void Refresh()
        {
            if (bound == null)
            {
                apUI?.Refresh(null);
                SetSelected(false);
                return;
            }

            var res = bound.GetComponent<UnitResources>();
            apUI?.Refresh(res);
        }

        public void SetSelected(bool on)
        {
            if (selectedFrame) selectedFrame.SetActive(on);
        }
    }
}