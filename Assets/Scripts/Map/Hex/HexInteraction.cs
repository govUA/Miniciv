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

    private HexGrid hexGrid;
    private Pathfinder pathfinder;

    private Vector3Int previousHoverPosition = new Vector3Int(-9999, -9999, -9999);
    private HexNode selectedNode;
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
            currentPath = null;
            DrawOverlays(cellPosition);
            Debug.Log("Selected Start Node: [" + selectedNode.x + ", " + selectedNode.y + "]");
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame && selectedNode != null)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3Int cellPosition = mainTilemap.WorldToCell(mousePosition);
            HexNode clickedNode = hexGrid.GetNode(cellPosition.y, cellPosition.x);

            if (clickedNode == null) return;

            currentPath = pathfinder.FindPath(selectedNode, clickedNode);
            DrawOverlays(cellPosition);

            if (currentPath == null)
            {
                Debug.Log("No valid path found.");
            }
            else
            {
                Debug.Log("Path established. Length: " + currentPath.Count);
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