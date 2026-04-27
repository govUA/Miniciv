using UnityEngine;
using System.Collections.Generic;

public class EconomyManager : MonoBehaviour
{
    public PlayerManager playerManager;
    public CityManager cityManager;
    public UnitManager unitManager;
    public TurnManager turnManager;

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
}