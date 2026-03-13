using System.Collections.Generic;
using UnityEngine;

namespace TacticsGrid
{
    public class AmmoTypeDatabase : MonoBehaviour
    {
        [SerializeField] private List<AmmoTypeDef> ammoTypes = new();

        private readonly Dictionary<string, AmmoTypeDef> map = new();

        private void Awake()
        {
            map.Clear();
            foreach (var a in ammoTypes)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.ammoTypeId)) continue;
                map[a.ammoTypeId] = a;
            }
        }

        public bool TryGet(string ammoTypeId, out AmmoTypeDef def)
        {
            def = null;
            if (string.IsNullOrWhiteSpace(ammoTypeId)) return false;
            return map.TryGetValue(ammoTypeId, out def) && def != null;
        }
    }
}
