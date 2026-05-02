using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AICityController))]
[RequireComponent(typeof(AIUnitController))]
[RequireComponent(typeof(AITechController))]
public class AIManager : MonoBehaviour
{
    public TurnManager turnManager;
    public PlayerManager playerManager;

    private AICityController cityController;
    private AIUnitController unitController;
    private AITechController techController;
    private AIGrandStrategy grandStrategy;

    private void Awake()
    {
        cityController = GetComponent<AICityController>();
        unitController = GetComponent<AIUnitController>();
        techController = GetComponent<AITechController>();
        grandStrategy = GetComponent<AIGrandStrategy>();
    }

    private void Start()
    {
        if (turnManager != null)
        {
            turnManager.OnPlayerChanged += HandlePlayerTurn;
        }
    }

    private void HandlePlayerTurn(int playerId)
    {
        PlayerData currentPlayer = playerManager.GetPlayer(playerId);

        if (currentPlayer != null && currentPlayer.isAI)
        {
            StartCoroutine(ExecuteAITurn(playerId));
        }
    }

    private IEnumerator ExecuteAITurn(int playerId)
    {
        Debug.Log($"AI Player {playerId} makes decision...");

        if (grandStrategy != null)
        {
            if (turnManager.CurrentTurn == 1) grandStrategy.InitializeStrategy(playerId);

            grandStrategy.EvaluateState(playerId);
        }

        yield return new WaitForSeconds(0.5f);

        techController.ExecuteTechActions(playerId);
        cityController.ExecuteCityActions(playerId);
        unitController.ExecuteUnitActions(playerId);

        yield return new WaitForSeconds(0.5f);

        if (turnManager != null && turnManager.CurrentPlayerID == playerId)
        {
            turnManager.EndTurn();
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnPlayerChanged -= HandlePlayerTurn;
        }
    }
}