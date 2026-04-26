using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(HexGrid))]
[RequireComponent(typeof(Pathfinder))]
public class HexInteraction : MonoBehaviour
{
    public Tilemap highlightTilemap;
    public Tilemap mainTilemap;
    public TileBase highlightTile;
    public TileBase pathTile;

    public UnitManager unitManager;
    public TurnManager turnManager;
    public CityManager cityManager;
    public PlayerManager playerManager;

    private HexGrid hexGrid;
    private Pathfinder pathfinder;

    private Vector3Int previousHoverPosition = new Vector3Int(-9999, -9999, -9999);
    private HexNode selectedNode;
    private Unit selectedUnit;
    private List<HexNode> currentPath;

    private City selectedCity;

    void Awake()
    {
        hexGrid = GetComponent<HexGrid>();
        pathfinder = GetComponent<Pathfinder>();
    }

    void Update()
    {
        if (highlightTilemap == null || mainTilemap == null) return;

        HandleHover();
        HandleClick();
        HandleHotkeys();
    }

    void HandleHover()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int cellPosition = mainTilemap.WorldToCell(mousePosition);

        if (cellPosition != previousHoverPosition)
        {
            DrawOverlays(cellPosition);
            previousHoverPosition = cellPosition;
        }
    }

    void HandleClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3Int cellPosition = mainTilemap.WorldToCell(mousePosition);
            HexNode clickedNode = hexGrid.GetNode(cellPosition.y, cellPosition.x);

            if (clickedNode == null) return;

            selectedNode = clickedNode;
            selectedCity = null;

            if (cityManager != null)
            {
                foreach (City city in cityManager.GetActiveCities())
                {
                    if (city.centerNode == clickedNode && city.ownerID == turnManager.CurrentPlayerID)
                    {
                        selectedCity = city;
                        string projStr = selectedCity.currentProject != null
                            ? selectedCity.currentProject.name
                            : "None";
                        int reqFood = selectedCity.population * 10 + 10;

                        string sciStr = "";
                        if (playerManager != null)
                        {
                            PlayerData p = playerManager.GetPlayer(turnManager.CurrentPlayerID);
                            if (p != null)
                                sciStr = $" | Global Sci: {p.accumulatedResearch} (Target: {p.currentResearch})";
                        }

                        Debug.Log(
                            $"City: {selectedCity.cityName} | Food: {selectedCity.storedFood}/{reqFood} | Prod: {selectedCity.storedProduction} | Project: {projStr}{sciStr}");
                        break;
                    }
                }
            }

            if (unitManager != null && turnManager != null)
            {
                Unit clickedUnit = unitManager.GetUnitAtNode(clickedNode);
                if (clickedUnit != null && clickedUnit.ownerID == turnManager.CurrentPlayerID)
                {
                    selectedUnit = clickedUnit;
                }
                else
                {
                    selectedUnit = null;
                }
            }

            currentPath = null;
            DrawOverlays(cellPosition);
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame && selectedUnit != null && !selectedUnit.IsAnimating())
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3Int cellPosition = mainTilemap.WorldToCell(mousePosition);
            HexNode clickedNode = hexGrid.GetNode(cellPosition.y, cellPosition.x);

            if (clickedNode == null) return;

            if (!clickedNode.isLand &&
                clickedNode.GetVision(turnManager.CurrentPlayerID) != VisionState.Unexplored) return;

            if (hexGrid.IsNodeOccupied(clickedNode) &&
                clickedNode.GetVision(turnManager.CurrentPlayerID) == VisionState.Visible)
            {
                return;
            }

            int remOwner = clickedNode.GetRememberedOwner(turnManager.CurrentPlayerID);
            if (remOwner != -1 && remOwner != turnManager.CurrentPlayerID)
            {
                return;
            }

            currentPath = pathfinder.FindPath(selectedUnit.CurrentNode, clickedNode, turnManager.CurrentPlayerID);
            DrawOverlays(cellPosition);

            if (currentPath != null && currentPath.Count > 0)
            {
                selectedUnit.SetPath(currentPath);
            }
        }
    }

    private void HandleHotkeys()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (turnManager != null)
            {
                selectedUnit = null;
                selectedNode = null;
                selectedCity = null;
                currentPath = null;
                DrawOverlays(new Vector3Int(-9999, -9999, -9999));
                turnManager.EndTurn();
            }
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (selectedUnit != null && selectedUnit.isSettler && !selectedUnit.IsAnimating() && cityManager != null)
            {
                bool success = cityManager.FoundCity(selectedUnit);
                if (success)
                {
                    unitManager.RemoveUnit(selectedUnit);
                    selectedUnit = null;
                    selectedNode = null;
                    currentPath = null;
                    DrawOverlays(new Vector3Int(-9999, -9999, -9999));
                }
            }
        }

        if (playerManager != null && turnManager != null)
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                playerManager.SetResearch(turnManager.CurrentPlayerID, "Pottery");
            }

            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                playerManager.SetResearch(turnManager.CurrentPlayerID, "Bronze Working");
            }
        }

        if (selectedCity != null && selectedCity.ownerID == turnManager.CurrentPlayerID)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                selectedCity.SetProject(new CityProject("Scout", ProjectType.Unit, 20));
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                selectedCity.SetProject(new CityProject("Settler", ProjectType.Unit, 50));
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                selectedCity.SetProject(new CityProject("Monument", ProjectType.Building, 40));
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                selectedCity.SetProject(new CityProject("Granary", ProjectType.Building, 30, "Pottery"));
            }

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                selectedCity.SetProject(new CityProject("Warrior", ProjectType.Unit, 40, "Bronze Working"));
            }
        }
    }

    private void DrawOverlays(Vector3Int hoverPos)
    {
        highlightTilemap.ClearAllTiles();

        if (hexGrid.GetNode(hoverPos.y, hoverPos.x) != null)
        {
            highlightTilemap.SetTile(hoverPos, highlightTile);
        }

        if (selectedNode != null)
        {
            highlightTilemap.SetTile(new Vector3Int(selectedNode.y, selectedNode.x, 0), highlightTile);
        }

        if (currentPath != null)
        {
            foreach (HexNode node in currentPath)
            {
                highlightTilemap.SetTile(new Vector3Int(node.y, node.x, 0), pathTile);
            }
        }
    }
}