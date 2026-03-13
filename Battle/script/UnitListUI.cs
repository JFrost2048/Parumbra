using System.Collections.Generic;
using UnityEngine;

namespace TacticsGrid.UI
{
    public class UnitListUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private UnitCardUI cardPrefab;

        private readonly List<UnitCardUI> cards = new();
        private GridController controller;

        // ✅ 같은 입력으로 Build가 반복 호출되면 스킵하기 위한 캐시
        private int lastHash;

        public void Build(GridController gc, List<Unit> units)
        {
            controller = gc;

            if (contentRoot == null || cardPrefab == null)
            {
                Debug.LogWarning("[UnitListUI] contentRoot 또는 cardPrefab이 비어있음");
                return;
            }

            // ✅ 입력 해시로 중복 Build 방지 (같은 유닛 구성인데 계속 Build하면 중복처럼 보임)
            int hash = ComputeUnitsHash(units);
            if (hash == lastHash && cards.Count > 0)
            {
                // 그래도 AP 등 갱신은 필요할 수 있으니 Refresh만
                RefreshAll();
                return;
            }
            lastHash = hash;

            // ✅ cards 리스트 기반 삭제는 누락이 생길 수 있음 -> contentRoot 자식 전부 제거가 안전
            ClearAllChildren(contentRoot);
            cards.Clear();

            if (units == null) return;

            // ✅ 데이터 중복도 체크 (원인 3번 잡기)
            var seen = new HashSet<Unit>();

            foreach (var u in units)
            {
                if (u == null) continue;

                if (!seen.Add(u))
                {
                    Debug.LogWarning($"[UnitListUI] units 리스트에 동일 유닛 중복 포함: {u.name}");
                    continue;
                }

                var card = Instantiate(cardPrefab, contentRoot);
                card.Bind(controller, u);
                cards.Add(card);
            }

            RefreshAll();
        }

        public void RefreshAll()
        {
            foreach (var c in cards)
                c?.Refresh();
        }

        public void SetSelected(Unit selected)
        {
            foreach (var c in cards)
                c?.SetSelected(c.BoundUnit == selected);
        }

        private static void ClearAllChildren(Transform root)
        {
            // ✅ 즉시 파괴: Build가 연속 호출돼도 중복 “보이는” 현상 제거
            // (Destroy는 프레임 끝이라 잠깐 겹쳐 보임)
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                Object.Destroy(child.gameObject);
            }
        }

        private static int ComputeUnitsHash(List<Unit> units)
        {
            unchecked
            {
                int h = 17;
                if (units == null) return h;

                h = h * 31 + units.Count;
                for (int i = 0; i < units.Count; i++)
                {
                    var u = units[i];
                    h = h * 31 + (u ? u.GetInstanceID() : 0);
                }
                return h;
            }
        }
    }
}