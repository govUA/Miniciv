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

                    positions[index] = new Vector3Int(tileX, y, 0);
                    tiles[index] = tileToPlace;
                    index++;
                }
            }
        }

        tilemap.SetTiles(positions, tiles);
        CenterCameraAndInitBounds(mapWidth, mapHeight);
    }

    private void CenterCameraAndInitBounds(int mapWidth, int mapHeight)
    {
        Vector3 centerPosition = tilemap.CellToWorld(new Vector3Int(mapWidth / 2, mapHeight / 2, 0));
        Camera.main.transform.position = new Vector3(centerPosition.x, centerPosition.y, -10f);

        CameraController camController = Camera.main.GetComponent<CameraController>();
        if (camController != null)
        {
            camController.mapGenerator = mapGenerator;
            camController.tilemap = tilemap;
            camController.InitializeBounds();

            Camera.main.orthographicSize = Mathf.Clamp(mapHeight / 2f, camController.minZoom, camController.maxZoom);
        }
        else
        {
            Debug.LogWarning("CameraController not found on Main Camera.");
        }
    }
}