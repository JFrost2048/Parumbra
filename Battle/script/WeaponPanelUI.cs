using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TacticsGrid.UI
{
    public class WeaponPanelUI : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private Image weaponImage;

        [Header("Ammo Pips")]
        [SerializeField] private Transform ammoRoot;
        [SerializeField] private AmmoPipUI pipPrefab;

        [SerializeField] private GridLayoutGroup ammoGrid;
        [SerializeField] private int horizontalThreshold = 6;
        [SerializeField] private int columnsWhenWrap = 6;

        [Header("DB")]
        [SerializeField] private TacticsGrid.AmmoTypeDatabase ammoDB;



        private readonly List<AmmoPipUI> spawned = new();

        // ✅ 핵심: Unit 타입을 명시적으로 TacticsGrid.Unit로!
        public void Refresh(TacticsGrid.Unit u)
        {
            ClearPips();



            if (u == null)
            {
                if (weaponImage) weaponImage.enabled = false;
                return;
            }

            var weapon = u.GetActiveWeaponRuntime();
            if (weapon == null || weapon.def == null)
            {
                if (weaponImage) weaponImage.enabled = false;
                return;
            }

            // 무기 이미지
            if (weaponImage)
            {
                weaponImage.enabled = true;
                weaponImage.sprite = weapon.def.uiIcon;
                weaponImage.preserveAspect = true;
            }

            // ✅ 방어: 필수 참조 누락 체크
            if (ammoRoot == null || pipPrefab == null)
            {
                Debug.LogError("[WeaponPanelUI] ammoRoot 또는 pipPrefab이 Inspector에서 연결되지 않았음");
                return;
            }

            // 탄약 타입 스프라이트 가져오기
            if (ammoDB == null || !ammoDB.TryGet(weapon.def.ammoTypeId, out var ammoType)
 || ammoType == null)
            {
                Debug.LogWarning($"[WeaponPanelUI] AmmoTypeDef 없음: {weapon.def.ammoTypeId}");
                return;
            }

            if (ammoType.filledPip == null || ammoType.emptyPip == null)
            {
                Debug.LogWarning($"[WeaponPanelUI] AmmoTypeDef 스프라이트 누락: {ammoType.name}");
                return;
            }

            int max = weapon.MaxAmmo;
            int cur = weapon.CurrentAmmo;

            // ✅ 레이아웃 규칙
            if (ammoGrid != null)
            {
                if (max <= horizontalThreshold)
                {
                    ammoGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    ammoGrid.constraintCount = 1; // 한 줄로
                }
                else
                {
                    ammoGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    ammoGrid.constraintCount = columnsWhenWrap; // 6칸 넘어가면 줄바꿈
                }
            }

            for (int i = 0; i < max; i++)
            {
                var pip = Instantiate(pipPrefab, ammoRoot);
                bool filled = i < cur;
                bool rotate = max <= horizontalThreshold; // 예: 6 이하면 눕힘
                pip.Set(filled ? ammoType.filledPip : ammoType.emptyPip, filled, rotate);
                spawned.Add(pip);
            }
        }

        private void ClearPips()
        {
            for (int i = 0; i < spawned.Count; i++)
                if (spawned[i]) Destroy(spawned[i].gameObject);
            spawned.Clear();
        }
    }
}
