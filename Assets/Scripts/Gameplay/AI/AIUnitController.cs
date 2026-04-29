using UnityEngine;
using System.Collections.Generic;

public class AIUnitController : MonoBehaviour
{
    public UnitManager unitManager;
    public CityManager cityManager;

    public void ExecuteUnitActions(int playerId)
    {
        if (unitManager == null) unitManager = FindObjectOfType<UnitManager>();
        if (cityManager == null) cityManager = FindObjectOfType<CityManager>();

        TechManager techManager = FindObjectOfType<TechManager>();
        bool canSail = techManager != null && techManager.HasTech(playerId, "Sailing");

        List<Unit> allUnits = new List<Unit>(unitManager.GetActiveUnits());
        Pathfinder pathfinder = FindObjectOfType<Pathfinder>();
        HexGrid grid = FindObjectOfType<HexGrid>();

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;

            if (unit.ownerID != playerId || unit.State != UnitState.Idle || unit.currentMP <= 0) continue;

            HexNode targetNode = EvaluateBestDestination(unit, grid, canSail);

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
                    else
                    {
                        unit.isFortified = true;
                        unit.currentMP = 0;
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

    private HexNode EvaluateBestDestination(Unit unit, HexGrid grid, bool canSail)
    {
        HexNode bestNode = null;
        float highestUtility = -1000f;

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                HexNode node = grid.GetNode(x, y);

                if (node == null) continue;

                if (!node.isLand)
                {
                    if (unit.isSettler || !canSail) continue;
                }

                bool isOccupiedBySameType = false;
                if (unitManager != null)
                {
                    foreach (Unit u in unitManager.GetUnitsAtNode(node))
                    {
                        if (u != unit && u.ownerID == unit.ownerID &&
                            (u.unitClass == UnitClass.Civilian) == (unit.unitClass == UnitClass.Civilian))
                        {
                            isOccupiedBySameType = true;
                            break;
                        }
                    }
                }

                if (isOccupiedBySameType) continue;

                float utility = 0f;

                if (unit.isSettler)
                {
                    utility = EvaluateSettlerTile(unit, node, grid);
                }
                else
                {
                    utility = EvaluateScoutTile(unit, node, grid);
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

    private float EvaluateScoutTile(Unit unit, HexNode candidateNode, HexGrid grid)
    {
        float score = 0f;
        VisionState vision = candidateNode.GetVision(unit.ownerID);

        if (vision == VisionState.Unexplored)
        {
            score += 500f;
        }
        else if (vision == VisionState.Explored)
        {
            score += 50f;
        }

        if (cityManager != null)
        {
            float minDistToCity = 999f;
            foreach (City c in cityManager.GetActiveCities())
            {
                if (c.ownerID == unit.ownerID)
                {
                    float d = grid.GetDistance(candidateNode, c.centerNode);
                    if (d < minDistToCity) minDistToCity = d;
                }
            }

            if (minDistToCity == 0) score += 200f;
            else if (minDistToCity <= 2) score += 150f;
            else score += (50f / (minDistToCity + 1f));
        }

        int distance = grid.GetDistance(unit.CurrentNode, candidateNode);
        float distanceScore = 100f / (distance + 1f);
        float randomBonus = Random.Range(0f, 10f);

        return score + distanceScore + randomBonus;
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
}