using UnityEngine;
using System.Collections.Generic;

public class EconomyManager : MonoBehaviour
{
    public PlayerManager playerManager;
    public CityManager cityManager;
    public UnitManager unitManager;
    public TurnManager turnManager;

    [Header("Happiness Settings")] public int baseHappiness = 9;
    public int penaltyPerCity = 3;
    public int penaltyPerPopulation = 1;

    void OnEnable()
    {
        if (turnManager != null) turnManager.OnPlayerChanged += ProcessEconomy;
    }

    void OnDisable()
    {
        if (turnManager != null) turnManager.OnPlayerChanged -= ProcessEconomy;
    }

    private void ProcessEconomy(int playerId)
    {
        int income = GetIncome(playerId);
        var data = playerManager.GetPlayer(playerId);
        if (data != null)
        {
            data.gold += income;
        }
    }

    public int GetIncome(int playerId)
    {
        int populationIncome = 0;
        foreach (var city in cityManager.GetActiveCities())
        {
            if (city.ownerID == playerId) populationIncome += city.population;
        }

        int unitMaintenance = 0;
        foreach (var unit in unitManager.GetActiveUnits())
        {
            if (unit.ownerID == playerId) unitMaintenance++;
        }

        return populationIncome - unitMaintenance;
    }

    public int GetHappiness(int playerId)
    {
        int totalHappiness = baseHappiness;
        int totalPop = 0;
        int cityCount = 0;

        foreach (var city in cityManager.GetActiveCities())
        {
            if (city.ownerID == playerId)
            {
                cityCount++;
                totalPop += city.population;

                foreach (string buildingName in city.builtBuildings)
                {
                    if (cityManager.buildingDatabaseDict.TryGetValue(buildingName, out var bData))
                    {
                        foreach (var effect in bData.effects)
                        {
                            if (effect.type == "Happiness")
                            {
                                totalHappiness += effect.amount;
                            }
                        }
                    }
                }
            }
        }

        totalHappiness -= (cityCount * penaltyPerCity);
        totalHappiness -= (totalPop * penaltyPerPopulation);

        return totalHappiness;
    }
}