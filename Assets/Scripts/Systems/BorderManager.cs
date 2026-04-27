using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BorderManager : MonoBehaviour
{
    public HexGrid hexGrid;
    public Tilemap tilemap;
    public GameObject borderSegmentPrefab;

    [Header("Settings")] [Tooltip("Hex radius. For standard Unity Tilemap, it is usually 0.577")]
    public float hexRadius = 0.427f;

    [Tooltip("Merge tolerance distance. Increase if 'rings' appear")]
    public float mergeTolerance = 0.15f;

    [Tooltip("Check this if your hex is Flat-Top")]
    public bool isFlatTop = true;

    private List<GameObject> activeBorders = new List<GameObject>();

    struct Edge : System.IEquatable<Edge>
    {
        public int v1;
        public int v2;

        public Edge(int a, int b)
        {
            if (a < b)
            {
                v1 = a;
                v2 = b;
            }
            else
            {
                v1 = b;
                v2 = a;
            }
        }

        public bool Equals(Edge other)
        {
            return v1 == other.v1 && v2 == other.v2;
        }

        public override int GetHashCode()
        {
            return v1.GetHashCode() ^ (v2.GetHashCode() << 2);
        }
    }

    public void UpdateBorders()
    {
        ClearBorders();

        PlayerManager pm = FindAnyObjectByType<PlayerManager>();
        if (pm == null) return;

        Dictionary<int, List<HexNode>> playerTerritories = new Dictionary<int, List<HexNode>>();

        int width = hexGrid.GetWidth();
        int height = hexGrid.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                HexNode node = hexGrid.GetNode(x, y);
                if (node != null && node.ownerID != -1)
                {
                    if (!playerTerritories.ContainsKey(node.ownerID))
                        playerTerritories[node.ownerID] = new List<HexNode>();

                    playerTerritories[node.ownerID].Add(node);
                }
            }
        }

        foreach (var kvp in playerTerritories)
        {
            PlayerData player = pm.GetPlayer(kvp.Key);
            if (player != null)
            {
                DrawContinuousBorders(kvp.Value, player);
            }
        }
    }

    private void DrawContinuousBorders(List<HexNode> nodes, PlayerData player)
    {
        List<Vector3> uniqueVertices = new List<Vector3>();
        Dictionary<Edge, int> edgeCounts = new Dictionary<Edge, int>();

        float angleOffset = isFlatTop ? 0f : -30f;

        foreach (HexNode node in nodes)
        {
            Vector3Int cellPos = new Vector3Int(node.y, node.x, 0);
            Vector3 center = tilemap.CellToWorld(cellPos);

            int[] cornerIds = new int[6];
            for (int i = 0; i < 6; i++)
            {
                Vector3 cornerPos = center + GetCornerOffset(60 * i + angleOffset);
                cornerIds[i] = GetOrAddVertex(cornerPos, uniqueVertices);
            }

            for (int i = 0; i < 6; i++)
            {
                Edge edge = new Edge(cornerIds[i], cornerIds[(i + 1) % 6]);

                if (edgeCounts.ContainsKey(edge))
                    edgeCounts[edge]++;
                else
                    edgeCounts[edge] = 1;
            }
        }

        List<Edge> outerEdges = new List<Edge>();
        foreach (var kvp in edgeCounts)
        {
            if (kvp.Value == 1) outerEdges.Add(kvp.Key);
        }

        List<List<int>> loops = StitchEdges(outerEdges);

        foreach (List<int> loop in loops)
        {
            List<Vector3> points = new List<Vector3>();
            foreach (int vid in loop) points.Add(uniqueVertices[vid]);
            CreateBorderLoop(points, player);
        }
    }

    private int GetOrAddVertex(Vector3 pos, List<Vector3> uniqueVertices)
    {
        for (int i = 0; i < uniqueVertices.Count; i++)
        {
            if (Vector3.Distance(pos, uniqueVertices[i]) < mergeTolerance)
                return i;
        }

        uniqueVertices.Add(pos);
        return uniqueVertices.Count - 1;
    }

    private List<List<int>> StitchEdges(List<Edge> edges)
    {
        List<List<int>> loops = new List<List<int>>();
        List<Edge> remaining = new List<Edge>(edges);

        while (remaining.Count > 0)
        {
            List<int> currentLoop = new List<int>();

            Edge startEdge = remaining[0];
            remaining.RemoveAt(0);

            currentLoop.Add(startEdge.v1);
            currentLoop.Add(startEdge.v2);
            int currentPoint = startEdge.v2;

            bool foundNext = true;
            while (foundNext)
            {
                foundNext = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    Edge candidate = remaining[i];

                    if (candidate.v1 == currentPoint)
                    {
                        currentPoint = candidate.v2;
                        currentLoop.Add(currentPoint);
                        remaining.RemoveAt(i);
                        foundNext = true;
                        break;
                    }
                    else if (candidate.v2 == currentPoint)
                    {
                        currentPoint = candidate.v1;
                        currentLoop.Add(currentPoint);
                        remaining.RemoveAt(i);
                        foundNext = true;
                        break;
                    }
                }
            }

            loops.Add(currentLoop);
        }

        return loops;
    }

    private void CreateBorderLoop(List<Vector3> points, PlayerData player)
    {
        GameObject segment = Instantiate(borderSegmentPrefab, transform);
        LineRenderer lr = segment.GetComponent<LineRenderer>();

        if (points.Count > 2 && Vector3.Distance(points[0], points[points.Count - 1]) < mergeTolerance)
        {
            lr.loop = true;
            points.RemoveAt(points.Count - 1);
        }

        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());

        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;

        Material mat = new Material(lr.material);
        mat.SetColor("_PrimaryColor", player.primaryColor);
        mat.SetColor("_SecondaryColor", player.secondaryColor);
        lr.material = mat;

        activeBorders.Add(segment);
    }

    private Vector3 GetCornerOffset(float angleDegree)
    {
        float rad = angleDegree * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * hexRadius, Mathf.Sin(rad) * hexRadius, 0);
    }

    public void ClearBorders()
    {
        foreach (var border in activeBorders)
        {
            if (border != null) Destroy(border);
        }

        activeBorders.Clear();
    }
}