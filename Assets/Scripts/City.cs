using UnityEngine;

public class City : MonoBehaviour
{
    public int ownerID;
    public HexNode centerNode;
    public string cityName;
    public int visionRange = 3;
    public int territoryRange = 1;

    public void Initialize(HexNode node, int playerId, string name)
    {
        centerNode = node;
        ownerID = playerId;
        cityName = name;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = (ownerID == 0) ? Color.blue : Color.red;
        }
    }

    public void SetVisibility(bool isVisible)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = isVisible;
        }
    }
}