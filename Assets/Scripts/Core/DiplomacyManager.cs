using UnityEngine;
using System;
using System.Collections.Generic;

public enum DiplomaticState
{
    Neutral,
    War,
    Alliance
}

public class DiplomacyManager : MonoBehaviour
{
    public static event Action<int, int, DiplomaticState> OnDiplomacyChanged;

    private PlayerManager playerManager;
    private TurnManager turnManager;

    private Dictionary<int, Dictionary<int, DiplomaticState>> relationships =
        new Dictionary<int, Dictionary<int, DiplomaticState>>();

    void Start()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
    }

    public DiplomaticState GetState(int playerA, int playerB)
    {
        if (turnManager != null)
        {
            int barbarianId = turnManager.TotalPlayers - 1;
            if (playerA == barbarianId || playerB == barbarianId)
                return DiplomaticState.War;
        }

        if (relationships.ContainsKey(playerA) && relationships[playerA].ContainsKey(playerB))
            return relationships[playerA][playerB];

        return DiplomaticState.Neutral;
    }

    public void SetState(int playerA, int playerB, DiplomaticState newState)
    {
        if (turnManager != null)
        {
            int barbarianId = turnManager.TotalPlayers - 1;
            if (playerA == barbarianId || playerB == barbarianId)
            {
                Debug.Log($"[DIPLOMACY] Blocked attempt to change state to {newState} with Barbarians.");
                return;
            }
        }

        if (!relationships.ContainsKey(playerA)) relationships[playerA] = new Dictionary<int, DiplomaticState>();
        if (!relationships.ContainsKey(playerB)) relationships[playerB] = new Dictionary<int, DiplomaticState>();

        relationships[playerA][playerB] = newState;
        relationships[playerB][playerA] = newState;

        if (newState == DiplomaticState.War)
        {
            playerManager.DeclareWar(playerA, playerB);
        }
        else
        {
            playerManager.MakePeace(playerA, playerB);
        }

        OnDiplomacyChanged?.Invoke(playerA, playerB, newState);
        Debug.Log($"[DIPLOMACY] Player {playerA} and Player {playerB} are now {newState}");
    }

    public bool ProposePeace(int proposerId, int targetId)
    {
        PlayerData targetData = playerManager.GetPlayer(targetId);

        if (targetData != null && targetData.isAI)
        {
            AIDiplomacyController aiDiplomacy = FindAnyObjectByType<AIDiplomacyController>();
            bool accepted = aiDiplomacy.EvaluatePeaceOffer(targetId, proposerId);

            if (accepted) SetState(proposerId, targetId, DiplomaticState.Neutral);
            return accepted;
        }
        else
        {
            SetState(proposerId, targetId, DiplomaticState.Neutral);
            return true;
        }
    }

    public bool ProposeAlliance(int proposerId, int targetId)
    {
        PlayerData targetData = playerManager.GetPlayer(targetId);

        if (targetData != null && targetData.isAI)
        {
            AIDiplomacyController aiDiplomacy = FindAnyObjectByType<AIDiplomacyController>();
            bool accepted = aiDiplomacy.EvaluateAllianceOffer(targetId, proposerId);

            if (accepted) SetState(proposerId, targetId, DiplomaticState.Alliance);
            return accepted;
        }

        return false;
    }
}