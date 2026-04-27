using UnityEngine;
using System.Collections.Generic;

public class PlayerData
{
    public int id;
    public bool isAI;
    public HashSet<int> atWarWith = new HashSet<int>();
    public Color primaryColor = Color.white;
    public Color secondaryColor = Color.black;
    public int gold = 10;

    public PlayerData(int id, bool isAI = false)
    {
        this.id = id;
        this.isAI = isAI;
        if (id == 0)
        {
            primaryColor = Color.blue;
            secondaryColor = Color.white;
        }
        else
        {
            primaryColor = Color.red;
            secondaryColor = Color.yellow;
        }
    }
}

public class PlayerManager : MonoBehaviour
{
    private Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();

    public void InitializePlayers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            bool isAI = (i > 0);
            players[i] = new PlayerData(i, isAI);
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
        }
    }

    public bool IsAtWar(int playerA, int playerB)
    {
        PlayerData pA = GetPlayer(playerA);
        if (pA != null) return pA.atWarWith.Contains(playerB);
        return false;
    }
}