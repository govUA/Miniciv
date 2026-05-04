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
                float myPower = GetMilitaryPower(aiPlayerId);
                float enemyPower = GetMilitaryPower(otherId);
                float powerRatio = enemyPower > 0f ? myPower / enemyPower : 5f;
                float warDesire = ResponseCurves.Logistic(powerRatio, 4f, 1.5f);

                if (strategy.basePersonality == AIPersonality.Militaristic) warDesire += 0.2f;
                if (strategy.basePersonality == AIPersonality.Capitalist) warDesire -= 0.15f;

                if (Random.value < warDesire)
                {
                    diplomacyManager.SetState(aiPlayerId, otherId, DiplomaticState.War);
                }
            }

            if (currentState == DiplomaticState.War && strategy.currentState == AIState.Panic)
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
        if (strategy.currentState == AIState.Panic || strategy.currentState == AIState.PeacefulDevelopment)
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