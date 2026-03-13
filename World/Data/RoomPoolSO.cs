using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/Room Pool")]
public class RoomPoolSO : ScriptableObject
{
    public List<RoomDef> rooms = new();
}