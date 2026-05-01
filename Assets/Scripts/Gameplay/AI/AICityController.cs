using UnityEngine;
using System.Collections.Generic;

public class AICityController : MonoBehaviour
{
    public CityManager cityManager;
    public UnitManager unitManager;

    public void ExecuteCityActions(int playerId)
    {
        if (cityManager == null) cityManager = FindObjectOfType<CityManager>();
        if (unitManager == null) unitManager = FindObjectOfType<UnitManager>();
        TechManager techManager = FindObjectOfType<TechManager>();

        int myCitiesCount = 0;
        foreach (City c in cityManager.GetActiveCities())
            if (c.ownerID == playerId)
                myCitiesCount++;

        int myMilitaryCount = 0;
        int mySettlersCount = 0;
        foreach (Unit u in unitManager.GetActiveUnits())
        {
            if (u.ownerID == playerId)
            {
                if (u.unitClass == UnitClass.Civilian) mySettlersCount++;
                else myMilitaryCount++;
            }
        }

        foreach (City city in cityManager.GetActiveCities())
        {
            if (city.ownerID != playerId) continue;

            if (city.currentProject == null)
            {
                CityProject bestProject = ChooseBestProjectForCity(city, playerId, techManager, myCitiesCount,
                    myMilitaryCount, mySettlersCount);

                if (bestProject != null)
                {
                    city.SetProject(bestProject);
                }
                else
                {
                    city.SetProject(new CityProject("Repair", ProjectType.Process, 0));
                }
            }
        }
    }

    private CityProject ChooseBestProjectForCity(City city, int playerId, TechManager techManager, int citiesCount,
        int militaryCount, int settlersCount)
    {
        List<CityProject> availableProjects = new List<CityProject>();
        List<float> projectScores = new List<float>();

        foreach (var kvp in unitManager.unitDatabaseDict)
        {
            UnitDataModel unitData = kvp.Value;

            if (!string.IsNullOrEmpty(unitData.requiredTech) && techManager != null)
            {
                if (!techManager.HasTech(playerId, unitData.requiredTech)) continue;
            }

            if (unitData.requiredPopulation > 0 && city.population < unitData.requiredPopulation) continue;

            float score = Random.Range(5f, 15f);

            if (unitData.unitClass == "Civilian" && unitData.name.ToLower() == "settler")
            {
                if (citiesCount + settlersCount < 4) score += 50f;
                else score -= 30f;
            }
            else
            {
                if (militaryCount < citiesCount * 3)
                {
                    score += 20f;

                    score += (unitData.meleeStrength + unitData.rangedStrength) * 0.4f;

                    if (unitData.unitClass == "Ranged") score += 5f;
                    if (unitData.unitClass == "Cavalry") score += 10f;
                    if (unitData.unitClass == "AntiCavalry") score += 5f;
                }
                else
                {
                    score -= 20f;
                }
            }

            availableProjects.Add(
                new CityProject(unitData.name, ProjectType.Unit, unitData.cost, unitData.requiredTech));
            projectScores.Add(score);
        }

        foreach (var kvp in cityManager.buildingDatabaseDict)
        {
            BuildingDataModel bData = kvp.Value;

            if (city.builtBuildings.Contains(bData.name)) continue;

            if (!string.IsNullOrEmpty(bData.requiredTech) && techManager != null)
            {
                if (!techManager.HasTech(playerId, bData.requiredTech)) continue;
            }

            float score = Random.Range(15f, 35f);

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