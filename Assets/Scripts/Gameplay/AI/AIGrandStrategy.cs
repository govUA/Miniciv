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
    private TurnManager turnManager;

    [Header("Current Status (Read Only)")] public AIState currentState;
    public AIPersonality basePersonality;

    public float militaryWeight { get; private set; }
    public float economyWeight { get; private set; }
    public float expansionWeight { get; private set; }
    public float scienceWeight { get; private set; }

    public float warExhaustion { get; private set; } = 0f;

    public void InitializeStrategy(int playerId)
    {
        playerManager = FindObjectOfType<PlayerManager>();
        unitManager = FindObjectOfType<UnitManager>();
        cityManager = FindObjectOfType<CityManager>();
        economyManager = FindObjectOfType<EconomyManager>();
        grid = FindObjectOfType<HexGrid>();
        turnManager = FindObjectOfType<TurnManager>();

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
        turnManager = FindAnyObjectByType<TurnManager>();
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
        if (cityManager == null || unitManager == null || playerManager == null || turnManager == null) return;

        int barbarianId = turnManager.TotalPlayers - 1;
        PlayerData pd = playerManager.GetPlayer(playerId);

        float myMilitaryPower = 0f;
        int actualArmySize = 0;
        foreach (Unit u in unitManager.GetActiveUnits())
        {
            if (u.ownerID == playerId && u.unitClass != UnitClass.Civilian)
            {
                myMilitaryPower += (u.meleeStrength + u.rangedStrength);
                actualArmySize++;
            }
        }

        bool isAtWar = false;
        bool isLosingWar = false;
        int enemyMilitaryPower = 0;

        if (pd != null)
        {
            foreach (int enemyId in pd.atWarWith)
            {
                if (enemyId != barbarianId) isAtWar = true;
            }
        }

        warExhaustion = 0f;

        if (isAtWar)
        {
            foreach (Unit u in unitManager.GetActiveUnits())
            {
                if (u.ownerID != barbarianId && u.unitClass != UnitClass.Civilian &&
                    playerManager.IsAtWar(playerId, u.ownerID))
                {
                    enemyMilitaryPower += (u.meleeStrength + u.rangedStrength);
                }
            }

            if (enemyMilitaryPower > myMilitaryPower * 2.0f) isLosingWar = true;

            if (pd != null && pd.gold < 20f) warExhaustion += 0.5f;

            int myCitiesCount = 0;
            foreach (var city in cityManager.GetActiveCities())
                if (city.ownerID == playerId)
                    myCitiesCount++;

            float desiredArmySize = (myCitiesCount * 3f) + 2f;
            float armyDeficit = 1f - Mathf.Clamp01(actualArmySize / desiredArmySize);
            if (armyDeficit > 0) warExhaustion += armyDeficit * 0.7f;

            warExhaustion = Mathf.Clamp01(warExhaustion);
        }

        if (isLosingWar)
        {
            currentState = AIState.Panic;
        }
        else if (isAtWar)
        {
            currentState = AIState.WarPreparation;
        }
        else
        {
            int availableFreeTiles = 0;
            if (grid != null)
            {
                for (int x = 0; x < grid.GetWidth(); x += 3)
                {
                    for (int y = 0; y < grid.GetHeight(); y += 3)
                    {
                        HexNode n = grid.GetNode(x, y);
                        if (n != null && n.isLand && n.ownerID == -1) availableFreeTiles++;
                    }
                }
            }

            float scorePeace = 50f;
            float scoreExpansion = availableFreeTiles * 4f;
            float scoreWarPrep = 0f;

            if (basePersonality == AIPersonality.Expansionist)
            {
                scoreExpansion += 40f;
                scorePeace -= 10f;
            }
            else if (basePersonality == AIPersonality.Capitalist)
            {
                scorePeace += 50f;
                scoreWarPrep -= 30f;
            }
            else if (basePersonality == AIPersonality.Militaristic)
            {
                scoreWarPrep += 40f;
                scorePeace -= 20f;
            }

            if (availableFreeTiles < 4)
            {
                scoreWarPrep += 50f;
                scoreExpansion = 0f;
            }

            if (scoreWarPrep > scorePeace && scoreWarPrep > scoreExpansion) currentState = AIState.WarPreparation;
            else if (scoreExpansion > scorePeace) currentState = AIState.Expansion;
            else currentState = AIState.PeacefulDevelopment;
        }

        CalculateWeights(0, 0);
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
                break;
        }
    }
}