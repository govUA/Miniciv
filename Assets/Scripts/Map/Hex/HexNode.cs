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

    public int ownerID = -1;
    public bool hasCity = false;

    private Dictionary<int, VisionState> visionStates = new Dictionary<int, VisionState>();
    private Dictionary<int, int> rememberedOwner = new Dictionary<int, int>();
    private Dictionary<int, bool> rememberedCity = new Dictionary<int, bool>();

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
            return state;
        return VisionState.Unexplored;
    }

    public void SetVision(int playerId, VisionState state)
    {
        visionStates[playerId] = state;
    }

    public int GetRememberedOwner(int playerId)
    {
        if (rememberedOwner.TryGetValue(playerId, out int owner)) return owner;
        return -1;
    }

    public void SetRememberedOwner(int playerId, int owner)
    {
        rememberedOwner[playerId] = owner;
    }

    public bool GetRememberedCity(int playerId)
    {
        if (rememberedCity.TryGetValue(playerId, out bool city)) return city;
        return false;
    }

    public void SetRememberedCity(int playerId, bool city)
    {
        rememberedCity[playerId] = city;
    }
}