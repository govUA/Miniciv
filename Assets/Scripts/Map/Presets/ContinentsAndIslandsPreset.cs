using UnityEngine;
using System.Collections.Generic;

public class ContinentsAndIslandsPreset : MapPreset
{
    private List<Vector2> landCenters;
    private List<Vector2> landRadii;
    private List<float> landAngles;
    private List<float> landIntensities;
    private List<Vector2> allGeneratedNodes;

    public override void Initialize(int width, int height, bool wrap, System.Random random)
    {
        base.Initialize(width, height, wrap, random);

        landCenters = new List<Vector2>();
        landRadii = new List<Vector2>();
        landAngles = new List<float>();
        landIntensities = new List<float>();
        allGeneratedNodes = new List<Vector2>();

        int numContinents = prng.Next(2, 5);

        for (int i = 0; i < numContinents; i++)
        {
            float cx = 0, cy = 0;
            bool positionFound = false;
            int attempts = 0;

            while (!positionFound && attempts < 100)
            {
                cx = (float)prng.NextDouble();
                cy = (float)(0.15 + prng.NextDouble() * 0.7);
                positionFound = IsPositionSafe(cx, cy, allGeneratedNodes, 0.28f);
                attempts++;
            }

            float coreRx = (float)(0.06 + prng.NextDouble() * 0.08);
            float coreRy = (float)(0.08 + prng.NextDouble() * 0.12);
            float coreIntensity = (float)(85.0 + prng.NextDouble() * 15.0);

            AddNode(cx, cy, coreRx, coreRy, 0f, coreIntensity);

            int numSubRegions = prng.Next(2, 5);
            for (int j = 0; j < numSubRegions; j++)
            {
                float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
                float offsetDist = (float)(0.04 + prng.NextDouble() * 0.09);
                float subX = cx + Mathf.Cos(angle) * offsetDist;
                float subY = cy + Mathf.Sin(angle) * offsetDist;

                if (wrapWorld) subX = (subX % 1f + 1f) % 1f;
                else subX = Mathf.Clamp(subX, 0.05f, 0.95f);
                subY = Mathf.Clamp(subY, 0.05f, 0.95f);

                float subRx = (float)(0.03 + prng.NextDouble() * 0.07);
                float subRy = (float)(0.03 + prng.NextDouble() * 0.07);
                float subAngle = (float)(prng.NextDouble() * Mathf.PI);
                float subIntensity = (float)(75.0 + prng.NextDouble() * 20.0);

                AddNode(subX, subY, subRx, subRy, subAngle, subIntensity);
            }
        }

        int numLargeIslands = prng.Next(5, 10);
        for (int i = 0; i < numLargeIslands; i++)
        {
            float cx = 0, cy = 0;
            bool positionFound = false;
            int attempts = 0;

            while (!positionFound && attempts < 100)
            {
                cx = (float)prng.NextDouble();
                cy = (float)(0.10 + prng.NextDouble() * 0.8);
                positionFound = IsPositionSafe(cx, cy, allGeneratedNodes, 0.15f);
                attempts++;
            }

            float rx = (float)(0.02 + prng.NextDouble() * 0.04);
            float ry = (float)(0.05 + prng.NextDouble() * 0.09);
            if (prng.NextDouble() > 0.5)
            {
                float temp = rx;
                rx = ry;
                ry = temp;
            }

            float angle = (float)(prng.NextDouble() * Mathf.PI);
            float intensity = (float)(85.0 + prng.NextDouble() * 15.0);

            AddNode(cx, cy, rx, ry, angle, intensity);
        }

        int numArchipelagos = prng.Next(12, 22);
        for (int i = 0; i < numArchipelagos; i++)
        {
            float archX = 0, archY = 0;
            bool positionFound = false;
            int attempts = 0;

            while (!positionFound && attempts < 100)
            {
                archX = (float)prng.NextDouble();
                archY = (float)(0.10 + prng.NextDouble() * 0.8);
                positionFound = IsPositionSafe(archX, archY, allGeneratedNodes, 0.12f);
                attempts++;
            }

            if (!positionFound) continue;

            float chainAngle = (float)(prng.NextDouble() * Mathf.PI * 2f);
            int numIslands = prng.Next(5, 12);

            for (int j = 0; j < numIslands; j++)
            {
                float islandOffset = (float)((prng.NextDouble() - 0.5) * 0.30);
                float isleX = archX + Mathf.Cos(chainAngle) * islandOffset;
                float isleY = archY + Mathf.Sin(chainAngle) * islandOffset;

                if (wrapWorld) isleX = (isleX % 1f + 1f) % 1f;
                else isleX = Mathf.Clamp(isleX, 0.02f, 0.98f);
                isleY = Mathf.Clamp(isleY, 0.05f, 0.95f);

                if (!IsPositionSafe(isleX, isleY, allGeneratedNodes, 0.08f)) continue;

                float isleRx = (float)(0.018 + prng.NextDouble() * 0.03);
                float isleRy = (float)(0.008 + prng.NextDouble() * 0.02);
                float isleIntensity = (float)(90.0 + prng.NextDouble() * 30.0);

                AddNode(isleX, isleY, isleRx, isleRy, chainAngle, isleIntensity);
            }
        }
    }

    private void AddNode(float x, float y, float rx, float ry, float angle, float intensity)
    {
        landCenters.Add(new Vector2(x, y));
        landRadii.Add(new Vector2(rx, ry));
        landAngles.Add(angle);
        landIntensities.Add(intensity);

        allGeneratedNodes.Add(new Vector2(x, y));
    }

    private bool IsPositionSafe(float cx, float cy, List<Vector2> existingNodes, float minDistance)
    {
        foreach (Vector2 node in existingNodes)
        {
            float dx = Mathf.Abs(cx - node.x);
            if (wrapWorld && dx > 0.5f) dx = 1f - dx;
            float dy = Mathf.Abs(cy - node.y);

            if (Mathf.Sqrt(dx * dx + dy * dy) < minDistance)
            {
                return false;
            }
        }

        return true;
    }

    public override float GetFillProbability(int x, int y, float baseFillProb)
    {
        float nx = (float)x / mapWidth;
        float ny = (float)y / mapHeight;
        float fillProb = baseFillProb - 35f;

        for (int i = 0; i < landCenters.Count; i++)
        {
            fillProb += GetInfluence(nx, ny, landCenters[i].x, landCenters[i].y, landRadii[i].x, landRadii[i].y,
                landAngles[i], landIntensities[i]);
        }

        return fillProb;
    }

    private float GetInfluence(float nx, float ny, float cx, float cy, float rx, float ry, float angle, float intensity)
    {
        float dx = nx - cx;
        if (wrapWorld)
        {
            if (dx > 0.5f) dx -= 1f;
            else if (dx < -0.5f) dx += 1f;
        }

        float dy = ny - cy;

        float maxRadius = rx > ry ? rx : ry;
        if (Mathf.Abs(dx) > maxRadius || Mathf.Abs(dy) > maxRadius)
        {
            return 0f;
        }

        float cos = Mathf.Cos(-angle);
        float sin = Mathf.Sin(-angle);

        float rotX = dx * cos - dy * sin;
        float rotY = dx * sin + dy * cos;

        rotX /= rx;
        rotY /= ry;

        float distSq = rotX * rotX + rotY * rotY;
        if (distSq > 1f) return 0f;

        return (1f - distSq) * intensity;
    }
}