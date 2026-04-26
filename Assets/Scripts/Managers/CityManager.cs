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
    private HexGrid grid;
    private List<City> activeCities = new List<City>();

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }

    public bool FoundCity(Unit settler)
    {
        if (!settler.isSettler) return false;

        HexNode node = settler.CurrentNode;

        if (node.ownerID != -1)
        {
            Debug.Log("Cannot found city. Territory already owned.");
            return false;
        }

        Vector3 spawnPos = transform.position;
        if (settler != null) spawnPos = settler.transform.position;

        GameObject cityObj = Instantiate(cityPrefab, spawnPos, Quaternion.identity);
        City newCity = cityObj.GetComponent<City>();

        string generatedName = "City " + (activeCities.Count + 1);
        newCity.Initialize(node, settler.ownerID, generatedName);

        node.hasCity = true;
        activeCities.Add(newCity);
        ClaimTerritory(newCity);

        Debug.Log("City founded: " + generatedName + " by Player " + settler.ownerID);
        return true;
    }

    private void ClaimTerritory(City city)
    {
        List<HexNode> territory = grid.GetNodesInRange(city.centerNode, city.territoryRange);

        foreach (HexNode n in territory)
        {
            if (n.isLand && n.ownerID == -1)
            {
                n.ownerID = city.ownerID;
            }
        }

        if (fowManager != null)
        {
            fowManager.UpdateVisionDisplay(city.ownerID);
        }
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
                        Vector3Int pos = new Vector3Int(y, tileX, 0);
                        territoryTilemap.SetTile(pos, tile);
                    }
                }
            }
        }
    }

    public List<City> GetActiveCities()
    {
        return activeCities;
    }
}