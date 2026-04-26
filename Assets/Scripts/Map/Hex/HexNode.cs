using UnityEngine;

public class HexNode
{
    public int x;
    public int y;
    public bool isLand;
    public float movementCost;

    public int gCost;
    public int hCost;
    public HexNode parent;

    public HexNode(int x, int y, bool isLand)
    {
        this.x = x;
        this.y = y;
        this.isLand = isLand;
        this.movementCost = isLand ? 10f : 1000f;
    }

    public int fCost
    {
        get { return gCost + hCost; }
    }
}