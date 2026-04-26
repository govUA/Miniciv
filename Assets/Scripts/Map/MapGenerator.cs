using UnityEngine;
using System;

public class MapGenerator : MonoBehaviour
{
    public enum MapSizeType
    {
        Duel,
        Small,
        Standard,
        Large,
        Massive,
        Custom
    }

    public enum SeaLevelType
    {
        Low,
        Normal,
        High,
        Custom
    }

    [Header("Map Settings")] public MapType currentMapType = MapType.Continents;
    public MapSizeType mapSize = MapSizeType.Standard;
    public SeaLevelType seaLevel = SeaLevelType.Normal;

    public int mapWidth = 102;
    public int mapHeight = 64;
    public bool wrapWorld = true;

    [Header("Generation")] public string seed;
    public bool useRandomSeed = true;
    [Range(0, 100)] public int randomFillPercent = 33;
    public int smoothingIterations = 2;

    private int[,] map;

    private static readonly Vector2Int[] evenNeighbors =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 0), new Vector2Int(1, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, -1)
    };

    private static readonly Vector2Int[] oddNeighbors =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, 0),
        new Vector2Int(-1, 1), new Vector2Int(-1, 0)
    };

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        ApplyPresets();

        if (wrapWorld && mapWidth % 2 != 0)
        {
            mapWidth += 1;
        }

        map = new int[mapWidth, mapHeight];
        RandomFillMap();

        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }

        MapDisplay display = GetComponent<MapDisplay>();
        if (display != null)
        {
            display.DrawMap();
        }
    }

    public void ApplyPresets()
    {
        switch (mapSize)
        {
            case MapSizeType.Duel:
                mapWidth = 52;
                mapHeight = 32;
                break;
            case MapSizeType.Small:
                mapWidth = 76;
                mapHeight = 48;
                break;
            case MapSizeType.Standard:
                mapWidth = 102;
                mapHeight = 64;
                break;
            case MapSizeType.Large:
                mapWidth = 154;
                mapHeight = 96;
                break;
            case MapSizeType.Massive:
                mapWidth = 204;
                mapHeight = 128;
                break;
            case MapSizeType.Custom:
                mapWidth = Mathf.Clamp(mapWidth, 32, 256);
                mapHeight = Mathf.Clamp(mapHeight, 32, 256);
                break;
        }

        switch (seaLevel)
        {
            case SeaLevelType.Low:
                randomFillPercent = 38;
                smoothingIterations = 2;
                break;
            case SeaLevelType.Normal:
                randomFillPercent = 33;
                smoothingIterations = 2;
                break;
            case SeaLevelType.High:
                randomFillPercent = 28;
                smoothingIterations = 3;
                break;
            case SeaLevelType.Custom: break;
        }
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = DateTime.Now.Ticks.ToString();
        }

        System.Random prng = new System.Random(seed.GetHashCode());

        MapPreset currentPreset = MapPresetFactory.GetPreset(currentMapType);
        currentPreset.Initialize(mapWidth, mapHeight, wrapWorld, prng);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float fillProb = currentPreset.GetFillProbability(x, y, randomFillPercent);
                fillProb = Mathf.Clamp(fillProb, 0, 100);

                map[x, y] = (prng.Next(0, 100) < fillProb) ? 1 : 0;
            }
        }
    }

    void SmoothMap()
    {
        int[,] newMap = new int[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > 3)
                    newMap[x, y] = 1;
                else if (neighbourWallTiles < 3)
                    newMap[x, y] = 0;
                else
                    newMap[x, y] = map[x, y];
            }
        }

        map = newMap;
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        Vector2Int[] neighbors = (gridX % 2 == 0) ? evenNeighbors : oddNeighbors;

        foreach (Vector2Int offset in neighbors)
        {
            int neighbourX = gridX + offset.x;
            int neighbourY = gridY + offset.y;

            if (wrapWorld)
            {
                neighbourX = (neighbourX % mapWidth + mapWidth) % mapWidth;
            }

            if (neighbourX >= 0 && neighbourX < mapWidth && neighbourY >= 0 && neighbourY < mapHeight)
            {
                wallCount += map[neighbourX, neighbourY];
            }
        }

        return wallCount;
    }

    public int[,] GetMap()
    {
        return map;
    }

    void OnValidate()
    {
        ApplyPresets();
    }
}