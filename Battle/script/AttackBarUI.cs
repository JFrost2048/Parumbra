using System.Collections.Generic;
using UnityEngine;

namespace TacticsGrid.UI
{
    public class AttackBarUI : MonoBehaviour
    {
        [SerializeField] private GridController controller;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private AttackButtonUI buttonPrefab;
        [SerializeField] private Sprite fallbackIcon;

        [Header("Options")]
        [Tooltip("최대 표시 버튼 수(기본 9: 숫자키 1~9 대응)")]
        [SerializeField] private int maxButtons = 9;

        // 런타임 생성 버튼 풀
        private readonly List<AttackButtonUI> pool = new();

        private void Awake()
        {
            // 인스펙터에 안 넣었어도 자동으로 잡아주기
            if (controller == null) controller = FindFirstObjectByType<GridController>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (contentRoot == null || buttonPrefab == null) return;

            var u = controller != null ? controller.Selected : null;

            // ✅ 핵심: 유닛+무기 합친 공격 리스트
            var uiAttacks = u != null ? u.GetUIAttacks() : null;

            int count = (uiAttacks != null) ? Mathf.Min(uiAttacks.Count, Mathf.Max(1, maxButtons)) : 0;

            // 1) 필요한 만큼 풀 생성
            while (pool.Count < count)
            {
                var btn = Instantiate(buttonPrefab, contentRoot);
                pool.Add(btn);
            }

            // 2) 버튼 바인딩/비활성
            for (int i = 0; i < pool.Count; i++)
            {
                bool active = i < count;
                pool[i].gameObject.SetActive(active);
                if (!active) continue;

                var item = uiAttacks[i];
                var def = item.def;

                // 선택 여부(ActiveAttack 기준)
                bool selected = (u.ActiveAttack == def);

                // ✅ 버튼 인덱스 i는 "합쳐진 리스트 기준 인덱스"
                // AttackButtonUI가 이 인덱스를 GridController에 전달하면,
                // GridController/Unit에서 SetActiveAttackByNumberKey(1~9) 또는 인덱스 기반 선택 로직으로 이어지게 하면 됨.
                pool[i].Bind(controller, i, def, selected, fallbackIcon);

                // (선택) 무기/유닛 출처에 따라 표시를 달리하고 싶으면 AttackButtonUI에 API를 추가해서 여기서 넘겨라.
                // 예: pool[i].SetSource(item.source);
            }
        }
    }
}
