using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticsGrid
{
    public enum WeaponType { Pistol, Rifle, Shotgun, Melee, Heavy }





    [Serializable]
    public class WeaponAttackEntry
    {
        public AttackDef attack;
        public WeaponType weaponType;

        [Min(0)] public int ammoPerUse = 1;
        public bool consumesAmmo = true;

        [Header("UI")]
        [Min(0)] public int uiSlot = 0; // ✅ 추가: 무기 공격도 슬롯 고정


    }

    [CreateAssetMenu(menuName = "Tactics/Weapon Def")]
    public class WeaponDef : ScriptableObject
    {
        public string displayName;
        public Sprite uiIcon;

        public string ammoTypeId = "rifle";
        public int magazineSize = 6;

        public List<WeaponAttackEntry> attacks = new();

        public WeaponType weaponType = WeaponType.Pistol;
    }
}
