using UnityEngine;

public abstract class MapPreset
{
    protected int mapWidth;
    protected int mapHeight;
    protected bool wrapWorld;
    protected System.Random prng;
    protected float centerX;
    protected float centerY;
    protected float maxRadius;

    public virtual void Initialize(int width, int height, bool wrap, System.Random random)
    {
        mapWidth = width;
        mapHeight = height;
        wrapWorld = wrap;
        prng = random;

        centerX = mapWidth / 2f;
        centerY = mapHeight / 2f;
        maxRadius = Mathf.Min(centerX, centerY);
    }

    // Main method to modify land probability
    public abstract float GetFillProbability(int x, int y, float baseFillProb);

    protected float GetDistance(float x1, float y1, float x2, float y2)
    {
        float dx = Mathf.Abs(x1 - x2);
        if (wrapWorld && dx > mapWidth / 2f)
        {
            dx = mapWidth - dx;
        }

        float dy = Mathf.Abs(y1 - y2);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}