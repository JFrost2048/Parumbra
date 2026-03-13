using UnityEngine;

namespace TacticsGrid
{
    [CreateAssetMenu(menuName = "Tactics/Attack Def")]
    public class AttackDef : ScriptableObject
    {
        public string displayName = "Attack";
        public AttackPattern pattern = AttackPattern.Manhattan;

        public bool targetPoint = false;   // true면 "지점 선택" 공격
        public int aoeRadius = 0;          // 지점 중심 폭발 반경(0이면 단일)

        [Header("Unlock")]
        public bool requiresSkill = false;

        // 예: "pistol.quickshot", "rifle.aim_training"
        public string requiredSkillId = ""; //비어있으면 조건 없음

        public enum WeaponType { Pistol, Rifle, Shotgun, Melee, Heavy }

        public WeaponType weaponType = WeaponType.Pistol;

        [Min(1)] public int range = 1;
        public int damage = 2;

        public Color overlayColor = new Color(1f, 0.2f, 0.2f, 0.35f);

        [Header("Cost (Resource)")]
        [Min(0)] public int apCost = 1;

        [Header("Weapon Usage")]
        public bool usesEquippedWeaponAmmo = true;
        [Min(0)] public int ammoPerUse = 1;

        [Header("Special")]
        public bool isReload = false;

        [Header("UI")]
        [Min(0)] public int uiSlot = 0;   // ✅ 추가: 0-based, 하단 UI 고정 슬롯
        public Sprite icon;
        public Sprite iconAlt;
    }
}
