using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HexGrid))]
public class Pathfinder : MonoBehaviour
{
    private HexGrid grid;
    private PlayerManager playerManager;
    private TechManager techManager;
    private UnitManager unitManager;

    private class PathNodeRecord
    {
        public HexNode Node;
        public PathNodeRecord Parent;
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;

        public PathNodeRecord(HexNode node)
        {
            Node = node;
        }
    }

    void Awake()
    {
        grid = GetComponent<HexGrid>();
        playerManager = Object.FindAnyObjectByType<PlayerManager>();
        techManager = Object.FindAnyObjectByType<TechManager>();
        unitManager = Object.FindAnyObjectByType<UnitManager>();
    }

    public List<HexNode> FindPath(Unit movingUnit, HexNode targetNode)
    {
        int playerId = movingUnit.ownerID;
        HexNode startNode = movingUnit.CurrentNode;

        HashSet<HexNode> claimedFriendlyNodes = new HashSet<HexNode>();
        HashSet<HexNode> enemyNodes = new HashSet<HexNode>();

        if (unitManager != null)
        {
            foreach (Unit u in unitManager.GetActiveUnits())
            {
                if (u == movingUnit) continue;

                if (u.ownerID != playerId)
                {
                    enemyNodes.Add(u.CurrentNode);
                }
                else
                {
                    bool isMovingCiv = movingUnit.unitClass == UnitClass.Civilian;
                    bool isTargetCiv = u.unitClass == UnitClass.Civilian;
                    if (isMovingCiv == isTargetCiv)
                    {
                        claimedFriendlyNodes.Add(u.GetExpectedEndTurnNode());
                    }
                }
            }
        }

        Dictionary<HexNode, PathNodeRecord> nodeRecords = new Dictionary<HexNode, PathNodeRecord>();
        List<PathNodeRecord> openSet = new List<PathNodeRecord>();
        HashSet<HexNode> closedSet = new HashSet<HexNode>();

        PathNodeRecord startRecord = new PathNodeRecord(startNode) { gCost = 0 };
        startRecord.hCost = GetDistance(startNode, targetNode);

        nodeRecords[startNode] = startRecord;
        openSet.Add(startRecord);

        while (openSet.Count > 0)
        {
            PathNodeRecord currentRecord = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentRecord.fCost ||
                    openSet[i].fCost == currentRecord.fCost && openSet[i].hCost < currentRecord.hCost)
                {
                    currentRecord = openSet[i];
                }
            }

            openSet.Remove(currentRecord);
            closedSet.Add(currentRecord.Node);

            if (currentRecord.Node == targetNode)
            {
                return RetracePath(startRecord, currentRecord);
            }

            foreach (HexNode neighbor in grid.GetNeighbors(currentRecord.Node))
            {
                if (closedSet.Contains(neighbor)) continue;

                bool isWalkable = neighbor.isLand;
                int perceivedCost = (int)neighbor.movementCost;

                if (movingUnit.unitClass == UnitClass.Naval)
                {
                    isWalkable = !neighbor.isLand;
                    perceivedCost = 10;
                }
                else
                {
                    bool currentIsLand = currentRecord.Node.isLand;
                    bool neighborIsLand = neighbor.isLand;

                    if (!currentIsLand && !neighborIsLand)
                    {
                        if (techManager != null && techManager.HasTech(playerId, "Sailing"))
                        {
                            isWalkable = true;
                            perceivedCost = 10;
                        }
                        else
                        {
                            isWalkable = false;
                        }
                    }
                    else if (currentIsLand != neighborIsLand)
                    {
                        if (techManager != null && techManager.HasTech(playerId, "Sailing"))
                        {
                            isWalkable = true;
                            perceivedCost = 20;
                        }
                        else
                        {
                            isWalkable = false;
                        }
                    }
                }

                if (neighbor.GetVision(playerId) == VisionState.Unexplored)
                {
                    isWalkable = true;
                    perceivedCost = 10;
                }
                else
                {
                    bool hasEnemy = enemyNodes.Contains(neighbor);
                    bool hasSameTypeFriendly = claimedFriendlyNodes.Contains(neighbor);

                    if (hasEnemy && neighbor.GetVision(playerId) == VisionState.Visible)
                    {
                        isWalkable = false;
                    }
                    else if (hasSameTypeFriendly && neighbor == targetNode)
                    {
                        isWalkable = false;
                    }

                    int remOwner = neighbor.GetRememberedOwner(playerId);
                    if (remOwner != -1 && remOwner != playerId)
                    {
                        if (playerManager == null || !playerManager.IsAtWar(playerId, remOwner))
                        {
                            isWalkable = false;
                        }
                    }
                }

                if (!isWalkable) continue;

                int newMovementCostToNeighbor = currentRecord.gCost + perceivedCost;

                if (!nodeRecords.TryGetValue(neighbor, out PathNodeRecord neighborRecord))
                {
                    neighborRecord = new PathNodeRecord(neighbor);
                    nodeRecords[neighbor] = neighborRecord;
                }

                if (newMovementCostToNeighbor < neighborRecord.gCost || !openSet.Contains(neighborRecord))
                {
                    neighborRecord.gCost = newMovementCostToNeighbor;
                    neighborRecord.hCost = GetDistance(neighbor, targetNode);
                    neighborRecord.Parent = currentRecord;

                    if (!openSet.Contains(neighborRecord))
                        openSet.Add(neighborRecord);
                }
            }
        }

        return null;
    }

    private List<HexNode> RetracePath(PathNodeRecord startNode, PathNodeRecord endNode)
    {
        List<HexNode> path = new List<HexNode>();
        PathNodeRecord currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.Node);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    private int GetDistance(HexNode nodeA, HexNode nodeB)
    {
        return grid.GetDistance(nodeA, nodeB) * 10;
    }
}