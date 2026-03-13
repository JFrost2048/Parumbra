using UnityEngine;

[System.Serializable]
public class PartyMemberRuntimeData
{
    public string memberId;
    public TacticsGrid.Unit unitPrefab;

    public int currentHP;
    public int maxHP;
    public int currentAP;

    public bool isDead;

    [Header("Equipment")]
    public TacticsGrid.WeaponDef weaponPrimary;
    public int weaponPrimaryAmmo;
    public TacticsGrid.WeaponDef weaponSecondary;
    public int weaponSecondaryAmmo;
    public ArmorDef armor;
    public AccessoryDef accessory;
}
