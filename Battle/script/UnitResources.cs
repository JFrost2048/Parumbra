using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticsGrid
{
    /// <summary>
    /// 유닛 자원(AP, 탄약 등) 관리.
    /// 지금은 AP/탄약만. 나중에 인벤토리 붙일 때 내부 구현만 교체하면 됨.
    /// </summary>
    public class UnitResources : MonoBehaviour
    {
        [Header("Action Points")]
        [SerializeField] private int maxAP = 2;
        [SerializeField] private int currentAP = 2;

        [Serializable]
        public struct AmmoEntry
        {
            public string ammoTypeId;
            public int amount;
        }

        [Header("Ammo (temporary)")]
        [SerializeField] private List<AmmoEntry> initialAmmo = new();

        private readonly Dictionary<string, int> ammo = new();

        public int MaxAP => maxAP;
        public int CurrentAP => currentAP;

        private void Awake()
        {
            ammo.Clear();
            foreach (var e in initialAmmo)
            {
                if (string.IsNullOrWhiteSpace(e.ammoTypeId)) continue;
                ammo[e.ammoTypeId] = Mathf.Max(0, e.amount);
            }
        }

        // ===== AP =====
        public bool HasAP(int need) => currentAP >= Mathf.Max(0, need);

        public bool TrySpendAP(int cost)
        {
            cost = Mathf.Max(0, cost);
            if (cost == 0) return true;
            if (currentAP < cost) return false;
            currentAP -= cost;
            return true;
        }

        public void RefillAP()
        {
            currentAP = maxAP;
        }

        // ===== Ammo (지금은 임시) =====
        public int GetAmmo(string ammoTypeId)
        {
            if (string.IsNullOrWhiteSpace(ammoTypeId)) return 0;
            return ammo.TryGetValue(ammoTypeId, out var v) ? v : 0;
        }

        public bool HasAmmo(string ammoTypeId, int need)
        {
            need = Mathf.Max(0, need);
            if (need == 0) return true;
            return GetAmmo(ammoTypeId) >= need;
        }

        public bool TrySpendAmmo(string ammoTypeId, int cost)
        {
            cost = Mathf.Max(0, cost);
            if (cost == 0) return true;
            if (string.IsNullOrWhiteSpace(ammoTypeId)) return false;

            var cur = GetAmmo(ammoTypeId);
            if (cur < cost) return false;

            ammo[ammoTypeId] = cur - cost;
            return true;
        }

        public void SetCurrentAP(int value)
        {
            currentAP = Mathf.Clamp(value, 0, maxAP);
        }
    }
}
