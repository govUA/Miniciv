using UnityEngine;
using System.Collections.Generic;

public class PlayerData
{
    public int id;
    public int globalScience = 0;

    public string currentResearch = "";
    public int accumulatedResearch = 0;
    public List<string> unlockedTechs = new List<string>();

    public HashSet<int> atWarWith = new HashSet<int>();

    public PlayerData(int id)
    {
        this.id = id;
    }
}

public class PlayerManager : MonoBehaviour
{
    private Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();

    public Dictionary<string, int> techCosts = new Dictionary<string, int>()
    {
        { "Pottery", 30 },
        { "Bronze Working", 50 }
    };

    public void InitializePlayers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            players[i] = new PlayerData(i);
        }
    }

    public PlayerData GetPlayer(int id)
    {
        if (players.ContainsKey(id)) return players[id];
        return null;
    }

    public void DeclareWar(int playerA, int playerB)
    {
        PlayerData pA = GetPlayer(playerA);
        PlayerData pB = GetPlayer(playerB);
        if (pA != null && pB != null)
        {
            pA.atWarWith.Add(playerB);
            pB.atWarWith.Add(playerA);
            Debug.Log($"[DIPLOMACY] Player {playerA} and Player {playerB} are now AT WAR!");
        }
    }

    public void MakePeace(int playerA, int playerB)
    {
        PlayerData pA = GetPlayer(playerA);
        PlayerData pB = GetPlayer(playerB);
        if (pA != null && pB != null)
        {
            pA.atWarWith.Remove(playerB);
            pB.atWarWith.Remove(playerA);
            Debug.Log($"[DIPLOMACY] Player {playerA} and Player {playerB} made PEACE.");
        }
    }

    public bool IsAtWar(int playerA, int playerB)
    {
        PlayerData pA = GetPlayer(playerA);
        if (pA != null) return pA.atWarWith.Contains(playerB);
        return false;
    }

    public void AddScience(int playerId, int amount)
    {
        PlayerData p = GetPlayer(playerId);
        if (p == null) return;

        p.globalScience += amount;

        if (!string.IsNullOrEmpty(p.currentResearch))
        {
            p.accumulatedResearch += amount;
            int cost = techCosts[p.currentResearch];

            if (p.accumulatedResearch >= cost)
            {
                p.unlockedTechs.Add(p.currentResearch);
                Debug.Log($"[TECH] Player {playerId} successfully researched {p.currentResearch}!");

                p.accumulatedResearch -= cost;
                p.currentResearch = "";
            }
        }
    }

    public void SetResearch(int playerId, string techName)
    {
        PlayerData p = GetPlayer(playerId);
        if (p == null) return;

        if (p.unlockedTechs.Contains(techName))
        {
            Debug.Log($"Tech {techName} is already researched.");
            return;
        }

        if (!techCosts.ContainsKey(techName)) return;

        p.currentResearch = techName;
        Debug.Log($"[TECH] Player {playerId} started researching {techName}. Cost: {techCosts[techName]} Sci.");
    }
}