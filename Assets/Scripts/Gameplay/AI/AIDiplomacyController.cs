using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AIGrandStrategy))]
public class AIDiplomacyController : MonoBehaviour
{
    private AIGrandStrategy strategy;
    private DiplomacyManager diplomacyManager;
    private PlayerManager playerManager;
    private TurnManager turnManager;
    private UnitManager unitManager;

    private void Awake()
    {
        strategy = GetComponent<AIGrandStrategy>();
    }

    private void Start()
    {
        diplomacyManager = FindAnyObjectByType<DiplomacyManager>();
        playerManager = FindAnyObjectByType<PlayerManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
        unitManager = FindAnyObjectByType<UnitManager>();
    }

    public void ExecuteDiplomacyActions(int aiPlayerId)
    {
        if (diplomacyManager == null || playerManager == null || turnManager == null || unitManager == null) return;

        for (int otherId = 0; otherId < turnManager.TotalPlayers - 1; otherId++)
        {
            if (otherId == aiPlayerId) continue;

            DiplomaticState currentState = diplomacyManager.GetState(aiPlayerId, otherId);

            if (currentState == DiplomaticState.Neutral && strategy.currentState == AIState.WarPreparation)
            {
                if (!diplomacyManager.HasTruce(aiPlayerId, otherId))
                {
                    float myPower = GetMilitaryPower(aiPlayerId);
                    float enemyPower = GetMilitaryPower(otherId);
                    float powerRatio = enemyPower > 0f ? myPower / enemyPower : 5f;

                    float midpoint = 1.5f;
                    float maxDesireMultiplier = 1.0f;

                    if (strategy.basePersonality == AIPersonality.Militaristic)
                    {
                        midpoint = 1.1f;
                    }
                    else if (strategy.basePersonality == AIPersonality.Capitalist)
                    {
                        midpoint = 2.5f;
                        maxDesireMultiplier = 0.15f;
                    }
                    else if (strategy.basePersonality == AIPersonality.Expansionist)
                    {
                        midpoint = 1.8f;
                        maxDesireMultiplier = 0.3f;
                    }
                    else
                    {
                        midpoint = 1.5f;
                        maxDesireMultiplier = 0.4f;
                    }

                    float warDesire = ResponseCurves.Logistic(powerRatio, 5f, midpoint) * maxDesireMultiplier;

                    if (Random.value < warDesire)
                    {
                        diplomacyManager.SetState(aiPlayerId, otherId, DiplomaticState.War);
                    }
                }
            }

            if (currentState == DiplomaticState.War)
            {
                if (strategy.currentState == AIState.Panic || strategy.warExhaustion > 0.65f)
                {
                    PlayerData otherPlayer = playerManager.GetPlayer(otherId);
                    if (otherPlayer != null && otherPlayer.isAI)
                    {
                        bool accepted = EvaluatePeaceOffer(otherId, aiPlayerId);
                        if (accepted) diplomacyManager.SetState(aiPlayerId, otherId, DiplomaticState.Neutral);
                    }
                }
            }
        }
    }

    private float GetMilitaryPower(int playerId)
    {
        float power = 0f;
        foreach (Unit u in unitManager.GetActiveUnits())
        {
            if (u.ownerID == playerId && u.unitClass != UnitClass.Civilian)
            {
                power += (u.meleeStrength + u.rangedStrength);
            }
        }

        return power;
    }

    public bool EvaluatePeaceOffer(int aiId, int proposerId)
    {
        strategy.EvaluateState(aiId);

        if (strategy.currentState == AIState.Panic ||
            strategy.currentState == AIState.PeacefulDevelopment ||
            strategy.warExhaustion > 0.5f)
        {
            return true;
        }

        return false;
    }

    public bool EvaluateAllianceOffer(int aiId, int proposerId)
    {
        if (strategy.basePersonality == AIPersonality.Militaristic) return false;

        return (strategy.currentState == AIState.PeacefulDevelopment ||
                strategy.basePersonality == AIPersonality.Capitalist);
    }
}