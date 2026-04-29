using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIManager : MonoBehaviour
{
    public TurnManager turnManager;
    public PlayerManager playerManager;
    public UnitManager unitManager;
    public CityManager cityManager;

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
        yield return new WaitForSeconds(0.5f);

        ExecuteUnitActions(playerId);

        yield return new WaitForSeconds(0.5f);

        turnManager.EndTurn();
    }

    private void ExecuteUnitActions(int playerId)
    {
        List<Unit> allUnits = new List<Unit>(unitManager.GetActiveUnits());
        Pathfinder pathfinder = FindObjectOfType<Pathfinder>();
        HexGrid grid = FindObjectOfType<HexGrid>();

        if (cityManager == null) cityManager = FindObjectOfType<CityManager>();

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            if (unit.ownerID != playerId || unit.State != UnitState.Idle || unit.currentMP <= 0) continue;

            HexNode targetNode = EvaluateBestDestination(unit, grid);

            if (targetNode != null)
            {
                if (targetNode == unit.CurrentNode)
                {
                    if (unit.isSettler)
                    {
                        if (cityManager.FoundCity(unit))
                        {
                            unitManager.RemoveUnit(unit);
                        }
                    }
                }
                else
                {
                    List<HexNode> path = pathfinder.FindPath(unit, targetNode);

                    if (path != null && path.Count > 0)
                    {
                        unit.MoveAlongPath(path);
                    }
                }
            }
        }
    }

    private HexNode EvaluateBestDestination(Unit unit, HexGrid grid)
    {
        HexNode bestNode = null;
        float highestUtility = -1000f;

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                HexNode node = grid.GetNode(x, y);

                if (node == null || !node.isLand) continue;

                float utility = 0f;

                if (unit.isSettler)
                {
                    utility = EvaluateSettlerTile(unit, node, grid);
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

        return bestNode ?? unit.CurrentNode;
    }

    private float EvaluateSettlerTile(Unit settler, HexNode candidateNode, HexGrid grid)
    {
        if (cityManager != null)
        {
            foreach (City city in cityManager.GetActiveCities())
            {
                if (grid.GetDistance(candidateNode, city.centerNode) <= 3)
                {
                    return -1000f;
                }
            }
        }

        if (candidateNode.hasCity) return -1000f;

        List<HexNode> initialTerritory = grid.GetNodesInRange(candidateNode, 1);
        float yieldScore = 0f;

        foreach (HexNode n in initialTerritory)
        {
            yieldScore += (n.foodYield * 1.5f) + (n.prodYield * 1.2f) + (n.sciYield * 1.0f);
        }

        bool hasWaterNeighbor = false;
        foreach (HexNode neighbor in grid.GetNeighbors(candidateNode))
        {
            if (!neighbor.isLand) hasWaterNeighbor = true;
        }

        if (hasWaterNeighbor) yieldScore += 3f;

        int distance = grid.GetDistance(settler.CurrentNode, candidateNode);

        float distanceScore = 20f / (distance + 1f);

        return yieldScore + distanceScore;
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnPlayerChanged -= HandlePlayerTurn;
        }
    }
}