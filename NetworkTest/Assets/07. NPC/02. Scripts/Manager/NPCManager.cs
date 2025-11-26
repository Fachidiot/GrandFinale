using UnityEngine;
using System.Collections.Generic;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    // 현재 씬에 있는 NPC 목록
    private readonly Dictionary<string, INPC> liveNPCs = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // NPCBrain이 Awake될 때 스스로 등록함
    public void RegisterNPC(INPC npc)
    {
        if (!liveNPCs.ContainsKey(npc.Id))
        {
            liveNPCs.Add(npc.Id, npc);
        }
    }

    public void UnregisterNPC(string id)
    {
        if (liveNPCs.ContainsKey(id))
        {
            liveNPCs.Remove(id);
        }
    }

    public INPC GetNPC(string id)
    {
        liveNPCs.TryGetValue(id, out var npc);
        return npc;
    }
}