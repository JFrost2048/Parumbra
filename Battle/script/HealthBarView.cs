using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TacticsGrid
{
    public class HealthBarView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Transform gridRoot;
        [SerializeField] private Image segmentPrefab;

        [Header("Style")]
        [Tooltip("현재 체력 칸 색")]
        [SerializeField] private Color filledColor = Color.white;
        [Tooltip("빈 칸 색")]
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.2f);

        private readonly List<Image> segments = new();
        private Health bound;

        private void Awake()
        {
            if (gridRoot == null) gridRoot = transform;
        }

        public void Bind(Health health)
        {
            if (bound != null)
            {
                bound.OnChanged -= OnHealthChanged;
            }

            bound = health;
            if (bound == null) return;

            EnsureSegments(bound.MaxHP);
            UpdateSegments(bound.CurrentHP, bound.MaxHP);

            bound.OnChanged += OnHealthChanged;
        }

        private void OnDestroy()
        {
            if (bound != null)
                bound.OnChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(int cur, int max)
        {
            EnsureSegments(max);
            UpdateSegments(cur, max);
        }

        private void EnsureSegments(int max)
        {
            if (segmentPrefab == null || gridRoot == null) return;

            // 필요만큼 생성
            while (segments.Count < max)
            {
                var img = Instantiate(segmentPrefab, gridRoot);
                img.gameObject.SetActive(true);
                segments.Add(img);
            }

            // 남는 건 숨김 (maxHP가 변할 가능성 대비)
            for (int i = 0; i < segments.Count; i++)
                segments[i].gameObject.SetActive(i < max);
        }

        private void UpdateSegments(int cur, int max)
        {
            for (int i = 0; i < max && i < segments.Count; i++)
            {
                segments[i].color = (i < cur) ? filledColor : emptyColor;
            }
        }
    }
}
