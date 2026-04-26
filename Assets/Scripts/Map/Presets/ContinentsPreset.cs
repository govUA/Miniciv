using UnityEngine;
using System.Collections.Generic;

public class ContinentsPreset : MapPreset
{
    private List<Vector2> continentCenters;
    private List<Vector2> continentRadii;
    private List<float> continentIntensities;

    public override void Initialize(int width, int height, bool wrap, System.Random random)
    {
        base.Initialize(width, height, wrap, random);

        int numContinents = prng.Next(4, 9);

        continentCenters = new List<Vector2>();
        continentRadii = new List<Vector2>();
        continentIntensities = new List<float>();

        List<Vector2> coreCenters = new List<Vector2>();

        for (int i = 0; i < numContinents; i++)
        {
            float cx = 0;
            float cy = 0;
            bool positionFound = false;
            int attempts = 0;

            while (!positionFound && attempts < 100)
            {
                cx = (float)prng.NextDouble();
                cy = (float)(0.15 + prng.NextDouble() * 0.7);

                positionFound = true;

                foreach (Vector2 existingCore in coreCenters)
                {
                    float dx = Mathf.Abs(cx - existingCore.x);
                    if (wrapWorld && dx > 0.5f) dx = 1f - dx;
                    float dy = Mathf.Abs(cy - existingCore.y);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < 0.28f)
                    {
                        positionFound = false;
                        break;
                    }
                }

                attempts++;
            }

            coreCenters.Add(new Vector2(cx, cy));

            float coreRx = (float)(0.06 + prng.NextDouble() * 0.08);
            float coreRy = (float)(0.08 + prng.NextDouble() * 0.12);
            float coreIntensity = (float)(85.0 + prng.NextDouble() * 15.0);

            continentCenters.Add(new Vector2(cx, cy));
            continentRadii.Add(new Vector2(coreRx, coreRy));
            continentIntensities.Add(coreIntensity);

            int numSubRegions = prng.Next(2, 6);
            for (int j = 0; j < numSubRegions; j++)
            {
                float angle = (float)(prng.NextDouble() * Mathf.PI * 2f);
                float offsetDist = (float)(0.04 + prng.NextDouble() * 0.09);

                float subX = cx + Mathf.Cos(angle) * offsetDist;
                float subY = cy + Mathf.Sin(angle) * offsetDist;

                if (wrapWorld)
                {
                    subX = (subX % 1f + 1f) % 1f;
                }
                else
                {
                    subX = Mathf.Clamp(subX, 0.05f, 0.95f);
                }

                subY = Mathf.Clamp(subY, 0.05f, 0.95f);

                float subRx = (float)(0.03 + prng.NextDouble() * 0.07);
                float subRy = (float)(0.03 + prng.NextDouble() * 0.07);
                float subIntensity = (float)(70.0 + prng.NextDouble() * 20.0);

                continentCenters.Add(new Vector2(subX, subY));
                continentRadii.Add(new Vector2(subRx, subRy));
                continentIntensities.Add(subIntensity);
            }
        }
    }

    public override float GetFillProbability(int x, int y, float baseFillProb)
    {
        float nx = (float)x / mapWidth;
        float ny = (float)y / mapHeight;
        float fillProb = baseFillProb - 35f;

        for (int i = 0; i < continentCenters.Count; i++)
        {
            fillProb += GetInfluence(nx, ny, continentCenters[i].x, continentCenters[i].y, continentRadii[i].x,
                continentRadii[i].y, continentIntensities[i]);
        }

        return fillProb;
    }

    private float GetInfluence(float nx, float ny, float cx, float cy, float rx, float ry, float intensity)
    {
        float dx = Mathf.Abs(nx - cx);
        if (wrapWorld && dx > 0.5f)
        {
            dx = 1f - dx;
        }

        float dy = Mathf.Abs(ny - cy);

        if (dx > rx || dy > ry)
        {
            return 0f;
        }

        dx /= rx;
        dy /= ry;

        float distSq = dx * dx + dy * dy;

        if (distSq > 1f) return 0f;

        float falloff = 1f - distSq;
        return falloff * intensity;
    }
}