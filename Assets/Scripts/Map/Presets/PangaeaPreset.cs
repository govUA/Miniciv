using UnityEngine;
using System.Collections.Generic;

public class PangaeaPreset : MapPreset
{
    private List<Vector2> landCenters;
    private List<Vector2> landRadiiTiles;
    private List<float> landAngles;
    private List<float> landIntensities;

    public override void Initialize(int width, int height, bool wrap, System.Random random)
    {
        base.Initialize(width, height, wrap, random);

        landCenters = new List<Vector2>();
        landRadiiTiles = new List<Vector2>();
        landAngles = new List<float>();
        landIntensities = new List<float>();

        float mapScale = mapWidth / 102f;

        float panCX = 0.5f + (float)((prng.NextDouble() - 0.5) * 0.1);
        float panCY = 0.5f + (float)((prng.NextDouble() - 0.5) * 0.1);

        // ==========================================
        // 1. MAIN SUPERCONTINENT CORE
        // ==========================================
        float coreRx = 16f * mapScale;
        float coreRy = 16f * mapScale;
        AddNode(panCX, panCY, coreRx, coreRy, 0f, 100f);

        // ==========================================
        // 2. SUB-CORES (Merged tectonic plates)
        // ==========================================
        int numSubCores = prng.Next(3, 7);
        for (int i = 0; i < numSubCores; i++)
        {
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float distTiles = (8f + (float)prng.NextDouble() * 12f) * mapScale;

            float cx = panCX + (Mathf.Cos(angle) * distTiles) / mapWidth;
            float cy = panCY + (Mathf.Sin(angle) * distTiles) / mapHeight;
            WrapCoordinates(ref cx, ref cy);

            float rx = (8f + (float)prng.NextDouble() * 8f) * mapScale;
            float ry = (8f + (float)prng.NextDouble() * 8f) * mapScale;
            float rotAngle = (float)(prng.NextDouble() * Mathf.PI);

            AddNode(cx, cy, rx, ry, rotAngle, 95f);
        }

        // ==========================================
        // 3. PENINSULAS AND CURVED EDGES
        // ==========================================
        int numPeninsulas = prng.Next(5, 10);
        for (int i = 0; i < numPeninsulas; i++)
        {
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float distTiles = (15f + (float)prng.NextDouble() * 15f) * mapScale;

            float cx = panCX + (Mathf.Cos(angle) * distTiles) / mapWidth;
            float cy = panCY + (Mathf.Sin(angle) * distTiles) / mapHeight;
            WrapCoordinates(ref cx, ref cy);

            float rx = (6f + (float)prng.NextDouble() * 6f) * mapScale;
            float ry = (2f + (float)prng.NextDouble() * 3f) * mapScale;
            float rotAngle = angle + (float)((prng.NextDouble() - 0.5) * 1.5f);

            AddNode(cx, cy, rx, ry, rotAngle, 85f);
        }

        // ==========================================
        // 4. COASTAL ISLANDS AND ARCHIPELAGOS
        // ==========================================
        int numIslands = Mathf.RoundToInt(prng.Next(15, 35) * mapScale);
        for (int i = 0; i < numIslands; i++)
        {
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float distTiles = (25f + (float)prng.NextDouble() * 20f) * mapScale;

            float cx = panCX + (Mathf.Cos(angle) * distTiles) / mapWidth;
            float cy = panCY + (Mathf.Sin(angle) * distTiles) / mapHeight;
            WrapCoordinates(ref cx, ref cy);

            float rx = Mathf.Max(1.2f, (1.5f + (float)prng.NextDouble() * 2.0f) * mapScale);
            float ry = Mathf.Max(0.8f, (0.8f + (float)prng.NextDouble() * 1.2f) * mapScale);
            float rotAngle = (float)(prng.NextDouble() * Mathf.PI);

            float intensity = 95f + (float)prng.NextDouble() * 25f;

            AddNode(cx, cy, rx, ry, rotAngle, intensity);
        }
    }

    private void AddNode(float x, float y, float rxTiles, float ryTiles, float angle, float intensity)
    {
        landCenters.Add(new Vector2(x, y));
        landRadiiTiles.Add(new Vector2(rxTiles, ryTiles));
        landAngles.Add(angle);
        landIntensities.Add(intensity);
    }

    private void WrapCoordinates(ref float x, ref float y)
    {
        if (wrapWorld) x = (x % 1f + 1f) % 1f;
        else x = Mathf.Clamp(x, 0.05f, 0.95f);

        y = Mathf.Clamp(y, 0.05f, 0.95f);
    }

    public override float GetFillProbability(int x, int y, float baseFillProb)
    {
        float nx = (float)x / mapWidth;
        float ny = (float)y / mapHeight;

        float fillProb = baseFillProb - 40f;

        for (int i = 0; i < landCenters.Count; i++)
        {
            fillProb += GetInfluence(nx, ny, landCenters[i].x, landCenters[i].y, landRadiiTiles[i].x,
                landRadiiTiles[i].y, landAngles[i], landIntensities[i]);
        }

        return fillProb;
    }

    private float GetInfluence(float nx, float ny, float cx, float cy, float rxTiles, float ryTiles, float angle,
        float intensity)
    {
        float dxTiles = (nx - cx) * mapWidth;
        if (wrapWorld)
        {
            if (dxTiles > mapWidth / 2f) dxTiles -= mapWidth;
            else if (dxTiles < -mapWidth / 2f) dxTiles += mapWidth;
        }

        float dyTiles = (ny - cy) * mapHeight;

        float maxRadius = rxTiles > ryTiles ? rxTiles : ryTiles;
        if (Mathf.Abs(dxTiles) > maxRadius || Mathf.Abs(dyTiles) > maxRadius)
        {
            return 0f;
        }

        float cos = Mathf.Cos(-angle);
        float sin = Mathf.Sin(-angle);

        float rotX = dxTiles * cos - dyTiles * sin;
        float rotY = dxTiles * sin + dyTiles * cos;

        rotX /= rxTiles;
        rotY /= ryTiles;

        float distSq = rotX * rotX + rotY * rotY;
        if (distSq > 1f) return 0f;

        return (1f - distSq) * intensity;
    }
}