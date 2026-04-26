using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(MapGenerator))]
public class MapDisplay : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase landTile;
    public TileBase waterTile;

    private MapGenerator mapGenerator;

    void Awake()
    {
        mapGenerator = GetComponent<MapGenerator>();
    }

    public void DrawMap()
    {
        int[,] map = mapGenerator.GetMap();

        if (map == null)
        {
            Debug.LogError("Map not found for rendering.");
            return;
        }

        int mapWidth = map.GetLength(0);
        int mapHeight = map.GetLength(1);

        tilemap.ClearAllTiles();

        int startOffset = mapGenerator.wrapWorld ? -1 : 0;
        int endOffset = mapGenerator.wrapWorld ? 1 : 0;
        int copiesCount = endOffset - startOffset + 1;

        int totalTiles = mapWidth * mapHeight * copiesCount;
        Vector3Int[] positions = new Vector3Int[totalTiles];
        TileBase[] tiles = new TileBase[totalTiles];

        int index = 0;

        for (int offset = startOffset; offset <= endOffset; offset++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    TileBase tileToPlace = (map[x, y] == 1) ? landTile : waterTile;
                    int tileX = x + (offset * mapWidth);

                    positions[index] = new Vector3Int(y, tileX, 0);
                    tiles[index] = tileToPlace;
                    index++;
                }
            }
        }

        tilemap.SetTiles(positions, tiles);

        HexGrid hexGrid = GetComponent<HexGrid>();
        if (hexGrid != null)
        {
            hexGrid.InitializeGrid(mapGenerator);
        }

        CameraController camController = Camera.main.GetComponent<CameraController>();
        if (camController != null)
        {
            camController.mapGenerator = mapGenerator;
            camController.tilemap = tilemap;
            camController.InitializeBounds();
            Camera.main.orthographicSize = Mathf.Clamp(mapHeight / 3f, camController.minZoom, camController.maxZoom);
        }
        else
        {
            Debug.LogWarning("CameraController not found on Main Camera.");
        }

        GameManager gameManager = GetComponent<GameManager>();
        if (gameManager != null)
        {
            gameManager.StartGame();
        }
        else
        {
            Vector3 centerPosition = tilemap.CellToWorld(new Vector3Int(mapHeight / 2, mapWidth / 2, 0));
            Camera.main.transform.position = new Vector3(centerPosition.x, centerPosition.y, -10f);

            FogOfWarManager fow = GetComponent<FogOfWarManager>();
            if (fow != null)
            {
                fow.InitializeFOW();
            }
        }
    }
}