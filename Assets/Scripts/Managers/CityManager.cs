using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(HexGrid))]
public class CityManager : MonoBehaviour
{
    public GameObject cityPrefab;
    public Tilemap territoryTilemap;
    public TileBase p0TerritoryTile;
    public TileBase p1TerritoryTile;

    public FogOfWarManager fowManager;
    public TurnManager turnManager;
    public UnitManager unitManager;
    public PlayerManager playerManager;

    private HexGrid grid;
    private List<City> activeCities = new List<City>();

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }

    void OnEnable()
    {
        if (turnManager != null) turnManager.OnPlayerChanged += ProcessPlayerCities;
    }

    void OnDisable()
    {
        if (turnManager != null) turnManager.OnPlayerChanged -= ProcessPlayerCities;
    }

    private void ProcessPlayerCities(int playerId)
    {
        foreach (City city in activeCities)
        {
            if (city.ownerID == playerId)
            {
                city.ProcessTurn(grid);
            }
        }
    }

    public void ExpandTerritoryByOne(City city)
    {
        List<HexNode> candidates = new List<HexNode>();

        foreach (HexNode node in city.territoryNodes)
        {
            foreach (HexNode neighbor in grid.GetNeighbors(node))
            {
                if (neighbor.isLand && neighbor.ownerID == -1)
                {
                    if (!candidates.Contains(neighbor))
                        candidates.Add(neighbor);
                }
            }
        }

        if (candidates.Count > 0)
        {
            HexNode bestTile = candidates[0];
            float minDistance = float.MaxValue;

            foreach (HexNode c in candidates)
            {
                float d = Mathf.Sqrt(Mathf.Pow(c.x - city.centerNode.x, 2) + Mathf.Pow(c.y - city.centerNode.y, 2));
                if (d < minDistance)
                {
                    minDistance = d;
                    bestTile = c;
                }
            }

            bestTile.ownerID = city.ownerID;
            city.territoryNodes.Add(bestTile);

            DrawPlayerMemoryTerritory(turnManager.CurrentPlayerID);

            if (fowManager != null)
            {
                fowManager.UpdateVisionDisplay(city.ownerID);
            }
        }
    }

    public bool FoundCity(Unit settler)
    {
        if (!settler.isSettler) return false;
        HexNode node = settler.CurrentNode;
        if (node.ownerID != -1) return false;

        Vector3 spawnPos = settler.transform.position;
        GameObject cityObj = Instantiate(cityPrefab, spawnPos, Quaternion.identity);
        City newCity = cityObj.GetComponent<City>();

        newCity.Initialize(node, settler.ownerID, "City " + (activeCities.Count + 1), unitManager, this, playerManager);

        node.hasCity = true;
        activeCities.Add(newCity);

        ClaimInitialTerritory(newCity);
        return true;
    }

    private void ClaimInitialTerritory(City city)
    {
        List<HexNode> initial = grid.GetNodesInRange(city.centerNode, 1);
        foreach (HexNode n in initial)
        {
            if (n.isLand && n.ownerID == -1)
            {
                n.ownerID = city.ownerID;
                city.territoryNodes.Add(n);
            }
        }

        DrawPlayerMemoryTerritory(city.ownerID);
        if (fowManager != null) fowManager.UpdateVisionDisplay(city.ownerID);
    }

    public void DrawPlayerMemoryTerritory(int playerId)
    {
        territoryTilemap.ClearAllTiles();
        int startOffset = grid.wrapWorld ? -1 : 0;
        int endOffset = grid.wrapWorld ? 1 : 0;

        for (int offset = startOffset; offset <= endOffset; offset++)
        {
            for (int x = 0; x < grid.GetWidth(); x++)
            {
                for (int y = 0; y < grid.GetHeight(); y++)
                {
                    HexNode node = grid.GetNode(x, y);
                    int remOwner = node.GetRememberedOwner(playerId);
                    if (remOwner != -1)
                    {
                        TileBase tile = (remOwner == 0) ? p0TerritoryTile : p1TerritoryTile;
                        int tileX = x + (offset * grid.GetWidth());
                        territoryTilemap.SetTile(new Vector3Int(y, tileX, 0), tile);
                    }
                }
            }
        }
    }

    public List<City> GetActiveCities() => activeCities;

    public void UpdateCityOwnership(City city)
    {
        foreach (HexNode n in city.territoryNodes)
        {
            n.ownerID = city.ownerID;
        }

        if (turnManager != null) DrawPlayerMemoryTerritory(turnManager.CurrentPlayerID);
        if (fowManager != null) fowManager.UpdateVisionDisplay(city.ownerID);
    }
}