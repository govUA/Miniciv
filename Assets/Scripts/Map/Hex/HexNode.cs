using UnityEngine;
using System.Collections.Generic;

public enum VisionState
{
    Unexplored,
    Explored,
    Visible
}

public class HexNode
{
    public int x;
    public int y;
    public bool isLand;
    public float movementCost;

    public int gCost;
    public int hCost;
    public HexNode parent;

    private Dictionary<int, VisionState> visionStates = new Dictionary<int, VisionState>();

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

    public VisionState GetVision(int playerId)
    {
        if (visionStates.TryGetValue(playerId, out VisionState state))
        {
            return state;
        }

        return VisionState.Unexplored;
    }

    public void SetVision(int playerId, VisionState state)
    {
        visionStates[playerId] = state;
    }
}