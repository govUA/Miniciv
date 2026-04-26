using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HexGrid))]
public class GameManager : MonoBehaviour
{
    public UnitManager unitManager;
    public TurnManager turnManager;
    public FogOfWarManager fowManager;

    public void StartGame()
    {
        HexGrid grid = GetComponent<HexGrid>();
        int numPlayers = turnManager != null ? turnManager.TotalPlayers : 2;
        List<HexNode> startNodes = new List<HexNode>();

        for (int i = 0; i < numPlayers; i++)
        {
            HexNode startNode = GetValidStartNode(grid, startNodes);
            if (startNode != null)
            {
                startNodes.Add(startNode);
                unitManager.SpawnUnit(startNode, i);
            }
            else
            {
                Debug.LogWarning("Could not find a valid start node for Player " + i);
            }
        }

        if (startNodes.Count > 0 && unitManager.mainTilemap != null)
        {
            HexNode p0Start = startNodes[0];
            Vector3 p0Pos = unitManager.mainTilemap.CellToWorld(new Vector3Int(p0Start.y, p0Start.x, 0));
            Camera.main.transform.position = new Vector3(p0Pos.x, p0Pos.y, Camera.main.transform.position.z);
        }

        if (fowManager != null)
        {
            fowManager.InitializeFOW();
        }
    }

    private HexNode GetValidStartNode(HexGrid grid, List<HexNode> existingStarts)
    {
        int attempts = 0;
        while (attempts < 2000)
        {
            int rx = UnityEngine.Random.Range(0, grid.GetWidth());
            int ry = UnityEngine.Random.Range(0, grid.GetHeight());
            HexNode candidate = grid.GetNode(rx, ry);

            if (candidate != null && candidate.isLand)
            {
                bool tooClose = false;
                foreach (HexNode existing in existingStarts)
                {
                    int dx = Mathf.Abs(candidate.x - existing.x);
                    if (grid.wrapWorld && dx > grid.GetWidth() / 2) dx = grid.GetWidth() - dx;
                    int dy = Mathf.Abs(candidate.y - existing.y);

                    int dist = Mathf.Max(dx, dy);
                    if (dist < 15)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    return candidate;
                }
            }

            attempts++;
        }

        return null;
    }
}