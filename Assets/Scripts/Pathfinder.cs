using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HexGrid))]
public class Pathfinder : MonoBehaviour
{
    private HexGrid grid;
    private PlayerManager playerManager;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
        playerManager = Object.FindAnyObjectByType<PlayerManager>();
    }

    public List<HexNode> FindPath(HexNode startNode, HexNode targetNode, int playerId)
    {
        List<HexNode> openSet = new List<HexNode>();
        HashSet<HexNode> closedSet = new HashSet<HexNode>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            HexNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost ||
                    openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (HexNode neighbor in grid.GetNeighbors(currentNode))
            {
                bool isWalkable = neighbor.isLand;
                int perceivedCost = (int)neighbor.movementCost;

                if (neighbor.GetVision(playerId) == VisionState.Unexplored)
                {
                    isWalkable = true;
                    perceivedCost = 10;
                }
                else
                {
                    if (grid.IsNodeOccupied(neighbor) && neighbor.GetVision(playerId) == VisionState.Visible)
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

                if (!isWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.gCost + perceivedCost;
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private List<HexNode> RetracePath(HexNode startNode, HexNode endNode)
    {
        List<HexNode> path = new List<HexNode>();
        HexNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private int GetDistance(HexNode nodeA, HexNode nodeB)
    {
        int dx = Mathf.Abs(nodeA.x - nodeB.x);

        if (grid.wrapWorld && dx > grid.GetWidth() / 2)
        {
            dx = grid.GetWidth() - dx;
        }

        int dy = Mathf.Abs(nodeA.y - nodeB.y);

        return Mathf.Max(dx, dy) * 10;
    }
}