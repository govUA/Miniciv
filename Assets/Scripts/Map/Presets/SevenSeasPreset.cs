using UnityEngine;
using System.Collections.Generic;

public class SevenSeasPreset : MapPreset
{
    private List<Vector2> nodesCenters;
    private List<Vector2> nodesRadiiTiles;
    private List<float> nodesAngles;
    private List<float> nodesIntensities;

    private List<Vector2> seaCenters;

    public override void Initialize(int width, int height, bool wrap, System.Random random)
    {
        base.Initialize(width, height, wrap, random);

        nodesCenters = new List<Vector2>();
        nodesRadiiTiles = new List<Vector2>();
        nodesAngles = new List<float>();
        nodesIntensities = new List<float>();
        seaCenters = new List<Vector2>();

        float mapScale = mapWidth / 102f;

        // ==========================================
        // CARVE EXACTLY 7 MAJOR COMPOSITE SEAS
        // ==========================================
        int numSeas = 7;

        for (int i = 0; i < numSeas; i++)
        {
            float cx = 0, cy = 0;
            bool positionFound = false;
            int attempts = 0;

            while (!positionFound && attempts < 200)
            {
                cx = (float)prng.NextDouble();
                cy = (float)(0.15 + prng.NextDouble() * 0.7);
                positionFound = IsPositionSafe(cx, cy, seaCenters, 22f * mapScale);
                attempts++;
            }

            if (!positionFound) continue;
            seaCenters.Add(new Vector2(cx, cy));

            float coreRx = (10f + (float)prng.NextDouble() * 6f) * mapScale;
            float coreRy = (8f + (float)prng.NextDouble() * 5f) * mapScale;
            float coreAngle = (float)(prng.NextDouble() * Mathf.PI);

            AddNode(cx, cy, coreRx, coreRy, coreAngle, -110f);

            int numGulfs = prng.Next(3, 7);
            for (int g = 0; g < numGulfs; g++)
            {
                float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
                float distTiles = (6f + (float)prng.NextDouble() * 8f) * mapScale;

                float gulfX = cx + (Mathf.Cos(angle) * distTiles) / mapWidth;
                float gulfY = cy + (Mathf.Sin(angle) * distTiles) / mapHeight;
                WrapCoordinates(ref gulfX, ref gulfY);

                float gulfRx = (5f + (float)prng.NextDouble() * 6f) * mapScale;
                float gulfRy = (4f + (float)prng.NextDouble() * 5f) * mapScale;
                float gulfAngle = (float)(prng.NextDouble() * Mathf.PI);

                AddNode(gulfX, gulfY, gulfRx, gulfRy, gulfAngle, -95f);
            }

            int numStraits = prng.Next(1, 4);
            for (int s = 0; s < numStraits; s++)
            {
                float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
                float distTiles = (10f + (float)prng.NextDouble() * 8f) * mapScale;

                float straitX = cx + (Mathf.Cos(angle) * distTiles) / mapWidth;
                float straitY = cy + (Mathf.Sin(angle) * distTiles) / mapHeight;
                WrapCoordinates(ref straitX, ref straitY);

                float straitRx = (12f + (float)prng.NextDouble() * 10f) * mapScale;
                float straitRy = (1.5f + (float)prng.NextDouble() * 1.5f) * mapScale;
                float straitAngle = angle + (float)((prng.NextDouble() - 0.5) * 0.4f);

                AddNode(straitX, straitY, straitRx, straitRy, straitAngle, -90f);
            }

            int numIslands = prng.Next(2, 6);
            for (int isl = 0; isl < numIslands; isl++)
            {
                float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
                float distTiles = (float)(prng.NextDouble() * 12f) * mapScale;

                float isleX = cx + (Mathf.Cos(angle) * distTiles) / mapWidth;
                float isleY = cy + (Mathf.Sin(angle) * distTiles) / mapHeight;
                WrapCoordinates(ref isleX, ref isleY);

                float isleRx = Mathf.Max(1.2f, (1.5f + (float)prng.NextDouble() * 2.0f) * mapScale);
                float isleRy = Mathf.Max(1.0f, (0.8f + (float)prng.NextDouble() * 1.5f) * mapScale);
                float isleAngle = (float)(prng.NextDouble() * Mathf.PI);

                float isleIntensity = 130f + (float)prng.NextDouble() * 30f;

                AddNode(isleX, isleY, isleRx, isleRy, isleAngle, isleIntensity);
            }
        }
    }

    private void AddNode(float x, float y, float rxTiles, float ryTiles, float angle, float intensity)
    {
        nodesCenters.Add(new Vector2(x, y));
        nodesRadiiTiles.Add(new Vector2(rxTiles, ryTiles));
        nodesAngles.Add(angle);
        nodesIntensities.Add(intensity);
    }

    private bool IsPositionSafe(float cx, float cy, List<Vector2> existingNodes, float minDistanceTiles)
    {
        foreach (Vector2 node in existingNodes)
        {
            float dx = Mathf.Abs(cx - node.x) * mapWidth;
            if (wrapWorld && dx > mapWidth / 2f) dx = mapWidth - dx;
            float dy = Mathf.Abs(cy - node.y) * mapHeight;

            if (Mathf.Sqrt(dx * dx + dy * dy) < minDistanceTiles) return false;
        }

        return true;
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

        float fillProb = baseFillProb + 30f;

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