using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIManager : MonoBehaviour
{
    public TurnManager turnManager;
    public PlayerManager playerManager;
    public UnitManager unitManager;

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
        Debug.Log($"ШІ Гравець {playerId} приймає рішення...");
        yield return new WaitForSeconds(0.5f);

        ExecuteUnitActions(playerId);

        yield return new WaitForSeconds(0.5f);

        turnManager.EndTurn();
    }

    private void ExecuteUnitActions(int playerId)
    {
        List<Unit> allUnits = unitManager.GetActiveUnits();
        Pathfinder pathfinder = FindObjectOfType<Pathfinder>();
        HexGrid grid = FindObjectOfType<HexGrid>();

        foreach (Unit unit in allUnits)
        {
            if (unit.ownerID != playerId || unit.State != UnitState.Idle || unit.currentMP <= 0) continue;

            HexNode targetNode = EvaluateBestDestination(unit, grid);

            if (targetNode != null && targetNode != unit.CurrentNode)
            {
                List<HexNode> path = pathfinder.FindPath(unit, targetNode);

                if (path != null && path.Count > 0)
                {
                    unit.MoveAlongPath(path);
                }
            }
        }
    }

    private HexNode EvaluateBestDestination(Unit unit, HexGrid grid)
    {
        HexNode bestNode = null;
        float highestUtility = -1f;

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                HexNode node = grid.GetNode(x, y);

                if (node == null || !node.isLand || node == unit.CurrentNode) continue;

                float utility = 0f;

                if (unit.isSettler)
                {
                    float distance = Vector2.Distance(new Vector2(unit.CurrentNode.x, unit.CurrentNode.y),
                        new Vector2(node.x, node.y));
                    float distanceScore = 1f / (distance + 1f);

                    float resourceScore = Random.Range(0.1f, 1f);

                    utility = (distanceScore * 0.4f) + (resourceScore * 0.6f);
                }
                else
                {
                    utility = Random.Range(0.1f, 0.5f);
                }

                if (utility > highestUtility)
                {
                    highestUtility = utility;
                    bestNode = node;
                }
            }
        }

        return bestNode;
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnPlayerChanged -= HandlePlayerTurn;
        }
    }
}