using UnityEngine;
using System;

public class TurnManager : MonoBehaviour
{
    public event Action OnTurnEnded;
    public int CurrentTurn { get; private set; } = 1;

    public void EndTurn()
    {
        CurrentTurn++;
        Debug.Log("Turn Ended. Current Turn: " + CurrentTurn);
        OnTurnEnded?.Invoke();
    }
}