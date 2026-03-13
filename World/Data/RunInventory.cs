using System;
using System.Collections.Generic;
using UnityEngine;
using TacticsGrid;

public enum ItemKind
{
    Weapon,
    Armor,
    Accessory
}

[Serializable]
public class InventoryEntry
{
    public ItemKind kind;
    public WeaponDef weapon;
    public ArmorDef armor;
    public AccessoryDef accessory;
    public int quantity = 1;

    public string DisplayName
    {
        get
        {
            switch (kind)
            {
                case ItemKind.Weapon: return weapon != null ? weapon.displayName : "Weapon";
                case ItemKind.Armor: return armor != null ? armor.displayName : "Armor";
                case ItemKind.Accessory: return accessory != null ? accessory.displayName : "Accessory";
                default: return "Item";
            }
        }
    }
}

[Serializable]
public class RunInventory
{
    [SerializeField] private List<InventoryEntry> items = new();
    public IReadOnlyList<InventoryEntry> Items => items;

    public void Clear()
    {
        items.Clear();
    }

    public bool AddWeapon(WeaponDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return AddInternal(ItemKind.Weapon, def, null, null, amount);
    }

    public bool AddArmor(ArmorDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return AddInternal(ItemKind.Armor, null, def, null, amount);
    }

    public bool AddAccessory(AccessoryDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return AddInternal(ItemKind.Accessory, null, null, def, amount);
    }

    public bool RemoveWeapon(WeaponDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return RemoveInternal(ItemKind.Weapon, def, null, null, amount);
    }

    public bool RemoveArmor(ArmorDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return RemoveInternal(ItemKind.Armor, null, def, null, amount);
    }

    public bool RemoveAccessory(AccessoryDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return RemoveInternal(ItemKind.Accessory, null, null, def, amount);
    }

    public bool HasWeapon(WeaponDef def, int amount = 1)
    {
        return HasInternal(ItemKind.Weapon, def, null, null, amount);
    }

    public bool HasArmor(ArmorDef def, int amount = 1)
    {
        return HasInternal(ItemKind.Armor, null, def, null, amount);
    }

    public bool HasAccessory(AccessoryDef def, int amount = 1)
    {
        return HasInternal(ItemKind.Accessory, null, null, def, amount);
    }

    private bool AddInternal(ItemKind kind, WeaponDef weapon, ArmorDef armor, AccessoryDef accessory, int amount)
    {
        var entry = FindEntry(kind, weapon, armor, accessory);
        if (entry == null)
        {
            entry = new InventoryEntry
            {
                kind = kind,
                weapon = weapon,
                armor = armor,
                accessory = accessory,
                quantity = amount
            };
            items.Add(entry);
            return true;
        }

        entry.quantity += amount;
        return true;
    }

    private bool RemoveInternal(ItemKind kind, WeaponDef weapon, ArmorDef armor, AccessoryDef accessory, int amount)
    {
        var entry = FindEntry(kind, weapon, armor, accessory);
        if (entry == null || entry.quantity < amount) return false;

        entry.quantity -= amount;
        if (entry.quantity <= 0)
            items.Remove(entry);

        return true;
    }

    private bool HasInternal(ItemKind kind, WeaponDef weapon, ArmorDef armor, AccessoryDef accessory, int amount)
    {
        var entry = FindEntry(kind, weapon, armor, accessory);
        return entry != null && entry.quantity >= amount;
    }

    private InventoryEntry FindEntry(ItemKind kind, WeaponDef weapon, ArmorDef armor, AccessoryDef accessory)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            if (entry.kind != kind) continue;

            switch (kind)
            {
                case ItemKind.Weapon:
                    if (entry.weapon == weapon) return entry;
                    break;
                case ItemKind.Armor:
                    if (entry.armor == armor) return entry;
                    break;
                case ItemKind.Accessory:
                    if (entry.accessory == accessory) return entry;
                    break;
            }
        }

        return null;
    }
}
