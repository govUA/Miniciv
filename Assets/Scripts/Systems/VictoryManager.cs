using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class VictoryManager : MonoBehaviour
{
    public TurnManager turnManager;
    public CityManager cityManager;
    public UnitManager unitManager;
    public PlayerManager playerManager;

    [Header("Settings")] public int maxTurns = 100;

    private bool gameEnded = false;
    private HashSet<int> eliminatedPlayers = new HashSet<int>();

    void OnEnable()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnEnded += CheckTimeLimit;
            turnManager.OnPlayerChanged += CheckElimination;
        }
    }

    void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnEnded -= CheckTimeLimit;
            turnManager.OnPlayerChanged -= CheckElimination;
        }
    }

    private void CheckElimination(int playerId)
    {
        if (gameEnded || eliminatedPlayers.Contains(playerId)) return;

        bool hasCities = cityManager.GetActiveCities().Any(c => c.ownerID == playerId);
        bool hasSettlers = unitManager.GetActiveUnits().Any(u => u.ownerID == playerId && u.isSettler);

        if (!hasCities && !hasSettlers)
        {
            Debug.Log($"[VICTORY] Player {playerId} was defeated!");
            eliminatedPlayers.Add(playerId);

            if (turnManager.CurrentPlayerID == playerId)
            {
                turnManager.EndTurn();
            }
        }
    }

    private void CheckTimeLimit()
    {
        if (gameEnded) return;

        if (turnManager.CurrentTurn > maxTurns)
        {
            EvaluateVictory();
        }
    }

    private void EvaluateVictory()
    {
        gameEnded = true;
        Debug.Log($"[VICTORY] The game has reached its limit at {maxTurns} turns. Deciding the winner...");

        int totalPlayers = turnManager.TotalPlayers;

        float[] armyScores = new float[totalPlayers];
        float[] cityScores = new float[totalPlayers];
        float[] goldScores = new float[totalPlayers];

        for (int i = 0; i < totalPlayers; i++)
        {
            if (eliminatedPlayers.Contains(i)) continue;

            armyScores[i] = unitManager.GetActiveUnits()
                .Where(u => u.ownerID == i)
                .Sum(u => u.meleeStrength + u.rangedStrength);

            cityScores[i] = cityManager.GetActiveCities()
                .Count(c => c.ownerID == i);

            var playerData = playerManager.GetPlayer(i);
            goldScores[i] = playerData != null ? playerData.gold : 0;
        }

        float maxArmy = Mathf.Max(armyScores.Max(), 1f);
        float maxCities = Mathf.Max(cityScores.Max(), 1f);
        float maxGold = Mathf.Max(goldScores.Max(), 1f);

        int winnerId = -1;
        float bestScore = -1f;

        for (int i = 0; i < totalPlayers; i++)
        {
            if (eliminatedPlayers.Contains(i)) continue;

            float normalizedScore = (armyScores[i] / maxArmy) +
                                    (cityScores[i] / maxCities) +
                                    (goldScores[i] / maxGold);

            Debug.Log(
                $"[VICTORY] Player {i} | Army: {armyScores[i]}, Cities: {cityScores[i]}, Gold: {goldScores[i]} | Total score: {normalizedScore:F2}");

            if (normalizedScore > bestScore)
            {
                bestScore = normalizedScore;
                winnerId = i;
            }
        }

        if (winnerId != -1)
        {
            Debug.Log($"<color=green>[VICTORY] WINNER: Player {winnerId} with score {bestScore:F2}!</color>");
            // TODO: UI
        }
        else
        {
            Debug.Log("<color=red>[VICTORY] All players were defeated, no winner!</color>");
        }
    }
}