using System.Collections;
using UnityEngine;

namespace TacticsGrid
{
    public class WorldHealthBar : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Health targetHealth;

        [Header("Placement")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private bool billboardToCamera = true;

        [Header("Visibility")]
        [Tooltip("이 유닛은 체력바를 표시할지 여부(프리팹마다 다르게)")]
        [SerializeField] private bool showBar = true;

        [Tooltip("중립(Faction.Neutral)은 자동 숨김(유닛에 Faction이 있을 때만)")]
        [SerializeField] private bool hideIfNeutral = true;

        [Tooltip("피해를 받기 전에는 숨김")]
        [SerializeField] private bool hideUntilDamaged = false;

        [Tooltip("맞으면 n초 동안 보였다가 다시 숨김(0이면 유지)")]
        [SerializeField] private float showOnHitSeconds = 0f;

        [Header("View")]
        [SerializeField] private HealthBarView view;
        [SerializeField] private Canvas canvas;

        private Transform cam;
        private int lastHP;
        private Coroutine autoHideCo;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);
            if (view == null) view = GetComponentInChildren<HealthBarView>(true);

            if (targetHealth == null)
                targetHealth = GetComponentInParent<Health>();

            cam = Camera.main != null ? Camera.main.transform : null;

            if (targetHealth != null)
            {
                lastHP = targetHealth.CurrentHP;
                view.Bind(targetHealth);
                targetHealth.OnChanged += HandleChanged;
                targetHealth.OnDied += _ => Hide();
            }

            ApplyInitialVisibility();
        }

        private void LateUpdate()
        {
            // 머리 위 위치 고정
            var root = targetHealth != null ? targetHealth.transform : transform;
            transform.position = root.position + worldOffset;

            // 카메라 바라보게(빌보드)
            if (billboardToCamera && cam != null)
            {
                transform.forward = cam.forward;
            }
        }

        private void ApplyInitialVisibility()
        {
            if (!showBar)
            {
                Hide();
                return;
            }

            if (hideIfNeutral)
            {
                // Unit에 faction이 있을 경우만 체크 (없으면 무시)
                var unit = GetComponentInParent<Unit>();
                if (unit != null && unit.faction == Faction.Neutral)
                {
                    Hide();
                    return;
                }
            }

            if (hideUntilDamaged)
                Hide();
            else
                Show();
        }

        private void HandleChanged(int cur, int max)
        {
            // 피해 감지(치유는 무시하고 싶으면 조건 수정)
            bool tookDamage = cur < lastHP;
            lastHP = cur;

            if (!showBar) return;

            // neutral 숨김 조건이면 계속 숨김
            if (hideIfNeutral)
            {
                var unit = GetComponentInParent<Unit>();
                if (unit != null && unit.faction == Faction.Neutral) return;
            }

            if (hideUntilDamaged && tookDamage)
                Show();

            if (showOnHitSeconds > 0f && tookDamage)
            {
                Show();
                if (autoHideCo != null) StopCoroutine(autoHideCo);
                autoHideCo = StartCoroutine(AutoHideAfter(showOnHitSeconds));
            }
        }

        private IEnumerator AutoHideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (hideUntilDamaged || showOnHitSeconds > 0f)
                Hide();
        }

        public void SetShowBar(bool value)
        {
            showBar = value;
            ApplyInitialVisibility();
        }

        private void Show()
        {
            if (canvas != null) canvas.enabled = true;
        }

        private void Hide()
        {
            if (canvas != null) canvas.enabled = false;
        }
    }
}
