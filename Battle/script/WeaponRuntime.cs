using UnityEngine;

namespace TacticsGrid
{
    public class WeaponRuntime : MonoBehaviour
    {
        public WeaponDef def;
        [Tooltip("0=primary, 1=secondary")]
        public int slotIndex = 0;
        [SerializeField] private int currentAmmo;

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => def ? def.magazineSize : 0;



        private void Awake()
        {
            if (def != null && currentAmmo <= 0)
                currentAmmo = def.magazineSize;
        }

        public bool CanUseAttack(int weaponAttackIndex)
        {
            if (def == null) return false;
            if (weaponAttackIndex < 0 || weaponAttackIndex >= def.attacks.Count) return false;

            var entry = def.attacks[weaponAttackIndex];
            if (entry.attack == null) return false;

            if (!entry.consumesAmmo) return true;
            return currentAmmo >= Mathf.Max(0, entry.ammoPerUse);
        }

        // ✅ 여기서만 탄약 깎는다
        public bool TryConsumeForAttack(int weaponAttackIndex)
        {
            if (!CanUseAttack(weaponAttackIndex)) return false;

            var entry = def.attacks[weaponAttackIndex];
            if (entry.consumesAmmo)
                currentAmmo -= Mathf.Max(0, entry.ammoPerUse);

            return true;
        }

        public AttackDef GetAttackDef(int weaponAttackIndex)
        {
            if (def == null) return null;
            if (weaponAttackIndex < 0 || weaponAttackIndex >= def.attacks.Count) return null;
            return def.attacks[weaponAttackIndex].attack;
        }

        public void Reload()
        {
            int max = MaxAmmo; // def.magazineSize 기반
            if (max <= 0)
            {
                Debug.LogWarning($"[Reload] MaxAmmo is {max}. Check WeaponDef.magazineSize / def link.", this);
                return;
            }

            currentAmmo = max;
            Debug.Log($"[Reload] Ammo refilled to {currentAmmo}/{max}", this);
        }

        public void SetCurrentAmmo(int value)
        {
            currentAmmo = Mathf.Clamp(value, 0, MaxAmmo);
        }
    }
}
