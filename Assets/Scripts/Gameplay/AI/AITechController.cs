using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AIGrandStrategy))]
public class AITechController : MonoBehaviour
{
    private AIGrandStrategy strategy;

    private void Awake()
    {
        strategy = GetComponent<AIGrandStrategy>();
    }

    public void ExecuteTechActions(int playerId)
    {
        TechManager techManager = FindObjectOfType<TechManager>();
        if (techManager == null || !string.IsNullOrEmpty(techManager.GetCurrentResearch(playerId))) return;

        UnitManager unitManager = FindObjectOfType<UnitManager>();
        CityManager cityManager = FindObjectOfType<CityManager>();

        string bestTech = null;
        float bestScore = -9999f;

        foreach (string techId in techManager.GetAllTechIds())
        {
            if (techManager.CanResearch(playerId, techId))
            {
                float score = 10f;

                if (unitManager != null)
                {
                    foreach (var kvp in unitManager.unitDatabaseDict)
                    {
                        if (kvp.Value.requiredTech == techId)
                        {
                            if (kvp.Value.unitClass != "Civilian")
                                score += 20f * strategy.militaryWeight;
                        }
                    }
                }

                if (cityManager != null)
                {
                    foreach (var kvp in cityManager.buildingDatabaseDict)
                    {
                        if (kvp.Value.requiredTech == techId)
                        {
                            bool hasEcon = false;
                            bool hasMilitary = false;
                            foreach (var effect in kvp.Value.effects)
                            {
                                if (effect.type == "Gold" || effect.type == "Food" || effect.type == "Science")
                                    hasEcon = true;
                                if (effect.type == "MaxHP" || effect.type == "MilitaryProdBonus" ||
                                    effect.type == "NavalProdBonus") hasMilitary = true;
                            }

                            if (hasEcon) score += 25f * strategy.economyWeight;
                            if (hasMilitary) score += 15f * strategy.militaryWeight;
                        }
                    }
                }

                int cost = techManager.GetTechCost(techId);
                score -= (cost * 0.05f);

                score *= Random.Range(0.9f, 1.1f);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTech = techId;
                }
            }
        }

        if (bestTech != null)
        {
            techManager.SetResearch(playerId, bestTech);
            Debug.Log(
                $"[AI] Player {playerId} researches: {techManager.GetTechName(bestTech)} (Score: {bestScore:F1}) based on state: {strategy.currentState}");
        }
    }
}