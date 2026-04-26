using UnityEngine;
using System.Collections.Generic;

public class InlandSeaPreset : MapPreset
{
    private List<Vector2> nodesCenters;
    private List<Vector2> nodesRadiiTiles;
    private List<float> nodesAngles;
    private List<float> nodesIntensities;

    public override void Initialize(int width, int height, bool wrap, System.Random random)
    {
        base.Initialize(width, height, wrap, random);

        nodesCenters = new List<Vector2>();
        nodesRadiiTiles = new List<Vector2>();
        nodesAngles = new List<float>();
        nodesIntensities = new List<float>();

        float mapScale = mapWidth / 102f;

        float seaCX = 0.5f + (float)((prng.NextDouble() - 0.5) * 0.1);
        float seaCY = 0.5f + (float)((prng.NextDouble() - 0.5) * 0.1);

        // ==========================================
        // 1. MAIN SEA CORE (Negative influence)
        // ==========================================
        float coreRx = (18f + (float)prng.NextDouble() * 4f) * mapScale;
        float coreRy = (14f + (float)prng.NextDouble() * 4f) * mapScale;
        float coreAngle = (float)(prng.NextDouble() * Mathf.PI);
        AddNode(seaCX, seaCY, coreRx, coreRy, coreAngle, -120f);

        // ==========================================
        // 2. GULFS AND SUB-SEAS
        // ==========================================
        int numGulfs = prng.Next(5, 10);
        for (int i = 0; i < numGulfs; i++)
        {
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float distTiles = (10f + (float)prng.NextDouble() * 15f) * mapScale;

            float cx = seaCX + (Mathf.Cos(angle) * distTiles) / mapWidth;
            float cy = seaCY + (Mathf.Sin(angle) * distTiles) / mapHeight;
            WrapCoordinates(ref cx, ref cy);

            float rx = (8f + (float)prng.NextDouble() * 8f) * mapScale;
            float ry = (6f + (float)prng.NextDouble() * 6f) * mapScale;
            float rotAngle = (float)(prng.NextDouble() * Mathf.PI);

            AddNode(cx, cy, rx, ry, rotAngle, -100f);
        }

        // ==========================================
        // 3. STRAITS AND FAULTS
        // ==========================================
        int numStraits = prng.Next(2, 5);
        for (int i = 0; i < numStraits; i++)
        {
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float distTiles = (15f + (float)prng.NextDouble() * 10f) * mapScale;

            float cx = seaCX + (Mathf.Cos(angle) * distTiles) / mapWidth;
            float cy = seaCY + (Mathf.Sin(angle) * distTiles) / mapHeight;
            WrapCoordinates(ref cx, ref cy);

            float rx = (15f + (float)prng.NextDouble() * 15f) * mapScale;
            float ry = (1.5f + (float)prng.NextDouble() * 2f) * mapScale;

            float rotAngle = angle + (float)((prng.NextDouble() - 0.5) * 0.5f);

            AddNode(cx, cy, rx, ry, rotAngle, -90f);
        }

        // ==========================================
        // 4. ISLANDS INSIDE THE SEA (Positive influence)
        // ==========================================
        int numIslands = Mathf.RoundToInt(prng.Next(10, 25) * mapScale);
        for (int i = 0; i < numIslands; i++)
        {
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float distTiles = (float)(prng.NextDouble() * 20f) * mapScale;

            float cx = seaCX + (Mathf.Cos(angle) * distTiles) / mapWidth;
            float cy = seaCY + (Mathf.Sin(angle) * distTiles) / mapHeight;
            WrapCoordinates(ref cx, ref cy);

            float rx = Mathf.Max(1.2f, (1.5f + (float)prng.NextDouble() * 2.5f) * mapScale);
            float ry = Mathf.Max(0.8f, (0.8f + (float)prng.NextDouble() * 1.5f) * mapScale);
            float rotAngle = (float)(prng.NextDouble() * Mathf.PI);

            float intensity = 140f + (float)prng.NextDouble() * 40f;

            AddNode(cx, cy, rx, ry, rotAngle, intensity);
        }

        // ==========================================
        // 5. OUTER LAKES
        // ==========================================
        int numLakes = Mathf.RoundToInt(prng.Next(5, 15) * mapScale);
        for (int i = 0; i < numLakes; i++)
        {
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float distTiles = (30f + (float)prng.NextDouble() * 20f) * mapScale;

            float cx = seaCX + (Mathf.Cos(angle) * distTiles) / mapWidth;
            float cy = seaCY + (Mathf.Sin(angle) * distTiles) / mapHeight;
            WrapCoordinates(ref cx, ref cy);

            float rx = Mathf.Max(1.5f, (2f + (float)prng.NextDouble() * 3f) * mapScale);
            float ry = Mathf.Max(1.5f, (2f + (float)prng.NextDouble() * 3f) * mapScale);
            float rotAngle = (float)(prng.NextDouble() * Mathf.PI);

            AddNode(cx, cy, rx, ry, rotAngle, -90f);
        }
    }

    private void AddNode(float x, float y, float rxTiles, float ryTiles, float angle, float intensity)
    {
        nodesCenters.Add(new Vector2(x, y));
        nodesRadiiTiles.Add(new Vector2(rxTiles, ryTiles));
        nodesAngles.Add(angle);
        nodesIntensities.Add(intensity);
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

        // Base map is solid land
        float fillProb = baseFillProb + 35f;

        for (int i = 0; i < nodesCenters.Count; i++)
        {
            fillProb += GetInfluence(nx, ny, nodesCenters[i].x, nodesCenters[i].y, nodesRadiiTiles[i].x,
                nodesRadiiTiles[i].y, nodesAngles[i], nodesIntensities[i]);
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