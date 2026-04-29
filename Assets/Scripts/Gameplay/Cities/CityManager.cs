using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class BuildingDataModel
{
    public string name;
    public int cost;
    public string requiredTech;
    public List<BuildingEffect> effects = new List<BuildingEffect>();
}

[System.Serializable]
public class BuildingDatabase
{
    public List<BuildingDataModel> buildings;
}

[System.Serializable]
public class BuildingEffect
{
    public string type;
    public int amount;
}

[RequireComponent(typeof(HexGrid))]
public class CityManager : MonoBehaviour
{
    [Header("Data")] public TextAsset buildingsJsonFile;
    public Dictionary<string, BuildingDataModel> buildingDatabaseDict = new Dictionary<string, BuildingDataModel>();

    [Header("Settings")] public GameObject cityPrefab;

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
        LoadBuildingDatabase();
    }

    private void LoadBuildingDatabase()
    {
        if (buildingsJsonFile == null)
        {
            Debug.LogError("[CityManager] JSON file with buildings not found!");
            return;
        }

        BuildingDatabase db = JsonUtility.FromJson<BuildingDatabase>(buildingsJsonFile.text);
        if (db != null && db.buildings != null)
        {
            foreach (var b in db.buildings)
            {
                buildingDatabaseDict[b.name] = b;
            }

            Debug.Log($"[CityManager] Loaded {buildingDatabaseDict.Count} basic buildings.");
        }
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

        foreach (City city in activeCities)
        {
            if (grid.GetDistance(node, city.centerNode) <= 3)
            {
                Debug.Log(
                    $"[CITY] Impossible to found a city: too close to {city.cityName}. At least 3 free tiles are needed.");
                return false;
            }
        }

        Vector3 spawnPos = settler.transform.position;
        GameObject cityObj = Instantiate(cityPrefab, spawnPos, Quaternion.identity);
        City newCity = cityObj.GetComponent<City>();

        string newCityName = "City " + (activeCities.Count + 1);
        PlayerData pd = playerManager.GetPlayer(settler.ownerID);

        if (pd != null && pd.civilization != null && pd.civilization.cityNames != null &&
            pd.civilization.cityNames.Count > 0)
        {
            int playerCityCount = 0;
            foreach (City c in activeCities)
            {
                if (c.ownerID == settler.ownerID) playerCityCount++;
            }

            if (playerCityCount < pd.civilization.cityNames.Count)
            {
                newCityName = pd.civilization.cityNames[playerCityCount];
            }
            else
            {
                newCityName = pd.civilization.cityNames[0] + " " + (playerCityCount + 1);
            }
        }

        newCity.Initialize(node, settler.ownerID, newCityName, unitManager, this, playerManager);

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