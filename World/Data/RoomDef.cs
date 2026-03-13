using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/Room Def")]
public class RoomDef : ScriptableObject
{
    [Header("Identity")]
    public string roomId;          // 유니크 키(중복 방 방지용) ex) "medbay_basic"
    public string displayName;     // UI 표기용 ex) "의무실"

    [Header("Type")]
    public RoomType type;

    [Header("Interactions (placeholder)")]
    public List<InteractionSlot> interactionSlots = new();

    [Header("Optional (later)")]
    public Sprite roomIcon;        // 나중에
    // public LootTable lootTable;  // 나중에
}