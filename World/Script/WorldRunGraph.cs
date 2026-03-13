using System.Collections.Generic;
using UnityEngine;

public class WorldRunGraph
{
    public Dictionary<string, RoomNodeRuntime> nodes = new();

    public string currentRoomId;
    public string startId;
    public string exitId;

    public bool TryGetNode(string id, out RoomNodeRuntime node)
    {
        return nodes.TryGetValue(id, out node);
    }

    public void Initialize(WorldLayoutSO layout, RoomPoolSO mandatoryPool, RoomPoolSO optionalPool)
    {
        nodes.Clear();

        foreach (var ln in layout.nodes)
        {
            var node = new RoomNodeRuntime
            {
                id = ln.id,
                position = ln.position,
                floor = ln.floor,
                neighbors = new List<string>(),
                revealedAtStart = ln.revealedAtStart
            };

            nodes.Add(node.id, node);
        }

        // links -> neighbors 변환
        foreach (var link in layout.links)
        {
            if (string.IsNullOrEmpty(link.a) || string.IsNullOrEmpty(link.b))
                continue;

            if (nodes.TryGetValue(link.a, out var aNode) && !aNode.neighbors.Contains(link.b))
                aNode.neighbors.Add(link.b);

            if (link.bidirectional)
            {
                if (nodes.TryGetValue(link.b, out var bNode) && !bNode.neighbors.Contains(link.a))
                    bNode.neighbors.Add(link.a);
            }
        }

        startId = layout.startId;
        currentRoomId = startId;

        AssignRoomsFromLayoutAndPools(layout, mandatoryPool, optionalPool);

        foreach (var node in nodes.Values)
        {
            if (node.revealedAtStart)
                node.kindRevealed = true;
        }

        if (nodes.TryGetValue(startId, out var startNode))
        {
            startNode.kindRevealed = true;
            startNode.revealedAtStart = true;
        }
    }
    private void AssignRoomsFromLayoutAndPools(WorldLayoutSO layout, RoomPoolSO mandatoryPool, RoomPoolSO optionalPool)
    {
        var usedRoomIds = new HashSet<string>();
        var emptySlots = new List<RoomNodeRuntime>();

        foreach (var ln in layout.nodes)
        {
            if (!nodes.TryGetValue(ln.id, out var node))
                continue;

            if (ln.fixedRoom != null)
            {
                node.def = ln.fixedRoom;

                if (!string.IsNullOrEmpty(ln.fixedRoom.roomId))
                    usedRoomIds.Add(ln.fixedRoom.roomId);
            }
            else
            {
                emptySlots.Add(node);
            }
        }

        if (nodes.TryGetValue(startId, out var startNode) && startNode.def == null)
        {
            RoomDef startDef = FindFirstByType(mandatoryPool, RoomType.Start)
                               ?? FindFirstByType(optionalPool, RoomType.Start);

            if (startDef != null)
            {
                startNode.def = startDef;
                usedRoomIds.Add(startDef.roomId);
                emptySlots.Remove(startNode);
            }
            else
            {
                Debug.LogWarning("[WorldGen] No Start RoomDef found for startId node.");
            }
        }

        if (nodes.TryGetValue(startId, out var sNode))
            emptySlots.Remove(sNode);

        Shuffle(emptySlots);

        int idx = 0;
        if (mandatoryPool != null)
        {
            var mandatory = new List<RoomDef>(mandatoryPool.rooms);
            Shuffle(mandatory);

            foreach (var def in mandatory)
            {
                if (idx >= emptySlots.Count) break;
                if (def == null || string.IsNullOrEmpty(def.roomId)) continue;
                if (def.type == RoomType.Start) continue;
                if (usedRoomIds.Contains(def.roomId)) continue;

                emptySlots[idx].def = def;
                usedRoomIds.Add(def.roomId);
                idx++;
            }
        }

        if (optionalPool != null)
        {
            var optional = new List<RoomDef>(optionalPool.rooms);
            Shuffle(optional);

            int optCursor = 0;
            for (; idx < emptySlots.Count; idx++)
            {
                RoomDef pick = null;

                while (optCursor < optional.Count)
                {
                    var cand = optional[optCursor++];
                    if (cand == null || string.IsNullOrEmpty(cand.roomId)) continue;
                    if (cand.type == RoomType.Start) continue;
                    if (usedRoomIds.Contains(cand.roomId)) continue;

                    pick = cand;
                    break;
                }

                if (pick == null)
                {
                    Debug.LogWarning("[WorldGen] Optional pool exhausted. Some nodes remain empty.");
                    break;
                }

                emptySlots[idx].def = pick;
                usedRoomIds.Add(pick.roomId);
            }
        }
    }

    private RoomDef FindFirstByType(RoomPoolSO pool, RoomType type)
    {
        if (pool == null) return null;
        foreach (var def in pool.rooms)
            if (def != null && def.type == type) return def;
        return null;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}