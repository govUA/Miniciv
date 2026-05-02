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

        DeterminePersonality(playerId);
        EvaluateState(playerId);
    }

    private void DeterminePersonality(int playerId)
    {
        PlayerData pd = playerManager.GetPlayer(playerId);

        string personalityString = pd.civilization.aiPersonality;

        if (!string.IsNullOrEmpty(personalityString) &&
            System.Enum.TryParse(personalityString, true, out AIPersonality parsedPersonality))
        {
            basePersonality = parsedPersonality;
        }
        else
        {
            Debug.LogWarning(
                $"[AIGrandStrategy] Unknown or missing personality '{personalityString}' for leader {pd.civilization.leaderName}. Defaulting to Balanced.");
            basePersonality = AIPersonality.Balanced;
        }

        float randomVariance = Random.Range(0.8f, 1.2f);
    }

    public void EvaluateState(int playerId)
    {
        int myCities = 0;
        int myMilitaryPower = 0;

        foreach (City c in cityManager.GetActiveCities())
            if (c.ownerID == playerId)
                myCities++;

        foreach (Unit u in unitManager.GetActiveUnits())
            if (u.ownerID == playerId && u.unitClass != UnitClass.Civilian)
                myMilitaryPower += (u.meleeStrength + u.rangedStrength);

        bool isAtWar = false; // DiplomacyManager: playerManager.IsAtWar(playerId)
        bool isLosingWar = false;

        if (isLosingWar)
        {
            currentState = AIState.Panic;
        }
        else if (isAtWar || (basePersonality == AIPersonality.Militaristic && myMilitaryPower < 100))
        {
            currentState = AIState.WarPreparation;
        }
        else if (myCities < 5 || (basePersonality == AIPersonality.Expansionist && myCities < 8))
        {
            currentState = AIState.Expansion;
        }
        else
        {
            currentState = AIState.PeacefulDevelopment;
        }

        CalculateWeights();
    }

    private void CalculateWeights()
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
                expansionWeight *= 0.5f;
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
                break;
        }
    }
}