using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class TechDataModel
{
    public string name;
    public int cost;
    public bool isRepeatable;
    public List<string> prerequisites;
}

[Serializable]
public class TechDatabase
{
    public List<TechDataModel> technologies;
}

public class TechManager : MonoBehaviour
{
    [Tooltip("Link Technologies.json file")]
    public TextAsset techJsonFile;

    public class TechData
    {
        public string Name;
        public int Cost;
        public bool IsRepeatable;
        public List<string> Prerequisites;
    }

    private class PlayerResearchState
    {
        public string CurrentResearch = null;
        public int AccumulatedResearch = 0;
        public HashSet<string> UnlockedTechs = new HashSet<string>();
        public Dictionary<string, int> TechCompletions = new Dictionary<string, int>();
    }

    private Dictionary<string, TechData> techTree;
    private Dictionary<int, PlayerResearchState> playerStates;

    public event Action<int, string, int> OnRepeatableTechCompleted;

    void Awake()
    {
        playerStates = new Dictionary<int, PlayerResearchState>();
        InitializeTechTree();
    }

    private void InitializeTechTree()
    {
        techTree = new Dictionary<string, TechData>();

        if (techJsonFile == null)
        {
            Debug.LogError("[TechManager] JSON file with these technologies was not found!");
            return;
        }

        TechDatabase db = JsonUtility.FromJson<TechDatabase>(techJsonFile.text);
        if (db == null || db.technologies == null) return;

        foreach (var t in db.technologies)
        {
            TechData data = new TechData
            {
                Name = t.name,
                Cost = t.cost,
                IsRepeatable = t.isRepeatable,
                Prerequisites = new List<string>(t.prerequisites)
            };

            techTree[t.name] = data;
        }
    }

    private PlayerResearchState GetState(int playerId)
    {
        if (!playerStates.ContainsKey(playerId))
        {
            var state = new PlayerResearchState();
            state.UnlockedTechs.Add("Agriculture");
            state.TechCompletions["Agriculture"] = 1;
            playerStates[playerId] = state;
        }

        return playerStates[playerId];
    }

    public bool HasTech(int playerId, string techId)
    {
        return GetState(playerId).UnlockedTechs.Contains(techId);
    }

    public int GetTechCompletions(int playerId, string techId)
    {
        var state = GetState(playerId);
        if (state.TechCompletions.TryGetValue(techId, out int count)) return count;
        return 0;
    }

    public bool CanResearch(int playerId, string techId)
    {
        if (!techTree.TryGetValue(techId, out TechData data)) return false;

        if (HasTech(playerId, techId) && !data.IsRepeatable) return false;

        foreach (var prereq in data.Prerequisites)
        {
            if (!HasTech(playerId, prereq)) return false;
        }

        return true;
    }

    public void SetResearch(int playerId, string techId)
    {
        if (!CanResearch(playerId, techId)) return;
        GetState(playerId).CurrentResearch = techId;
    }

    public void AddScience(int playerId, int amount)
    {
        var state = GetState(playerId);
        if (string.IsNullOrEmpty(state.CurrentResearch)) return;

        state.AccumulatedResearch += amount;
        TechData data = techTree[state.CurrentResearch];

        if (state.AccumulatedResearch >= data.Cost)
        {
            UnlockTech(playerId, state.CurrentResearch);
            state.AccumulatedResearch -= data.Cost;
            state.CurrentResearch = null;
        }
    }

    public void UnlockTech(int playerId, string techId)
    {
        var state = GetState(playerId);
        state.UnlockedTechs.Add(techId);

        if (!state.TechCompletions.ContainsKey(techId))
            state.TechCompletions[techId] = 0;

        state.TechCompletions[techId]++;

        if (techTree.TryGetValue(techId, out TechData data) && data.IsRepeatable)
        {
            OnRepeatableTechCompleted?.Invoke(playerId, techId, state.TechCompletions[techId]);
            Debug.Log(
                $"[TechManager] Player {playerId} discovered repeating technology {techId} (Times: {state.TechCompletions[techId]})");
        }
    }

    public string GetCurrentResearch(int playerId)
    {
        return GetState(playerId).CurrentResearch;
    }

    public int GetAccumulatedResearch(int playerId)
    {
        return GetState(playerId).AccumulatedResearch;
    }

    public int GetTechCost(string techId)
    {
        if (techTree.TryGetValue(techId, out TechData data)) return data.Cost;
        return 0;
    }

    public List<string> GetAllTechIds()
    {
        return techTree.Keys.ToList();
    }

    public string GetTechName(string techId)
    {
        if (techTree.TryGetValue(techId, out TechData data)) return data.Name;
        return techId;
    }
    
    public List<string> GetPrerequisites(string techId)
    {
        if (techTree.TryGetValue(techId, out TechData data))
        {
            return data.Prerequisites;
        }
        return new List<string>();
    }
}