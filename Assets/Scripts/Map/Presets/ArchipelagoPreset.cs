using UnityEngine;
using System.Collections.Generic;

public class ArchipelagoPreset : MapPreset
{
    private List<Vector2> landCenters;
    private List<Vector2> landRadiiTiles;
    private List<float> landAngles;
    private List<float> landIntensities;

    private List<Vector2> majorLandmassNodes;
    private List<Vector2> chainStartNodes;
    private List<Vector2> allCompletedChainNodes;

    public override void Initialize(int width, int height, bool wrap, System.Random random)
    {
        base.Initialize(width, height, wrap, random);

        landCenters = new List<Vector2>();
        landRadiiTiles = new List<Vector2>();
        landAngles = new List<float>();
        landIntensities = new List<float>();

        majorLandmassNodes = new List<Vector2>();
        chainStartNodes = new List<Vector2>();
        allCompletedChainNodes = new List<Vector2>();

        float areaMultiplier = (mapWidth * mapHeight) / 6500f;
        float mapScale = Mathf.Clamp(mapWidth / 102f, 0.4f, 1.0f);

        // ==========================================
        // 1. LARGE ISLANDS (Sub-continents)
        // ==========================================
        int numLargeIslands = Mathf.Max(2, Mathf.RoundToInt(prng.Next(4, 8) * areaMultiplier));

        for (int i = 0; i < numLargeIslands; i++)
        {
            float cx = 0, cy = 0;
            bool positionFound = false;
            int attempts = 0;

            while (!positionFound && attempts < 100)
            {
                cx = (float)prng.NextDouble();
                cy = (float)(0.15 + prng.NextDouble() * 0.7);
                positionFound = IsPositionSafe(cx, cy, majorLandmassNodes, 20f * mapScale);
                attempts++;
            }

            if (!positionFound) continue;
            majorLandmassNodes.Add(new Vector2(cx, cy));

            int nodesCount = prng.Next(4, 9);
            float islandAngle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float islandCurve = (float)((prng.NextDouble() - 0.5) * 1.2f);

            float currX = cx, currY = cy;

            for (int n = 0; n < nodesCount; n++)
            {
                float rxTiles = (2.5f + (float)prng.NextDouble() * 3.5f) * mapScale;
                float ryTiles;

                if (prng.NextDouble() < 0.4)
                {
                    ryTiles = Mathf.Max(0.8f, (0.6f + (float)prng.NextDouble() * 1.0f) * mapScale);
                }
                else
                {
                    ryTiles = (2.0f + (float)prng.NextDouble() * 3.0f) * mapScale;
                }

                float intensity = 85f + (float)prng.NextDouble() * 20f;

                AddNode(currX, currY, rxTiles, ryTiles, islandAngle, intensity);

                if (n > 0) majorLandmassNodes.Add(new Vector2(currX, currY));

                islandAngle += islandCurve;
                float stepTiles = rxTiles * (ryTiles < 1.6f * mapScale ? 1.2f : 0.8f);

                currX += (Mathf.Cos(islandAngle) * stepTiles) / mapWidth;
                currY += (Mathf.Sin(islandAngle) * stepTiles) / mapHeight;

                if (wrapWorld) currX = (currX % 1f + 1f) % 1f;
                else currX = Mathf.Clamp(currX, 0.05f, 0.95f);
                currY = Mathf.Clamp(currY, 0.05f, 0.95f);
            }
        }

        // ==========================================
        // 2. ARCHIPELAGOS (Tectonic Belts)
        // ==========================================
        int numArchipelagos = Mathf.Max(6, Mathf.RoundToInt(prng.Next(20, 35) * areaMultiplier));

        for (int i = 0; i < numArchipelagos; i++)
        {
            float archX = 0, archY = 0;
            bool positionFound = false;
            int attempts = 0;

            while (!positionFound && attempts < 100)
            {
                archX = (float)prng.NextDouble();
                archY = (float)(0.10 + prng.NextDouble() * 0.8);
                if (IsPositionSafe(archX, archY, majorLandmassNodes, 15f * mapScale) &&
                    IsPositionSafe(archX, archY, chainStartNodes, 12f * mapScale))
                {
                    positionFound = true;
                }

                attempts++;
            }

            if (!positionFound) continue;
            chainStartNodes.Add(new Vector2(archX, archY));

            float chainAngle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            float chainCurve = (float)((prng.NextDouble() - 0.5) * 0.6f);

            int numIslands = prng.Next(20, 45);

            float currX = archX, currY = archY;
            List<Vector2> currentChainNodes = new List<Vector2>();

            for (int j = 0; j < numIslands; j++)
            {
                float lateralOffsetTiles = (float)((prng.NextDouble() - 0.5) * 5.0f) * mapScale;
                float perpAngle = chainAngle + Mathf.PI / 2f;

                float isleX = currX + (Mathf.Cos(perpAngle) * lateralOffsetTiles) / mapWidth;
                float isleY = currY + (Mathf.Sin(perpAngle) * lateralOffsetTiles) / mapHeight;

                if (wrapWorld) isleX = (isleX % 1f + 1f) % 1f;
                else isleX = Mathf.Clamp(isleX, 0.02f, 0.98f);
                isleY = Mathf.Clamp(isleY, 0.05f, 0.95f);

                if (!IsPositionSafe(isleX, isleY, majorLandmassNodes, 5f * mapScale)) continue;

                if (!IsPositionSafe(isleX, isleY, allCompletedChainNodes, 6f * mapScale)) break;

                float isleRxTiles = Mathf.Max(1.5f, (1.5f + (float)prng.NextDouble() * 2.5f) * mapScale);
                float isleRyTiles = Mathf.Max(1.0f, (0.6f + (float)prng.NextDouble() * 1.2f) * mapScale);

                float isleIntensity = 110f + (float)prng.NextDouble() * 40f;

                AddNode(isleX, isleY, isleRxTiles, isleRyTiles, chainAngle, isleIntensity);
                currentChainNodes.Add(new Vector2(isleX, isleY));

                float stepTiles = Mathf.Max(1.2f, (1.2f + (float)prng.NextDouble() * 1.5f) * mapScale);

                chainAngle += chainCurve;
                currX += (Mathf.Cos(chainAngle) * stepTiles) / mapWidth;
                currY += (Mathf.Sin(chainAngle) * stepTiles) / mapHeight;
            }

            allCompletedChainNodes.AddRange(currentChainNodes);
        }
    }

    private void AddNode(float x, float y, float rxTiles, float ryTiles, float angle, float intensity)
    {
        landCenters.Add(new Vector2(x, y));
        landRadiiTiles.Add(new Vector2(rxTiles, ryTiles));
        landAngles.Add(angle);
        landIntensities.Add(intensity);
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

    public override float GetFillProbability(int x, int y, float baseFillProb)
    {
        float nx = (float)x / mapWidth;
        float ny = (float)y / mapHeight;
        float fillProb = baseFillProb - 45f;

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