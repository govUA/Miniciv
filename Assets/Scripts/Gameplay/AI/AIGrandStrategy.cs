using UnityEngine;
using System.Collections.Generic;

public enum AIState
{
    PeacefulDevelopment,
    Expansion,
    WarPreparation,
    Panic
}

public enum AIPersonality
{
    Balanced,
    Militaristic,
    Expansionist,
    Capitalist
}

public class AIGrandStrategy : MonoBehaviour
{
    private PlayerManager playerManager;
    private UnitManager unitManager;
    private CityManager cityManager;
    private EconomyManager economyManager;
    private HexGrid grid;

    [Header("Current Status (Read Only)")] public AIState currentState;
    public AIPersonality basePersonality;

    public float militaryWeight { get; private set; }
    public float economyWeight { get; private set; }
    public float expansionWeight { get; private set; }
    public float scienceWeight { get; private set; }

    public void InitializeStrategy(int playerId)
    {
        playerManager = FindObjectOfType<PlayerManager>();
        unitManager = FindObjectOfType<UnitManager>();
        cityManager = FindObjectOfType<CityManager>();
        economyManager = FindObjectOfType<EconomyManager>();
        grid = FindObjectOfType<HexGrid>();

        DeterminePersonality(playerId);
        EvaluateState(playerId);
    }

    private void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
        unitManager = FindAnyObjectByType<UnitManager>();
        cityManager = FindAnyObjectByType<CityManager>();
        economyManager = FindAnyObjectByType<EconomyManager>();
        grid = FindAnyObjectByType<HexGrid>();
    }

    private void DeterminePersonality(int playerId)
    {
        PlayerData pd = playerManager.GetPlayer(playerId);

        if (pd == null || pd.civilization == null || string.IsNullOrEmpty(pd.civilization.aiPersonality))
        {
            basePersonality = AIPersonality.Balanced;
            return;
        }

        if (System.Enum.TryParse(pd.civilization.aiPersonality, true, out AIPersonality parsedPersonality))
        {
            basePersonality = parsedPersonality;
        }
        else
        {
            basePersonality = AIPersonality.Balanced;
        }
    }

    public void EvaluateState(int playerId)
    {
        if (cityManager == null) cityManager = FindAnyObjectByType<CityManager>();
        if (unitManager == null) unitManager = FindAnyObjectByType<UnitManager>();
        if (economyManager == null) economyManager = FindAnyObjectByType<EconomyManager>();
        if (grid == null) grid = FindAnyObjectByType<HexGrid>();
        if (playerManager == null) playerManager = FindAnyObjectByType<PlayerManager>();

        int myCities = 0;
        int myMilitaryPower = 0;

        foreach (City c in cityManager.GetActiveCities())
            if (c.ownerID == playerId)
                myCities++;

        foreach (Unit u in unitManager.GetActiveUnits())
            if (u.ownerID == playerId && u.unitClass != UnitClass.Civilian)
                myMilitaryPower += (u.meleeStrength + u.rangedStrength);

        int happiness = economyManager != null ? economyManager.GetHappiness(playerId) : 10;
        int income = economyManager != null ? economyManager.GetIncome(playerId) : 10;

        int availableFreeTiles = 0;
        if (grid != null)
        {
            foreach (City c in cityManager.GetActiveCities())
            {
                if (c.ownerID == playerId)
                {
                    List<HexNode> nearbyNodes = grid.GetNodesInRange(c.centerNode, 5);
                    foreach (HexNode node in nearbyNodes)
                    {
                        if (node.isLand && node.ownerID == -1) availableFreeTiles++;
                    }
                }
            }
        }

        bool isAtWar = false;
        bool isLosingWar = false;
        int enemyMilitaryPower = 0;

        if (playerManager != null)
        {
            PlayerData pd = playerManager.GetPlayer(playerId);
            if (pd != null && pd.atWarWith.Count > 0)
            {
                isAtWar = true;
            }
        }

        if (isAtWar && unitManager != null)
        {
            foreach (Unit u in unitManager.GetActiveUnits())
            {
                if (u.unitClass != UnitClass.Civilian && playerManager.IsAtWar(playerId, u.ownerID))
                {
                    enemyMilitaryPower += (u.meleeStrength + u.rangedStrength);
                }
            }

            if (enemyMilitaryPower > myMilitaryPower * 2.0f)
            {
                isLosingWar = true;
            }
        }

        if (isLosingWar)
        {
            currentState = AIState.Panic;
        }
        else if (isAtWar || (basePersonality == AIPersonality.Militaristic && myMilitaryPower < 100))
        {
            currentState = AIState.WarPreparation;
        }
        else if (availableFreeTiles > 15 && happiness > 2 && income > -5)
        {
            currentState = AIState.Expansion;
        }
        else
        {
            currentState = AIState.PeacefulDevelopment;
        }

        CalculateWeights(happiness, availableFreeTiles);
    }

    private void CalculateWeights(int happiness, int freeTiles)
    {
        militaryWeight = basePersonality == AIPersonality.Militaristic ? 1.5f : 1.0f;
        expansionWeight = basePersonality == AIPersonality.Expansionist ? 1.5f : 1.0f;
        economyWeight = basePersonality == AIPersonality.Capitalist ? 1.5f : 1.0f;
        scienceWeight = 1.2f;

        switch (currentState)
        {
            case AIState.Panic:
                militaryWeight *= 3.0f;
                economyWeight *= 0.5f;
                expansionWeight = 0f;
                scienceWeight *= 0.5f;
                break;
            case AIState.WarPreparation:
                militaryWeight *= 2.0f;
                economyWeight *= 1.2f;
                expansionWeight *= 0.2f;
                break;
            case AIState.Expansion:
                expansionWeight *= 2.5f;
                militaryWeight *= 0.8f;
                economyWeight *= 1.2f;
                break;
            case AIState.PeacefulDevelopment:
                economyWeight *= 1.5f;
                scienceWeight *= 1.5f;
                militaryWeight *= 0.5f;

                if (happiness < 0 || freeTiles < 10) expansionWeight = 0f;
                break;
        }
    }
}