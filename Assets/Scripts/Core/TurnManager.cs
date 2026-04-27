using UnityEngine;
using System;

public class TurnManager : MonoBehaviour
{
    public event Action OnTurnEnded;
    public event Action<int> OnPlayerChanged;

    public int CurrentTurn { get; private set; } = 1;
    public int CurrentPlayerID { get; private set; } = 0;
    public int TotalPlayers = 2;

    public void EndTurn()
    {
        CurrentPlayerID++;

        if (CurrentPlayerID >= TotalPlayers)
        {
            CurrentPlayerID = 0;
            CurrentTurn++;
            Debug.Log("Global Turn Ended. Current Turn: " + CurrentTurn);
            OnTurnEnded?.Invoke();
        }

        Debug.Log("Active Player: " + CurrentPlayerID);
        OnPlayerChanged?.Invoke(CurrentPlayerID);
    }
}