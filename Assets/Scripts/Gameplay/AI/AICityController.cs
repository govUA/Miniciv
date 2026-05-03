using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AIGrandStrategy))]
public class AICityController : MonoBehaviour
{
    public CityManager cityManager;
    public UnitManager unitManager;
    private AIGrandStrategy strategy;

    private void Awake()
    {
        strategy = GetComponent<AIGrandStrategy>();
    }

    public void ExecuteCityActions(int playerId)
    {
        if (cityManager == null) cityManager = FindObjectOfType<CityManager>();
        if (unitManager == null) unitManager = FindObjectOfType<UnitManager>();
        TechManager techManager = FindObjectOfType<TechManager>();

        foreach (City city in cityManager.GetActiveCities())
        {
            if (city.ownerID != playerId) continue;

            if (city.currentProject == null)
            {
                CityProject bestProject = ChooseBestProjectForCity(city, playerId, techManager);

                if (bestProject != null)
                {
                    city.SetProject(bestProject);
                    Debug.Log(
                        $"[AI City] {city.cityName} is building {bestProject.name} (State: {strategy.currentState})");
                }
                else
                {
                    city.SetProject(new CityProject("Repair", ProjectType.Process, 0));
                }
            }
        }
    }

    private CityProject ChooseBestProjectForCity(City city, int playerId, TechManager techManager)
    {
        List<CityProject> availableProjects = new List<CityProject>();
        List<float> projectScores = new List<float>();

        EconomyManager ecoManager = FindObjectOfType<EconomyManager>();
        int currentHappiness = ecoManager != null ? ecoManager.GetHappiness(playerId) : 10;

        PlayerManager pm = FindObjectOfType<PlayerManager>();
        PlayerData pd = pm != null ? pm.GetPlayer(playerId) : null;

        bool hasWaterNeighbor = false;
        HexGrid grid = FindObjectOfType<HexGrid>();
        if (grid != null && city != null)
        {
            foreach (HexNode neighbor in grid.GetNeighbors(city.centerNode))
                if (!neighbor.isLand)
                {
                    hasWaterNeighbor = true;
                    break;
                }
        }

        foreach (var kvp in unitManager.unitDatabaseDict)
        {
            UnitDataModel unitData = kvp.Value;
            if (unitData.unitClass == "Naval" && !hasWaterNeighbor) continue;
            if (!string.IsNullOrEmpty(unitData.requiredTech) && techManager != null &&
                !techManager.HasTech(playerId, unitData.requiredTech)) continue;
            if (unitData.requiredPopulation > 0 && city.population < unitData.requiredPopulation) continue;

            float score = 10f;

            if (unitData.unitClass == "Civilian" && unitData.name.ToLower() == "settler")
            {
                score += 50f * strategy.expansionWeight;

                if (city.population <= 2) score -= 100f;

                if (currentHappiness < 1 || strategy.expansionWeight == 0f)
                {
                    score = -9999f;
                }
            }
            else if (unitData.unitClass != "Civilian")
            {
                score += (unitData.meleeStrength + unitData.rangedStrength) * strategy.militaryWeight;

                if (strategy.currentState == AIState.Panic) score += 100f;
            }

            if (score > -1000f)
            {
                score *= Random.Range(0.9f, 1.1f);
                availableProjects.Add(new CityProject(unitData.name, ProjectType.Unit, unitData.cost,
                    unitData.requiredTech));
                projectScores.Add(score);
            }
        }

        foreach (var kvp in cityManager.buildingDatabaseDict)
        {
            BuildingDataModel bData = kvp.Value;
            if (city.builtBuildings.Contains(bData.name)) continue;
            if (bData.name == "Port" && !hasWaterNeighbor) continue;
            if (!string.IsNullOrEmpty(bData.requiredTech) && techManager != null &&
                !techManager.HasTech(playerId, bData.requiredTech)) continue;

            if (bData.isWonder)
            {
                if (pd == null || !pd.unlockedWonders.Contains(bData.name)) continue;
                if (cityManager.IsWonderBuiltOrBuilding(playerId, bData.name)) continue;
            }

            float score = 5f;

            if (bData.isWonder) score += 1000f;

            foreach (var effect in bData.effects)
            {
                switch (effect.type)
                {
                    case "Gold":
                    case "Production":
                    case "Food":
                        score += effect.amount * 10f * strategy.economyWeight;
                        break;
                    case "Science":
                        score += effect.amount * 10f * strategy.scienceWeight;
                        break;
                    case "Culture":
                        score += effect.amount * 5f * strategy.expansionWeight;
                        break;
                    case "MaxHP":
                    case "Garrison":
                    case "MilitaryProdBonus":
                    case "NavalProdBonus":
                        score += effect.amount * 0.5f * strategy.militaryWeight;
                        break;
                }
            }

            score *= Random.Range(0.9f, 1.1f);

            availableProjects.Add(new CityProject(bData.name, ProjectType.Building, bData.cost, bData.requiredTech));
            projectScores.Add(score);
        }

        CityProject bestProj = null;
        float bestScore = -999f;
        for (int i = 0; i < availableProjects.Count; i++)
        {
            if (projectScores[i] > bestScore)
            {
                bestScore = projectScores[i];
                bestProj = availableProjects[i];
            }
        }

        return bestProj;
    }
}