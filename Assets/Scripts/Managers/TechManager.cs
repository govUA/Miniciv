using System.Collections.Generic;
using UnityEngine;

public enum TechType
{
    Agriculture,
    Pottery,
    AnimalHusbandry,
    Sailing,
    Mining,
    Archery,
    BronzeWorking,
    Masonry,
    IronWorking
}

public class TechManager : MonoBehaviour
{
    public class TechData
    {
        public TechType Tech;
        public string Name;
        public int Cost;
        public List<TechType> Prerequisites;
    }

    private class PlayerResearchState
    {
        public TechType? CurrentResearch = null;
        public int AccumulatedResearch = 0;
        public HashSet<TechType> UnlockedTechs = new HashSet<TechType>();
    }

    private Dictionary<TechType, TechData> techTree;
    private Dictionary<int, PlayerResearchState> playerStates;

    void Awake()
    {
        playerStates = new Dictionary<int, PlayerResearchState>();
        InitializeTechTree();
    }

    private void InitializeTechTree()
    {
        techTree = new Dictionary<TechType, TechData>
        {
            {
                TechType.Agriculture,
                new TechData
                {
                    Tech = TechType.Agriculture, Name = "Agriculture", Cost = 20, Prerequisites = new List<TechType>()
                }
            },
            {
                TechType.Pottery,
                new TechData
                {
                    Tech = TechType.Pottery, Name = "Pottery", Cost = 40,
                    Prerequisites = new List<TechType> { TechType.Agriculture }
                }
            },
            {
                TechType.Sailing,
                new TechData
                {
                    Tech = TechType.Sailing, Name = "Sailing", Cost = 65,
                    Prerequisites = new List<TechType> { TechType.Pottery }
                }
            },
            {
                TechType.Mining,
                new TechData
                {
                    Tech = TechType.Mining, Name = "Mining", Cost = 40,
                    Prerequisites = new List<TechType> { TechType.Agriculture }
                }
            }
        };
    }

    private PlayerResearchState GetState(int playerId)
    {
        if (!playerStates.ContainsKey(playerId))
            playerStates[playerId] = new PlayerResearchState();
        return playerStates[playerId];
    }

    public bool HasTech(int playerId, TechType tech)
    {
        return GetState(playerId).UnlockedTechs.Contains(tech);
    }

    public bool CanResearch(int playerId, TechType tech)
    {
        if (HasTech(playerId, tech)) return false;
        if (!techTree.TryGetValue(tech, out TechData data)) return false;

        foreach (var prereq in data.Prerequisites)
        {
            if (!HasTech(playerId, prereq)) return false;
        }

        return true;
    }

    public void SetResearch(int playerId, TechType tech)
    {
        if (!CanResearch(playerId, tech)) return;
        GetState(playerId).CurrentResearch = tech;
    }

    public void AddScience(int playerId, int amount)
    {
        var state = GetState(playerId);
        if (state.CurrentResearch == null) return;

        state.AccumulatedResearch += amount;
        TechData data = techTree[state.CurrentResearch.Value];

        if (state.AccumulatedResearch >= data.Cost)
        {
            UnlockTech(playerId, state.CurrentResearch.Value);
            state.AccumulatedResearch -= data.Cost;
            state.CurrentResearch = null;
        }
    }

    public void UnlockTech(int playerId, TechType tech)
    {
        GetState(playerId).UnlockedTechs.Add(tech);
    }
}