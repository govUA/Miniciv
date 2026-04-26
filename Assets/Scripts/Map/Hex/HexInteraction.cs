using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HexGrid))]
public class HexInteraction : MonoBehaviour
{
    public Tilemap highlightTilemap;
    public Tilemap mainTilemap;
    public TileBase highlightTile;

    private HexGrid hexGrid;
    private Vector3Int previousHoverPosition = new Vector3Int(-9999, -9999, -9999);
    private HexNode selectedNode;

    void Awake()
    {
        hexGrid = GetComponent<HexGrid>();
    }

    void Update()
    {
        if (highlightTilemap == null || mainTilemap == null || highlightTile == null) return;

        HandleHover();
        HandleClick();
    }

    void HandleHover()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int cellPosition = mainTilemap.WorldToCell(mousePosition);

        if (cellPosition != previousHoverPosition)
        {
            highlightTilemap.ClearAllTiles();

            HexNode hoveredNode = hexGrid.GetNode(cellPosition.y, cellPosition.x);
            if (hoveredNode != null)
            {
                highlightTilemap.SetTile(cellPosition, highlightTile);
            }

            if (selectedNode != null)
            {
                highlightTilemap.SetTile(new Vector3Int(selectedNode.y, selectedNode.x, 0), highlightTile);
            }

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

            if (clickedNode != null)
            {
                selectedNode = clickedNode;
                Debug.Log("Hex Selected: [" + selectedNode.x + ", " + selectedNode.y + "] Land: " +
                          selectedNode.isLand);

                highlightTilemap.ClearAllTiles();
                highlightTilemap.SetTile(new Vector3Int(selectedNode.y, selectedNode.x, 0), highlightTile);
            }
            else
            {
                selectedNode = null;
                highlightTilemap.ClearAllTiles();
            }
        }
    }
}