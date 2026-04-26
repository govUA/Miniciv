using UnityEngine;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour
{
    private MapGenerator mapGenerator;
    public UnitManager unitManager;

    private HexNode[,] nodes;
    private int width;
    private int height;
    public bool wrapWorld { get; private set; }

    private static readonly Vector2Int[] evenNeighbors =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 0), new Vector2Int(1, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, -1)
    };

    private static readonly Vector2Int[] oddNeighbors =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, 0),
        new Vector2Int(-1, 1), new Vector2Int(-1, 0)
    };

    public void InitializeGrid(MapGenerator generator)
    {
        mapGenerator = generator;
        wrapWorld = mapGenerator.wrapWorld;

        unitManager = GetComponent<UnitManager>();

        int[,] mapData = mapGenerator.GetMap();
        width = mapData.GetLength(0);
        height = mapData.GetLength(1);

        nodes = new HexNode[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isLand = mapData[x, y] == 1;
                nodes[x, y] = new HexNode(x, y, isLand);
            }
        }
    }

    public HexNode GetNode(int x, int y)
    {
        if (nodes == null) return null;

        if (wrapWorld)
        {
            x = (x % width + width) % width;
        }
        else if (x < 0 || x >= width)
        {
            return null;
        }

        if (y < 0 || y >= height)
        {
            return null;
        }

        return nodes[x, y];
    }

    public List<HexNode> GetNeighbors(HexNode node)
    {
        List<HexNode> neighbors = new List<HexNode>();
        Vector2Int[] offsets = (node.x % 2 == 0) ? evenNeighbors : oddNeighbors;

        foreach (Vector2Int offset in offsets)
        {
            int checkX = node.x + offset.x;
            int checkY = node.y + offset.y;

            if (wrapWorld)
            {
                checkX = (checkX % width + width) % width;
            }

            if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
            {
                neighbors.Add(nodes[checkX, checkY]);
            }
        }

        return neighbors;
    }

    public List<HexNode> GetNodesInRange(HexNode center, int range)
    {
        List<HexNode> inRange = new List<HexNode>();
        HashSet<HexNode> visited = new HashSet<HexNode>();
        Queue<HexNode> queue = new Queue<HexNode>();
        Dictionary<HexNode, int> distances = new Dictionary<HexNode, int>();

        queue.Enqueue(center);
        visited.Add(center);
        distances[center] = 0;

        while (queue.Count > 0)
        {
            HexNode current = queue.Dequeue();
            inRange.Add(current);

            int currentDist = distances[current];
            if (currentDist < range)
            {
                foreach (HexNode neighbor in GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        distances[neighbor] = currentDist + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return inRange;
    }

    public bool IsNodeOccupied(HexNode node)
    {
        if (unitManager != null)
        {
            return unitManager.GetUnitAtNode(node) != null;
        }

        return false;
    }

    public int GetWidth() => width;
    public int GetHeight() => height;
}