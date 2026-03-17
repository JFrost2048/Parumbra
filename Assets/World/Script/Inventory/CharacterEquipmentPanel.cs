using UnityEngine;
using TacticsGrid;

namespace UVoK.Inventory
{
    public class CharacterEquipmentPanel : MonoBehaviour
    {
        [SerializeField] private EquipmentSlotUI weaponPrimarySlot;
        [SerializeField] private EquipmentSlotUI weaponSecondarySlot;
        [SerializeField] private EquipmentSlotUI armorSlot;
        [SerializeField] private EquipmentSlotUI accessorySlot;
        [SerializeField] private Canvas canvas;

        private PartyMemberRuntimeData currentMember;

        public PartyMemberRuntimeData CurrentMember => currentMember;

        public void Bind(PartyMemberRuntimeData member)
        {
            currentMember = member;

            if (member == null)
            {
                weaponPrimarySlot?.SetEmpty("Primary Weapon");
                weaponSecondarySlot?.SetEmpty("Secondary Weapon");
                armorSlot?.SetEmpty("Armor");
                accessorySlot?.SetEmpty("Accessory");
                return;
            }

            if (weaponPrimarySlot != null)
            {
                if (member.weaponPrimaryItem != null)
                    weaponPrimarySlot.SetFilled(member.weaponPrimaryItem.itemName, member.weaponPrimaryItem.icon);
                else
                    weaponPrimarySlot.SetEmpty("Primary Weapon");
            }

            if (weaponSecondarySlot != null)
            {
                int maxSlots = member.unitPrefab != null ? member.unitPrefab.MaxWeaponSlots : 1;

                if (maxSlots < 2)
                {
                    weaponSecondarySlot.SetEmpty("Locked");
                }
                else if (member.weaponSecondaryItem != null)
                {
                    weaponSecondarySlot.SetFilled(member.weaponSecondaryItem.itemName, member.weaponSecondaryItem.icon);
                }
                else
                {
                    weaponSecondarySlot.SetEmpty("Secondary Weapon");
                }
            }

            if (armorSlot != null)
            {
                if (member.armorItem != null)
                    armorSlot.SetFilled(member.armorItem.itemName, member.armorItem.icon);
                else
                    armorSlot.SetEmpty("Armor");
            }

            if (accessorySlot != null)
            {
                if (member.accessoryItem != null)
                    accessorySlot.SetFilled(member.accessoryItem.itemName, member.accessoryItem.icon);
                else
                    accessorySlot.SetEmpty("Accessory");
            }
        }

        public EquipmentSlotUI GetSlotUnderMouse(Vector2 screenPoint)
        {
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            if (weaponPrimarySlot != null && weaponPrimarySlot.ContainsScreenPoint(screenPoint, cam))
                return weaponPrimarySlot;

            if (weaponSecondarySlot != null && weaponSecondarySlot.ContainsScreenPoint(screenPoint, cam))
                return weaponSecondarySlot;

            if (armorSlot != null && armorSlot.ContainsScreenPoint(screenPoint, cam))
                return armorSlot;

            if (accessorySlot != null && accessorySlot.ContainsScreenPoint(screenPoint, cam))
                return accessorySlot;

            return null;
        }

        public void ClearHighlights()
        {
            weaponPrimarySlot?.SetHighlight(false);
            weaponSecondarySlot?.SetHighlight(false);
            armorSlot?.SetHighlight(false);
            accessorySlot?.SetHighlight(false);
        }

        public void HighlightValidSlotFor(ItemData item)
        {
            ClearHighlights();
            if (item == null) return;

            if (item.weaponDef != null)
            {
                weaponPrimarySlot?.SetHighlight(true);
                weaponSecondarySlot?.SetHighlight(true);
            }
            else if (item.armorDef != null)
            {
                armorSlot?.SetHighlight(true);
            }
            else if (item.accessoryDef != null)
            {
                accessorySlot?.SetHighlight(true);
            }
        }
    }
}