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

    private HexGrid hexGrid;
    private Pathfinder pathfinder;

    private Vector3Int previousHoverPosition = new Vector3Int(-9999, -9999, -9999);
    private HexNode selectedNode;
    private Unit selectedUnit;
    private List<HexNode> currentPath;

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

            if (unitManager != null && turnManager != null)
            {
                Unit clickedUnit = unitManager.GetUnitAtNode(clickedNode);
                if (clickedUnit != null && clickedUnit.ownerID == turnManager.CurrentPlayerID)
                {
                    selectedUnit = clickedUnit;
                    Debug.Log("Friendly unit selected. MP: " + selectedUnit.currentMP + "/" + selectedUnit.maxMP);
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
                clickedNode.GetVision(turnManager.CurrentPlayerID) != VisionState.Unexplored)
            {
                Debug.Log("Cannot move to an occupied tile.");
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

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (selectedNode != null && unitManager != null && turnManager != null &&
                unitManager.GetUnitAtNode(selectedNode) == null)
            {
                unitManager.SpawnUnit(selectedNode, turnManager.CurrentPlayerID);
                Debug.Log("Spawned unit for Player " + turnManager.CurrentPlayerID + " at: [" + selectedNode.x + ", " +
                          selectedNode.y + "]");
            }
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (turnManager != null)
            {
                selectedUnit = null;
                selectedNode = null;
                currentPath = null;
                DrawOverlays(new Vector3Int(-9999, -9999, -9999));

                turnManager.EndTurn();
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