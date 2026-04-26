using UnityEngine;

public class HexGrid : MonoBehaviour
{
    private MapGenerator mapGenerator;
    private HexNode[,] nodes;
    private int width;
    private int height;

    public void InitializeGrid(MapGenerator generator)
    {
        mapGenerator = generator;
        int[,] mapData = mapGenerator.GetMap();
        width = mapData.GetLength(0);
        height = mapData.GetLength(1);

        nodes = new HexNode[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isLand = mapData[x, y] == 1;
                nodes[x, y] = new HexNode(x, y, isLand);
            }
        }
    }

    public HexNode GetNode(int x, int y)
    {
        if (mapGenerator == null) return null;

        if (mapGenerator.wrapWorld)
        {
            x = (x % width + width) % width;
        }
        else if (x < 0 || x >= width)
        {
            return null;
        }

        if (y < 0 || y >= height)
        {
            return null;
        }

        return nodes[x, y];
    }

    public int GetWidth() => width;
    public int GetHeight() => height;
}