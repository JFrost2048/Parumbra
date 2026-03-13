using System.Collections.Generic;
using UnityEngine;

public class RoomNodeRuntime
{
    public string id;
    public Vector2 position;
    public int floor;
    public List<string> neighbors = new();

    public RoomDef def;

    public bool revealedAtStart;
    public bool kindRevealed;

    // 런 진행 상태
    public bool visited;
    public bool cleared;
    public bool battleInProgress;

    public bool IsDiscovered => kindRevealed;

    public void OnEnter()
    {
        kindRevealed = true;
    }
}