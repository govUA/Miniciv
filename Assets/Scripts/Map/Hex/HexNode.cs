using UnityEngine;

public class HexNode
{
    public int x;
    public int y;
    public bool isLand;
    public float movementCost;

    public HexNode(int x, int y, bool isLand)
    {
        this.x = x;
        this.y = y;
        this.isLand = isLand;
        this.movementCost = isLand ? 1f : 1000f;
    }
}