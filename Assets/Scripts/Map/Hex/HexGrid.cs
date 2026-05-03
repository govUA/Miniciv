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
    
    public  static HexGrid Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    public void InitializeGrid(MapGenerator generator)
    {
        mapGenerator = generator;
        wrapWorld = mapGenerator.wrapWorld;
        unitManager = GetComponent<UnitManager>();
        int[,] mapData = mapGenerator.GetMap();
        width = mapData.GetLength(0);
        height = mapData.GetLength(1);
        nodes = new HexNode[width, height];

        float mountainNoiseOffset = UnityEngine.Random.Range(0f, 100000f);
        float forestNoiseOffset = UnityEngine.Random.Range(0f, 100000f);
        float smallForestNoiseOffset = UnityEngine.Random.Range(0f, 100000f);

        float mountainNoiseScale = 0.25f;
        float forestNoiseScale = 0.15f;
        float smallForestNoiseScale = 0.45f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isLand = mapData[x, y] == 1;

                float mountainNoise = Mathf.PerlinNoise((x + mountainNoiseOffset) * mountainNoiseScale,
                    (y + mountainNoiseOffset) * mountainNoiseScale);
                float forestNoise = Mathf.PerlinNoise((x + forestNoiseOffset) * forestNoiseScale,
                    (y + forestNoiseOffset) * forestNoiseScale);
                float smallForestNoise = Mathf.PerlinNoise((x + smallForestNoiseOffset) * smallForestNoiseScale,
                    (y + smallForestNoiseOffset) * smallForestNoiseScale);

                nodes[x, y] = new HexNode(x, y, isLand, mountainNoise, forestNoise, smallForestNoise);
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

    public int GetDistance(HexNode nodeA, HexNode nodeB)
    {
        int q1 = nodeA.x;
        int r1 = nodeA.y - (nodeA.x - (nodeA.x & 1)) / 2;
        int s1 = -q1 - r1;

        int q2 = nodeB.x;
        int r2 = nodeB.y - (nodeB.x - (nodeB.x & 1)) / 2;
        int s2 = -q2 - r2;

        int distNormal = Mathf.Max(Mathf.Abs(q1 - q2), Mathf.Abs(r1 - r2), Mathf.Abs(s1 - s2));

        if (wrapWorld)
        {
            int dx = nodeA.x - nodeB.x;
            if (Mathf.Abs(dx) > width / 2)
            {
                int wrappedX = nodeB.x + (dx > 0 ? width : -width);
                int r3 = nodeB.y - (wrappedX - (wrappedX & 1)) / 2;
                int s3 = -wrappedX - r3;

                int distWrapped = Mathf.Max(Mathf.Abs(q1 - wrappedX), Mathf.Abs(r1 - r3), Mathf.Abs(s1 - s3));
                return Mathf.Min(distNormal, distWrapped);
            }
        }

        return distNormal;
    }
}