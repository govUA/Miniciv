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
    public CivilizationData civilization;

    public PlayerData(int id, bool isAI = false, CivilizationData civ = null)
    {
        this.id = id;
        this.isAI = isAI;
        this.civilization = civ;
        if (civ != null)
        {
            if (ColorUtility.TryParseHtmlString(civ.primaryColorHex, out Color pColor))
                primaryColor = pColor;
            else
                primaryColor = Color.blue;

            if (ColorUtility.TryParseHtmlString(civ.secondaryColorHex, out Color sColor))
                secondaryColor = sColor;
            else
                secondaryColor = Color.white;
        }
        else
        {
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
}

public class PlayerManager : MonoBehaviour
{
    public CivilizationManager civilizationManager;
    private Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();

    public void InitializePlayers(int count)
    {
        var allCivs = civilizationManager.availableCivilizations;

        List<CivilizationData> usedCivs = new List<CivilizationData>();

        for (int i = 0; i < count; i++)
        {
            CivilizationData assignedCiv = null;

            if (i == 0)
            {
                int index = GameSettings.PlayerCivilizationIndex;
                if (index >= 0 && index < allCivs.Count)
                {
                    assignedCiv = allCivs[index];
                    usedCivs.Add(assignedCiv);
                }
            }

            if (assignedCiv == null)
            {
                assignedCiv = civilizationManager.AssignRandomCivilization(usedCivs);

                if (assignedCiv != null)
                {
                    usedCivs.Add(assignedCiv);
                }
            }

            players[i] = new PlayerData(i, i != 0, assignedCiv);

            string civName = assignedCiv != null ? assignedCiv.civName : "Unknown";
            string leader = assignedCiv != null ? assignedCiv.leaderName : "Unknown";
            Debug.Log($"[PLAYER] Player {i} initialized as {civName} (Leader: {leader})");
        }

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                DeclareWar(i, j);
            }
        }

        Debug.Log("[DIPLOMACY] TOTAL WAR MODE ENABLED: All players at war with each other!");
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