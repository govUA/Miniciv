using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AIGrandStrategy))]
public class AIDiplomacyController : MonoBehaviour
{
    private AIGrandStrategy strategy;
    private DiplomacyManager diplomacyManager;
    private PlayerManager playerManager;
    private TurnManager turnManager;

    private void Awake()
    {
        strategy = GetComponent<AIGrandStrategy>();
    }

    private void Start()
    {
        diplomacyManager = FindAnyObjectByType<DiplomacyManager>();
        playerManager = FindAnyObjectByType<PlayerManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
    }

    public void ExecuteDiplomacyActions(int aiPlayerId)
    {
        if (diplomacyManager == null || playerManager == null || turnManager == null) return;

        for (int otherId = 0; otherId < turnManager.TotalPlayers - 1; otherId++)
        {
            if (otherId == aiPlayerId) continue;

            DiplomaticState currentState = diplomacyManager.GetState(aiPlayerId, otherId);

            if (currentState == DiplomaticState.Neutral && strategy.currentState == AIState.WarPreparation)
            {
                if (Random.value < 0.15f)
                {
                    diplomacyManager.SetState(aiPlayerId, otherId, DiplomaticState.War);
                }
            }

            if (currentState == DiplomaticState.War && strategy.currentState == AIState.Panic)
            {
                PlayerData otherPlayer = playerManager.GetPlayer(otherId);
                if (otherPlayer.isAI)
                {
                    bool accepted = EvaluatePeaceOffer(otherId, aiPlayerId);
                    if (accepted) diplomacyManager.SetState(aiPlayerId, otherId, DiplomaticState.Neutral);
                }
            }
        }
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