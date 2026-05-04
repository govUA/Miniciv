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

    [Header("Truce Settings")] public int initialGracePeriod = 15;
    public int postWarTruceTurns = 10;
    private Dictionary<int, Dictionary<int, int>> truceTimers = new Dictionary<int, Dictionary<int, int>>();

    void Start()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
    }

    private void OnEnable()
    {
        if (turnManager != null) turnManager.OnTurnEnded += DecrementTruces;
    }

    private void OnDisable()
    {
        if (turnManager != null) turnManager.OnTurnEnded -= DecrementTruces;
    }

    private void DecrementTruces()
    {
        List<int> players = new List<int>(truceTimers.Keys);
        foreach (int pA in players)
        {
            List<int> targets = new List<int>(truceTimers[pA].Keys);
            foreach (int pB in targets)
            {
                if (truceTimers[pA][pB] > 0) truceTimers[pA][pB]--;
            }
        }
    }

    private void SetTruceTimer(int playerA, int playerB, int turns)
    {
        if (!truceTimers.ContainsKey(playerA)) truceTimers[playerA] = new Dictionary<int, int>();
        if (!truceTimers.ContainsKey(playerB)) truceTimers[playerB] = new Dictionary<int, int>();

        truceTimers[playerA][playerB] = turns;
        truceTimers[playerB][playerA] = turns;
    }

    public bool HasTruce(int playerA, int playerB)
    {
        if (turnManager != null)
        {
            int barbarianId = turnManager.TotalPlayers - 1;

            if (playerA == barbarianId || playerB == barbarianId) return false;
            if (turnManager.CurrentTurn <= initialGracePeriod) return true;
        }

        if (truceTimers.ContainsKey(playerA) && truceTimers[playerA].ContainsKey(playerB))
        {
            return truceTimers[playerA][playerB] > 0;
        }

        return false;
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
                return;
            }
        }

        if (newState == DiplomaticState.War && HasTruce(playerA, playerB))
        {
            Debug.Log($"[DIPLOMACY] Blocked War declaration between {playerA} and {playerB} due to Truce.");
            return;
        }

        DiplomaticState oldState = GetState(playerA, playerB);

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

            if (oldState == DiplomaticState.War && newState == DiplomaticState.Neutral)
            {
                SetTruceTimer(playerA, playerB, postWarTruceTurns);
                Debug.Log($"[DIPLOMACY] Truce signed between {playerA} and {playerB} for {postWarTruceTurns} turns.");
            }
        }

        OnDiplomacyChanged?.Invoke(playerA, playerB, newState);
    }

    public bool ProposePeace(int proposerId, int targetId)
    {
        PlayerData targetData = playerManager.GetPlayer(targetId);

        if (targetData != null && targetData.isAI)
        {
            AIDiplomacyController aiDiplomacy = FindAnyObjectByType<AIDiplomacyController>();
            if (aiDiplomacy != null)
            {
                bool accepted = aiDiplomacy.EvaluatePeaceOffer(targetId, proposerId);
                if (accepted) SetState(proposerId, targetId, DiplomaticState.Neutral);
                return accepted;
            }
        }

        SetState(proposerId, targetId, DiplomaticState.Neutral);
        return true;
    }

    public bool ProposeAlliance(int proposerId, int targetId)
    {
        PlayerData targetData = playerManager.GetPlayer(targetId);

        if (targetData != null && targetData.isAI)
        {
            AIDiplomacyController aiDiplomacy = FindAnyObjectByType<AIDiplomacyController>();
            if (aiDiplomacy != null)
            {
                bool accepted = aiDiplomacy.EvaluateAllianceOffer(targetId, proposerId);
                if (accepted) SetState(proposerId, targetId, DiplomaticState.Alliance);
                return accepted;
            }
        }

        SetState(proposerId, targetId, DiplomaticState.Alliance);
        return true;
    }
}