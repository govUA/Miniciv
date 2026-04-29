using UnityEngine;
using System.Collections.Generic;

public class AITechController : MonoBehaviour
{
    public void ExecuteTechActions(int playerId)
    {
        TechManager techManager = FindObjectOfType<TechManager>();
        if (techManager == null) return;

        if (!string.IsNullOrEmpty(techManager.GetCurrentResearch(playerId))) return;

        UnitManager unitManager = FindObjectOfType<UnitManager>();
        CityManager cityManager = FindObjectOfType<CityManager>();

        List<string> availableTechs = new List<string>();
        List<float> techScores = new List<float>();

        foreach (string techId in techManager.GetAllTechIds())
        {
            if (techManager.CanResearch(playerId, techId))
            {
                float score = Random.Range(10f, 25f);

                if (unitManager != null)
                {
                    foreach (var kvp in unitManager.unitDatabaseDict)
                    {
                        if (kvp.Value.requiredTech == techId)
                            score += 20f;
                    }
                }

                if (cityManager != null)
                {
                    foreach (var kvp in cityManager.buildingDatabaseDict)
                    {
                        if (kvp.Value.requiredTech == techId)
                            score += 25f;
                    }
                }

                int cost = techManager.GetTechCost(techId);
                score -= (cost * 0.02f);

                availableTechs.Add(techId);
                techScores.Add(score);
            }
        }

        if (availableTechs.Count > 0)
        {
            string bestTech = null;
            float bestScore = -9999f;

            for (int i = 0; i < availableTechs.Count; i++)
            {
                if (techScores[i] > bestScore)
                {
                    bestScore = techScores[i];
                    bestTech = availableTechs[i];
                }
            }

            if (bestTech != null)
            {
                techManager.SetResearch(playerId, bestTech);
                Debug.Log(
                    $"[AI] Player {playerId} started researching: {techManager.GetTechName(bestTech)} (Score: {bestScore:F1})");
            }
        }
    }
}