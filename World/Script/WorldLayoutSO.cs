using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/World Layout")]
public class WorldLayoutSO : ScriptableObject
{
    public List<LayoutNode> nodes = new();
    public List<LayoutLink> links = new();

    [Header("Start")]
    public string startId;

    [Header("Floor Settings")]
    public float floorVerticalSpacing = 4f;

    [Header("Grid Spacing")]
    public Vector2 nodeSpacing = new Vector2(3f, 2f);

    [Header("Global Offset")]
    public Vector2 globalOffset;

    [System.Serializable]
    public class LayoutNode
    {
        public string id;
        public Vector2 position;
        public int floor = 0;

        [Header("Optional Fixed Room")]
        public RoomDef fixedRoom;

        [Header("Reveal")]
        public bool revealedAtStart = false;
    }

    [System.Serializable]
    public class LayoutLink
    {
        public string a;
        public string b;
        public bool bidirectional = true;
    }
}