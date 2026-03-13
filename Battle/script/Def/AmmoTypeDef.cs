using UnityEngine;

namespace TacticsGrid
{
    [CreateAssetMenu(menuName = "Tactics/Ammo Type Def")]
    public class AmmoTypeDef : ScriptableObject
    {
        [Header("ID")]
        public string ammoTypeId = "rifle";

        [Header("UI Sprites")]
        public Sprite filledPip;
        public Sprite emptyPip;
    }
}
