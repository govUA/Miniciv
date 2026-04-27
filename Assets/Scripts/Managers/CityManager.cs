using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(HexGrid))]
public class CityManager : MonoBehaviour
{
    public GameObject cityPrefab;

    public FogOfWarManager fowManager;
    public TurnManager turnManager;
    public UnitManager unitManager;
    public PlayerManager playerManager;

    private BorderManager borderManager;

    private HexGrid grid;
    private List<City> activeCities = new List<City>();

    void Awake()
    {
        grid = GetComponent<HexGrid>();

        borderManager = FindAnyObjectByType<BorderManager>();
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
                if (neighbor.ownerID == -1)
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


            if (fowManager != null)
            {
                fowManager.UpdateVisionDisplay(city.ownerID);
            }

            if (borderManager != null) borderManager.UpdateBorders();
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

        if (borderManager != null) borderManager.UpdateBorders();

        return true;
    }

    private void ClaimInitialTerritory(City city)
    {
        List<HexNode> initial = grid.GetNodesInRange(city.centerNode, 1);
        foreach (HexNode n in initial)
        {
            if (n.ownerID == -1)
            {
                n.ownerID = city.ownerID;
                city.territoryNodes.Add(n);
            }
        }

        if (fowManager != null) fowManager.UpdateVisionDisplay(city.ownerID);
    }

    public List<City> GetActiveCities() => activeCities;

    public void UpdateCityOwnership(City city)
    {
        foreach (HexNode n in city.territoryNodes)
        {
            n.ownerID = city.ownerID;
        }

        if (fowManager != null) fowManager.UpdateVisionDisplay(city.ownerID);

        if (borderManager != null) borderManager.UpdateBorders();
    }
}