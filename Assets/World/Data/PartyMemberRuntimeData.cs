using UVoK.Inventory;
using TacticsGrid;

[System.Serializable]
public class PartyMemberRuntimeData
{
    public string memberId;
    public TacticsGrid.Unit unitPrefab;

    public int currentHP;
    public int maxHP;
    public int currentAP;
    public bool isDead;

    public ItemData weaponPrimaryItem;
    public int weaponPrimaryAmmo;

    public ItemData weaponSecondaryItem;
    public int weaponSecondaryAmmo;

    public ItemData armorItem;
    public ItemData accessoryItem;

    public WeaponDef WeaponPrimaryDef => weaponPrimaryItem != null ? weaponPrimaryItem.weaponDef : null;
    public WeaponDef WeaponSecondaryDef => weaponSecondaryItem != null ? weaponSecondaryItem.weaponDef : null;
    public ArmorDef ArmorDef => armorItem != null ? armorItem.armorDef : null;
    public AccessoryDef AccessoryDef => accessoryItem != null ? accessoryItem.accessoryDef : null;
}