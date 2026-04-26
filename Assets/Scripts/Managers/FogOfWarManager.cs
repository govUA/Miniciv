using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(HexGrid))]
public class FogOfWarManager : MonoBehaviour
{
    public Tilemap fowTilemap;
    public TileBase unexploredTile;
    public TileBase exploredTile;

    public TurnManager turnManager;
    public UnitManager unitManager;
    public CityManager cityManager;

    private HexGrid grid;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }

    void OnEnable()
    {
        Unit.OnUnitMoved += HandleUnitMoved;
        if (turnManager != null)
        {
            turnManager.OnPlayerChanged += UpdateVisionDisplay;
        }
    }

    void OnDisable()
    {
        Unit.OnUnitMoved -= HandleUnitMoved;
        if (turnManager != null)
        {
            turnManager.OnPlayerChanged -= UpdateVisionDisplay;
        }
    }

    public void InitializeFOW()
    {
        int startingPlayer = turnManager != null ? turnManager.CurrentPlayerID : 0;
        UpdateVisionDisplay(startingPlayer);
    }

    private void HandleUnitMoved(Unit unit)
    {
        CalculateVision(unit.ownerID);
        if (turnManager == null || turnManager.CurrentPlayerID == unit.ownerID)
        {
            UpdateVisionDisplay(unit.ownerID);
        }
    }

    public void CalculateVision(int playerId)
    {
        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                HexNode node = grid.GetNode(x, y);
                if (node.GetVision(playerId) == VisionState.Visible)
                {
                    node.SetVision(playerId, VisionState.Explored);
                }
            }
        }

        foreach (Unit unit in unitManager.GetActiveUnits())
        {
            if (unit.ownerID == playerId)
            {
                List<HexNode> visibleNodes = grid.GetNodesInRange(unit.CurrentNode, unit.visionRange);
                foreach (HexNode node in visibleNodes)
                {
                    node.SetVision(playerId, VisionState.Visible);
                }
            }
        }

        if (cityManager != null)
        {
            foreach (City city in cityManager.GetActiveCities())
            {
                if (city.ownerID == playerId)
                {
                    List<HexNode> centerVisible = grid.GetNodesInRange(city.centerNode, city.visionRange);
                    foreach (HexNode node in centerVisible)
                    {
                        node.SetVision(playerId, VisionState.Visible);
                    }

                    foreach (HexNode ownedNode in city.territoryNodes)
                    {
                        ownedNode.SetVision(playerId, VisionState.Visible);
                        foreach (HexNode neighbor in grid.GetNeighbors(ownedNode))
                        {
                            neighbor.SetVision(playerId, VisionState.Visible);
                        }
                    }
                }
            }
        }

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                HexNode node = grid.GetNode(x, y);
                if (node.GetVision(playerId) == VisionState.Visible)
                {
                    node.SetRememberedOwner(playerId, node.ownerID);
                    node.SetRememberedCity(playerId, node.hasCity);
                }
            }
        }
    }

    public void UpdateVisionDisplay(int playerId)
    {
        CalculateVision(playerId);
        fowTilemap.ClearAllTiles();

        int startOffset = grid.wrapWorld ? -1 : 0;
        int endOffset = grid.wrapWorld ? 1 : 0;

        for (int offset = startOffset; offset <= endOffset; offset++)
        {
            for (int x = 0; x < grid.GetWidth(); x++)
            {
                for (int y = 0; y < grid.GetHeight(); y++)
                {
                    HexNode node = grid.GetNode(x, y);
                    VisionState state = node.GetVision(playerId);

                    int tileX = x + (offset * grid.GetWidth());
                    Vector3Int pos = new Vector3Int(y, tileX, 0);

                    if (state == VisionState.Unexplored)
                    {
                        fowTilemap.SetTile(pos, unexploredTile);
                    }
                    else if (state == VisionState.Explored)
                    {
                        fowTilemap.SetTile(pos, exploredTile);
                    }
                }
            }
        }

        if (cityManager != null)
        {
            cityManager.DrawPlayerMemoryTerritory(playerId);
        }

        if (unitManager != null)
        {
            foreach (Unit unit in unitManager.GetActiveUnits())
            {
                if (unit.ownerID == playerId)
                {
                    unit.SetVisibility(true);
                }
                else
                {
                    VisionState state = unit.CurrentNode.GetVision(playerId);
                    unit.SetVisibility(state == VisionState.Visible);
                }
            }
        }

        if (cityManager != null)
        {
            foreach (City city in cityManager.GetActiveCities())
            {
                if (city.ownerID == playerId)
                {
                    city.SetVisibility(true);
                }
                else
                {
                    VisionState state = city.centerNode.GetVision(playerId);
                    if (state == VisionState.Visible)
                    {
                        city.SetVisibility(true);
                    }
                    else if (state == VisionState.Explored && city.centerNode.GetRememberedCity(playerId))
                    {
                        city.SetVisibility(true);
                    }
                    else
                    {
                        city.SetVisibility(false);
                    }
                }
            }
        }
    }
}