using System;
using System.Collections.Generic;
using UnityEngine;
using TacticsGrid;
using UVoK.Inventory;

public enum ItemKind
{
    Weapon,
    Armor,
    Accessory,
    Consumable,
    Material
}

[Serializable]
public class InventoryStack
{
    public ItemKind kind;
    public WeaponDef weapon;
    public ArmorDef armor;
    public AccessoryDef accessory;
    public ConsumableDef consumable;
    public MaterialDef material;
    public int quantity = 0;

    public bool IsEmpty => GetItemRef() == null || quantity <= 0;

    public string DisplayName
    {
        get
        {
            switch (kind)
            {
                case ItemKind.Weapon: return weapon != null ? weapon.displayName : "Weapon";
                case ItemKind.Armor: return armor != null ? armor.displayName : "Armor";
                case ItemKind.Accessory: return accessory != null ? accessory.displayName : "Accessory";
                case ItemKind.Consumable: return consumable != null ? consumable.displayName : "Consumable";
                case ItemKind.Material: return material != null ? material.displayName : "Material";
                default: return "Item";
            }
        }
    }

    public bool IsStackable
    {
        get
        {
            switch (kind)
            {
                case ItemKind.Weapon: return weapon != null && weapon.stackable;
                case ItemKind.Armor: return armor != null && armor.stackable;
                case ItemKind.Accessory: return accessory != null && accessory.stackable;
                case ItemKind.Consumable: return consumable != null && consumable.stackable;
                case ItemKind.Material: return material != null && material.stackable;
                default: return false;
            }
        }
    }

    public int MaxStack
    {
        get
        {
            switch (kind)
            {
                case ItemKind.Weapon: return weapon != null ? Mathf.Max(1, weapon.maxStack) : 1;
                case ItemKind.Armor: return armor != null ? Mathf.Max(1, armor.maxStack) : 1;
                case ItemKind.Accessory: return accessory != null ? Mathf.Max(1, accessory.maxStack) : 1;
                case ItemKind.Consumable: return consumable != null ? Mathf.Max(1, consumable.maxStack) : 1;
                case ItemKind.Material: return material != null ? Mathf.Max(1, material.maxStack) : 1;
                default: return 1;
            }
        }
    }

    public Vector2Int GridSize
    {
        get
        {
            Vector2Int size;
            switch (kind)
            {
                case ItemKind.Weapon: size = weapon != null ? weapon.gridSize : Vector2Int.one; break;
                case ItemKind.Armor: size = armor != null ? armor.gridSize : Vector2Int.one; break;
                case ItemKind.Accessory: size = accessory != null ? accessory.gridSize : Vector2Int.one; break;
                case ItemKind.Consumable: size = consumable != null ? consumable.gridSize : Vector2Int.one; break;
                case ItemKind.Material: size = material != null ? material.gridSize : Vector2Int.one; break;
                default: size = Vector2Int.one; break;
            }
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        }
    }

    public UnityEngine.Object GetItemRef()
    {
        switch (kind)
        {
            case ItemKind.Weapon: return weapon;
            case ItemKind.Armor: return armor;
            case ItemKind.Accessory: return accessory;
            case ItemKind.Consumable: return consumable;
            case ItemKind.Material: return material;
            default: return null;
        }
    }

    public Sprite GetIcon()
    {
        switch (kind)
        {
            case ItemKind.Weapon: return weapon != null ? weapon.uiIcon : null;
            case ItemKind.Armor: return armor != null ? armor.uiIcon : null;
            case ItemKind.Accessory: return accessory != null ? accessory.uiIcon : null;
            case ItemKind.Consumable: return consumable != null ? consumable.uiIcon : null;
            case ItemKind.Material: return material != null ? material.uiIcon : null;
            default: return null;
        }
    }

    public bool CanStackWith(InventoryStack other)
    {
        if (other == null) return false;
        if (IsEmpty || other.IsEmpty) return false;
        if (kind != other.kind) return false;
        return GetItemRef() == other.GetItemRef() && IsStackable;
    }

    public InventoryStack Clone(int amount)
    {
        return new InventoryStack
        {
            kind = kind,
            weapon = weapon,
            armor = armor,
            accessory = accessory,
            consumable = consumable,
            material = material,
            quantity = amount
        };
    }
}

[Serializable]
public class InventoryItem
{
    public InventoryStack stack = new InventoryStack();
    public Vector2Int position;
    public bool isRotated;

    public Vector2Int Size
    {
        get
        {
            var size = stack != null ? stack.GridSize : Vector2Int.one;
            if (isRotated)
                return new Vector2Int(size.y, size.x);
            return size;
        }
    }
}

[Serializable]
public class RunInventory
{
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private List<InventoryItem> items = new();
    [SerializeField] private List<int> occupancy = new();

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public int SlotCount => gridWidth * gridHeight;
    public IReadOnlyList<InventoryItem> Items => items;

    public void Clear()
    {
        items.Clear();
        EnsureOccupancy();
    }

    public void EnsureSlots(int count)
    {
        int width = Mathf.Max(1, gridWidth);
        int height = Mathf.Max(1, Mathf.CeilToInt((float)count / width));
        EnsureGrid(width, height);
    }

    public void EnsureGrid(int width, int height)
    {
        gridWidth = Mathf.Max(1, width);
        gridHeight = Mathf.Max(1, height);
        EnsureOccupancy();
        RebuildOccupancy();
    }

    public InventoryStack GetSlot(int index)
    {
        if (!TryGetItemAtCell(index, out var item, out var isRoot)) return null;
        return isRoot ? item.stack : null;
    }

    public int GetTotalCount(ItemKind kind, UnityEngine.Object item)
    {
        int total = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var s = items[i].stack;
            if (s == null || s.IsEmpty || s.kind != kind) continue;
            if (s.GetItemRef() == item)
                total += s.quantity;
        }
        return total;
    }

    public bool AddWeapon(WeaponDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryAdd(ItemKind.Weapon, def, null, null, null, null, amount);
    }

    public bool AddArmor(ArmorDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryAdd(ItemKind.Armor, null, def, null, null, null, amount);
    }

    public bool AddAccessory(AccessoryDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryAdd(ItemKind.Accessory, null, null, def, null, null, amount);
    }

    public bool AddConsumable(ConsumableDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryAdd(ItemKind.Consumable, null, null, null, def, null, amount);
    }

    public bool AddMaterial(MaterialDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryAdd(ItemKind.Material, null, null, null, null, def, amount);
    }

    public bool RemoveWeapon(WeaponDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryRemove(ItemKind.Weapon, def, null, null, null, null, amount);
    }

    public bool RemoveArmor(ArmorDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryRemove(ItemKind.Armor, null, def, null, null, null, amount);
    }

    public bool RemoveAccessory(AccessoryDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryRemove(ItemKind.Accessory, null, null, def, null, null, amount);
    }

    public bool RemoveConsumable(ConsumableDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryRemove(ItemKind.Consumable, null, null, null, def, null, amount);
    }

    public bool RemoveMaterial(MaterialDef def, int amount = 1)
    {
        if (def == null || amount <= 0) return false;
        return TryRemove(ItemKind.Material, null, null, null, null, def, amount);
    }

    public bool HasWeapon(WeaponDef def, int amount = 1)
    {
        return GetTotalCount(ItemKind.Weapon, def) >= amount;
    }

    public bool HasArmor(ArmorDef def, int amount = 1)
    {
        return GetTotalCount(ItemKind.Armor, def) >= amount;
    }

    public bool HasAccessory(AccessoryDef def, int amount = 1)
    {
        return GetTotalCount(ItemKind.Accessory, def) >= amount;
    }

    public bool HasConsumable(ConsumableDef def, int amount = 1)
    {
        return GetTotalCount(ItemKind.Consumable, def) >= amount;
    }

    public bool HasMaterial(MaterialDef def, int amount = 1)
    {
        return GetTotalCount(ItemKind.Material, def) >= amount;
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (item.weaponDef != null)
            return AddWeapon(item.weaponDef, amount);

        if (item.armorDef != null)
            return AddArmor(item.armorDef, amount);

        if (item.accessoryDef != null)
            return AddAccessory(item.accessoryDef, amount);

        return false;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (item.weaponDef != null)
            return RemoveWeapon(item.weaponDef, amount);

        if (item.armorDef != null)
            return RemoveArmor(item.armorDef, amount);

        if (item.accessoryDef != null)
            return RemoveAccessory(item.accessoryDef, amount);

        return false;
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (item.weaponDef != null)
            return HasWeapon(item.weaponDef, amount);

        if (item.armorDef != null)
            return HasArmor(item.armorDef, amount);

        if (item.accessoryDef != null)
            return HasAccessory(item.accessoryDef, amount);

        return false;
    }

    public bool TryMoveSlot(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return false;
        if (!TryGetItemAtCell(fromIndex, out var item, out var isRoot)) return false;
        var target = IndexToPos(toIndex);
        return TryMoveItem(item, target);
    }

    public bool CanPlaceAtIndex(int targetIndex, InventoryItem item)
    {
        if (item == null) return false;
        var target = IndexToPos(targetIndex);
        int itemIndex = items.IndexOf(item);
        return CanPlaceAt(item, target, itemIndex, -1);
    }

    public bool TryTakeOne(int slotIndex, out InventoryStack single)
    {
        single = null;
        if (!TryGetItemAtCell(slotIndex, out var item, out var isRoot)) return false;
        var stack = item.stack;
        if (stack == null || stack.IsEmpty) return false;

        single = stack.Clone(1);
        stack.quantity -= 1;
        if (stack.quantity <= 0)
        {
            items.Remove(item);
        }
        RebuildOccupancy();
        return true;
    }

    public bool TryPlaceOne(int slotIndex, InventoryStack stack, out InventoryStack remainder)
    {
        remainder = null;
        if (stack == null || stack.IsEmpty) return false;
        var pos = IndexToPos(slotIndex);

        if (TryGetItemAtCell(slotIndex, out var existing, out var isRoot))
        {
            if (existing.stack != null && stack.CanStackWith(existing.stack))
            {
                int space = existing.stack.MaxStack - existing.stack.quantity;
                if (space <= 0)
                {
                    remainder = stack;
                    return false;
                }
                int moved = Mathf.Min(space, stack.quantity);
                existing.stack.quantity += moved;
                if (stack.quantity > moved)
                    remainder = stack.Clone(stack.quantity - moved);
                RebuildOccupancy();
                return true;
            }
            remainder = stack;
            return false;
        }

        var newItem = new InventoryItem
        {
            stack = stack.Clone(1),
            position = pos
        };

        if (!CanPlaceAt(newItem, pos, -1, -1))
        {
            remainder = stack;
            return false;
        }

        items.Add(newItem);
        RebuildOccupancy();
        return true;
    }

    public bool TryAdd(ItemKind kind, WeaponDef weapon, ArmorDef armor, AccessoryDef accessory, ConsumableDef consumable, MaterialDef material, int amount)
    {
        if (amount <= 0) return false;
        EnsureOccupancy();

        var stack = new InventoryStack
        {
            kind = kind,
            weapon = weapon,
            armor = armor,
            accessory = accessory,
            consumable = consumable,
            material = material,
            quantity = amount
        };

        if (stack.IsEmpty) return false;

        if (stack.IsStackable)
        {
            for (int i = 0; i < items.Count && stack.quantity > 0; i++)
            {
                var s = items[i].stack;
                if (s == null || s.IsEmpty || !s.CanStackWith(stack)) continue;
                int space = s.MaxStack - s.quantity;
                if (space <= 0) continue;
                int moved = Mathf.Min(space, stack.quantity);
                s.quantity += moved;
                stack.quantity -= moved;
            }
        }

        while (stack.quantity > 0)
        {
            int moved = stack.IsStackable ? Mathf.Min(stack.MaxStack, stack.quantity) : 1;
            if (!TryFindSpace(stack.GridSize, out var pos))
                break;

            items.Add(new InventoryItem
            {
                stack = stack.Clone(moved),
                position = pos
            });
            stack.quantity -= moved;
        }

        RebuildOccupancy();
        return stack.quantity == 0;
    }

    public bool TryRemove(ItemKind kind, WeaponDef weapon, ArmorDef armor, AccessoryDef accessory, ConsumableDef consumable, MaterialDef material, int amount)
    {
        if (amount <= 0) return false;
        int remaining = amount;
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var s = items[i].stack;
            if (s == null || s.IsEmpty || s.kind != kind) continue;
            if (kind == ItemKind.Weapon && s.weapon != weapon) continue;
            if (kind == ItemKind.Armor && s.armor != armor) continue;
            if (kind == ItemKind.Accessory && s.accessory != accessory) continue;
            if (kind == ItemKind.Consumable && s.consumable != consumable) continue;
            if (kind == ItemKind.Material && s.material != material) continue;

            int take = Mathf.Min(remaining, s.quantity);
            s.quantity -= take;
            remaining -= take;
            if (s.quantity <= 0)
                items.RemoveAt(i);
        }
        RebuildOccupancy();
        return remaining == 0;
    }

    public bool TryGetItemAtCell(int cellIndex, out InventoryItem item, out bool isRoot)
    {
        item = null;
        isRoot = false;
        if (cellIndex < 0 || cellIndex >= SlotCount) return false;
        if (occupancy == null || occupancy.Count != SlotCount)
            RebuildOccupancy();
        int itemIndex = occupancy[cellIndex];
        if (itemIndex < 0 || itemIndex >= items.Count) return false;
        item = items[itemIndex];
        var pos = IndexToPos(cellIndex);
        isRoot = item != null && item.position == pos;
        return item != null;
    }

    private bool TryMoveItem(InventoryItem item, Vector2Int target)
    {
        if (item == null) return false;
        var current = item.position;
        if (current == target) return false;

        int itemIndex = items.IndexOf(item);
        if (itemIndex < 0) return false;

        if (CanPlaceAt(item, target, itemIndex, -1))
        {
            item.position = target;
            RebuildOccupancy();
            return true;
        }

        int targetIndex = GetItemIndexAtCell(target);
        if (targetIndex < 0 || targetIndex >= items.Count || targetIndex == itemIndex)
            return false;

        var other = items[targetIndex];
        if (other == null) return false;

        if (item.stack != null && other.stack != null && item.stack.CanStackWith(other.stack))
        {
            int space = other.stack.MaxStack - other.stack.quantity;
            if (space <= 0) return false;
            int moved = Mathf.Min(space, item.stack.quantity);
            other.stack.quantity += moved;
            item.stack.quantity -= moved;
            if (item.stack.quantity <= 0)
                items.RemoveAt(itemIndex);
            RebuildOccupancy();
            return true;
        }

        if (CanPlaceAt(item, other.position, itemIndex, targetIndex) &&
            CanPlaceAt(other, current, targetIndex, itemIndex))
        {
            item.position = other.position;
            other.position = current;
            RebuildOccupancy();
            return true;
        }

        return false;
    }

    private bool TryFindSpace(Vector2Int size, out Vector2Int pos)
    {
        pos = Vector2Int.zero;
        size = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        for (int y = 0; y <= gridHeight - size.y; y++)
        {
            for (int x = 0; x <= gridWidth - size.x; x++)
            {
                var test = new Vector2Int(x, y);
                if (IsAreaEmpty(test, size))
                {
                    pos = test;
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsAreaEmpty(Vector2Int pos, Vector2Int size)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x + size.x > gridWidth || pos.y + size.y > gridHeight)
            return false;
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                int idx = ToIndex(pos.x + x, pos.y + y);
                if (occupancy[idx] >= 0)
                    return false;
            }
        }
        return true;
    }

    private bool CanPlaceAt(InventoryItem item, Vector2Int pos, int ignoreIndexA, int ignoreIndexB)
    {
        if (item == null) return false;
        var size = item.Size;
        if (pos.x < 0 || pos.y < 0 || pos.x + size.x > gridWidth || pos.y + size.y > gridHeight)
            return false;

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                int idx = ToIndex(pos.x + x, pos.y + y);
                int occ = occupancy[idx];
                if (occ < 0) continue;
                if (occ == ignoreIndexA || occ == ignoreIndexB) continue;
                return false;
            }
        }
        return true;
    }

    private int GetItemIndexAtCell(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= gridWidth || pos.y >= gridHeight) return -1;
        return occupancy[ToIndex(pos.x, pos.y)];
    }

    private Vector2Int IndexToPos(int index)
    {
        if (gridWidth <= 0) return Vector2Int.zero;
        int x = index % gridWidth;
        int y = index / gridWidth;
        return new Vector2Int(x, y);
    }

    private int ToIndex(int x, int y)
    {
        return y * gridWidth + x;
    }

    private void EnsureOccupancy()
    {
        int count = SlotCount;
        if (occupancy == null)
            occupancy = new List<int>(count);
        if (occupancy.Count != count)
        {
            occupancy.Clear();
            for (int i = 0; i < count; i++)
                occupancy.Add(-1);
        }
    }

    private void RebuildOccupancy()
    {
        EnsureOccupancy();
        for (int i = 0; i < occupancy.Count; i++)
            occupancy[i] = -1;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            if (item == null || item.stack == null || item.stack.IsEmpty)
            {
                items.RemoveAt(i);
                continue;
            }

            var size = item.Size;
            if (item.position.x < 0 || item.position.y < 0 ||
                item.position.x + size.x > gridWidth ||
                item.position.y + size.y > gridHeight)
            {
                if (!TryFindSpace(size, out var newPos))
                {
                    items.RemoveAt(i);
                    continue;
                }
                item.position = newPos;
            }

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    int idx = ToIndex(item.position.x + x, item.position.y + y);
                    occupancy[idx] = i;
                }
            }
        }
    }
}
