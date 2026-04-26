using UnityEngine;

public class EarthPreset : MapPreset
{
    public override float GetFillProbability(int x, int y, float baseFillProb)
    {
        float nx = (float)x / mapWidth;
        float ny = (float)y / mapHeight;
        float fillProb = baseFillProb - 30f;

        // NORTH AMERICA
        fillProb += GetInfluence(nx, ny, 0.16f, 0.75f, 0.12f, 0.10f, 85f); // Canada
        fillProb += GetInfluence(nx, ny, 0.26f, 0.75f, 0.05f, 0.06f, 75f); // Labrador Peninsula
        fillProb += GetInfluence(nx, ny, 0.34f, 0.85f, 0.04f, 0.10f, 85f); // Greenland
        fillProb += GetInfluence(nx, ny, 0.13f, 0.65f, 0.06f, 0.10f, 85f); // US West
        fillProb += GetInfluence(nx, ny, 0.22f, 0.62f, 0.06f, 0.08f, 85f); // US East
        fillProb += GetInfluence(nx, ny, 0.08f, 0.82f, 0.06f, 0.06f, 75f); // Alaska
        fillProb += GetInfluence(nx, ny, 0.17f, 0.55f, 0.05f, 0.08f, 80f); // Mexico
        fillProb += GetInfluence(nx, ny, 0.19f, 0.49f, 0.02f, 0.04f, 80f); // Northern Central America
        fillProb += GetInfluence(nx, ny, 0.21f, 0.45f, 0.02f, 0.03f, 80f); // Southern Central America
        fillProb += GetInfluence(nx, ny, 0.22f, 0.41f, 0.02f, 0.03f, 85f); // Panama
        fillProb += GetInfluence(nx, ny, 0.24f, 0.52f, 0.03f, 0.015f, 50f); // Greater Antilles
        fillProb += GetInfluence(nx, ny, 0.27f, 0.50f, 0.02f, 0.015f, 45f); // Hispaniola
        fillProb += GetInfluence(nx, ny, 0.29f, 0.46f, 0.01f, 0.03f, 40f); // Lesser Antilles

        // SOUTH AMERICA
        fillProb += GetInfluence(nx, ny, 0.28f, 0.35f, 0.08f, 0.12f, 90f); // Brazil
        fillProb += GetInfluence(nx, ny, 0.21f, 0.32f, 0.03f, 0.08f, 80f); // Peru
        fillProb += GetInfluence(nx, ny, 0.26f, 0.26f, 0.05f, 0.08f, 85f); // La Pampas
        fillProb += GetInfluence(nx, ny, 0.24f, 0.18f, 0.04f, 0.10f, 80f); // Argentina/Chile

        // EUROPE
        fillProb += GetInfluence(nx, ny, 0.45f, 0.65f, 0.05f, 0.06f, 85f); // Western Europe
        fillProb += GetInfluence(nx, ny, 0.43f, 0.58f, 0.04f, 0.05f, 80f); // Iberian Peninsula
        fillProb += GetInfluence(nx, ny, 0.48f, 0.58f, 0.02f, 0.05f, 75f); // Italy
        fillProb += GetInfluence(nx, ny, 0.49f, 0.66f, 0.05f, 0.05f, 85f); // Central Europe
        fillProb += GetInfluence(nx, ny, 0.51f, 0.60f, 0.03f, 0.05f, 75f); // Balkans
        fillProb += GetInfluence(nx, ny, 0.54f, 0.70f, 0.07f, 0.08f, 85f); // Eastern Europe
        fillProb += GetInfluence(nx, ny, 0.48f, 0.78f, 0.04f, 0.08f, 80f); // Scandinavia
        fillProb += GetInfluence(nx, ny, 0.51f, 0.76f, 0.03f, 0.04f, 80f); // Finland
        fillProb += GetInfluence(nx, ny, 0.40f, 0.70f, 0.02f, 0.04f, 65f); // British Isles
        fillProb += GetInfluence(nx, ny, 0.38f, 0.78f, 0.02f, 0.02f, 55f); // Iceland
        fillProb += GetInfluence(nx, ny, 0.58f, 0.64f, 0.04f, 0.04f, 80f); // Caucasus
        fillProb += GetInfluence(nx, ny, 0.56f, 0.61f, 0.02f, 0.02f, 80f); // Caucasus to Anatolia bridge

        // MIDDLE EAST
        fillProb += GetInfluence(nx, ny, 0.55f, 0.58f, 0.04f, 0.03f, 80f); // Anatolia
        fillProb += GetInfluence(nx, ny, 0.54f, 0.53f, 0.02f, 0.03f, 75f); // Levant
        fillProb += GetInfluence(nx, ny, 0.58f, 0.56f, 0.03f, 0.03f, 80f); // Mesopotamia
        fillProb += GetInfluence(nx, ny, 0.59f, 0.46f, 0.03f, 0.07f, 85f); // Arabia
        fillProb += GetInfluence(nx, ny, 0.63f, 0.56f, 0.05f, 0.05f, 85f); // Iran
        fillProb += GetInfluence(nx, ny, 0.67f, 0.57f, 0.03f, 0.04f, 85f); // Iran to Central Asia bridge

        // AFRICA
        fillProb += GetInfluence(nx, ny, 0.45f, 0.50f, 0.06f, 0.06f, 90f); // Maghreb
        fillProb += GetInfluence(nx, ny, 0.44f, 0.40f, 0.03f, 0.03f, 85f); // Ghana
        fillProb += GetInfluence(nx, ny, 0.53f, 0.48f, 0.04f, 0.06f, 85f); // Egypt/Sudan
        fillProb += GetInfluence(nx, ny, 0.49f, 0.45f, 0.06f, 0.06f, 90f); // Sahara
        fillProb += GetInfluence(nx, ny, 0.50f, 0.35f, 0.08f, 0.08f, 95f); // Central Africa
        fillProb += GetInfluence(nx, ny, 0.52f, 0.24f, 0.06f, 0.06f, 85f); // South Africa
        fillProb += GetInfluence(nx, ny, 0.51f, 0.29f, 0.04f, 0.05f, 85f); // Central to South bridge
        fillProb += GetInfluence(nx, ny, 0.58f, 0.38f, 0.04f, 0.05f, 80f); // Horn of Africa
        fillProb += GetInfluence(nx, ny, 0.56f, 0.42f, 0.02f, 0.04f, 80f); // Egypt to Horn bridge
        fillProb += GetInfluence(nx, ny, 0.62f, 0.25f, 0.01f, 0.08f, 75f); // Madagascar

        // ASIA & SIBERIA
        fillProb += GetInfluence(nx, ny, 0.68f, 0.66f, 0.08f, 0.08f, 90f); // Central Asia
        fillProb += GetInfluence(nx, ny, 0.65f, 0.78f, 0.12f, 0.10f, 95f); // West Siberia
        fillProb += GetInfluence(nx, ny, 0.74f, 0.75f, 0.08f, 0.08f, 95f); // Central Siberia
        fillProb += GetInfluence(nx, ny, 0.82f, 0.80f, 0.12f, 0.12f, 95f); // East Siberia
        fillProb += GetInfluence(nx, ny, 0.78f, 0.70f, 0.08f, 0.08f, 90f); // East Asia to Siberia bridge
        fillProb += GetInfluence(nx, ny, 0.94f, 0.82f, 0.05f, 0.08f, 85f); // Kamchatka
        fillProb += GetInfluence(nx, ny, 0.72f, 0.58f, 0.06f, 0.05f, 85f); // Tibet
        fillProb += GetInfluence(nx, ny, 0.80f, 0.60f, 0.08f, 0.08f, 90f); // China
        fillProb += GetInfluence(nx, ny, 0.74f, 0.54f, 0.06f, 0.06f, 85f); // China to India bridge
        fillProb += GetInfluence(nx, ny, 0.83f, 0.68f, 0.05f, 0.05f, 85f); // Manchuria
        fillProb += GetInfluence(nx, ny, 0.86f, 0.62f, 0.015f, 0.03f, 75f); // Korea
        fillProb += GetInfluence(nx, ny, 0.69f, 0.44f, 0.04f, 0.12f, 85f); // India
        fillProb += GetInfluence(nx, ny, 0.74f, 0.50f, 0.03f, 0.05f, 80f); // Burma
        fillProb += GetInfluence(nx, ny, 0.91f, 0.63f, 0.01f, 0.06f, 75f); // Honshu
        fillProb += GetInfluence(nx, ny, 0.92f, 0.68f, 0.01f, 0.015f, 55f); // Hokkaido
        fillProb += GetInfluence(nx, ny, 0.90f, 0.59f, 0.01f, 0.015f, 55f); // Shikoku
        fillProb += GetInfluence(nx, ny, 0.89f, 0.57f, 0.01f, 0.015f, 55f); // Kyushu

        // SOUTHEAST ASIA
        fillProb += GetInfluence(nx, ny, 0.79f, 0.48f, 0.04f, 0.06f, 80f); // Indochina
        fillProb += GetInfluence(nx, ny, 0.79f, 0.38f, 0.02f, 0.05f, 65f); // Malaya
        fillProb += GetInfluence(nx, ny, 0.86f, 0.45f, 0.02f, 0.04f, 50f); // Philippines
        fillProb += GetInfluence(nx, ny, 0.78f, 0.35f, 0.02f, 0.04f, 55f); // Sumatra
        fillProb += GetInfluence(nx, ny, 0.82f, 0.31f, 0.03f, 0.015f, 55f); // Java
        fillProb += GetInfluence(nx, ny, 0.82f, 0.36f, 0.02f, 0.02f, 55f); // Borneo
        fillProb += GetInfluence(nx, ny, 0.85f, 0.33f, 0.015f, 0.02f, 50f); // Sulawesi
        fillProb += GetInfluence(nx, ny, 0.89f, 0.32f, 0.03f, 0.04f, 50f); // Papua

        // AUSTRALIA & OCEANIA
        fillProb += GetInfluence(nx, ny, 0.84f, 0.20f, 0.06f, 0.08f, 85f); // West Australia
        fillProb += GetInfluence(nx, ny, 0.92f, 0.22f, 0.05f, 0.09f, 85f); // East Australia
        fillProb += GetInfluence(nx, ny, 0.98f, 0.10f, 0.02f, 0.04f, 60f); // New Zealand
        fillProb += GetInfluence(nx, ny, 0.05f, 0.35f, 0.03f, 0.04f, 40f); // Polynesia
        fillProb += GetInfluence(nx, ny, 0.95f, 0.40f, 0.02f, 0.03f, 35f); // Micronesia
        fillProb += GetInfluence(nx, ny, 0.95f, 0.30f, 0.03f, 0.02f, 40f); // Melanesia

        // INLAND SEAS & GULFS (CARVING)
        fillProb += GetInfluence(nx, ny, 0.22f, 0.78f, 0.03f, 0.04f, -80f); // Hudson Bay
        fillProb += GetInfluence(nx, ny, 0.18f, 0.50f, 0.04f, 0.04f, -60f); // Gulf of Mexico
        fillProb += GetInfluence(nx, ny, 0.48f, 0.52f, 0.07f, 0.025f, -80f); // Mediterranean Sea
        fillProb += GetInfluence(nx, ny, 0.56f, 0.45f, 0.01f, 0.06f, -85f); // Red Sea
        fillProb += GetInfluence(nx, ny, 0.61f, 0.52f, 0.012f, 0.02f, -80f); // Persian Gulf
        fillProb += GetInfluence(nx, ny, 0.63f, 0.64f, 0.01f, 0.025f, -80f); // Caspian Sea
        fillProb += GetInfluence(nx, ny, 0.54f, 0.62f, 0.02f, 0.015f, -80f); // Black Sea
        fillProb += GetInfluence(nx, ny, 0.50f, 0.74f, 0.03f, 0.015f, -60f); // Baltic Sea
        fillProb += GetInfluence(nx, ny, 0.85f, 0.60f, 0.015f, 0.03f, -80f); // Yellow Sea
        fillProb += GetInfluence(nx, ny, 0.88f, 0.64f, 0.02f, 0.04f, -60f); // Sea of Japan
        fillProb += GetInfluence(nx, ny, 0.74f, 0.42f, 0.04f, 0.04f, -80f); // Bay of Bengal
        fillProb += GetInfluence(nx, ny, 0.60f, 0.25f, 0.01f, 0.05f, -80f); // Mozambique Channel
        fillProb += GetInfluence(nx, ny, 0.92f, 0.15f, 0.03f, 0.04f, -80f); // Tasman Sea
        fillProb += GetInfluence(nx, ny, 0.88f, 0.26f, 0.02f, 0.02f, -90f); // Gulf of Carpentaria
        fillProb += GetInfluence(nx, ny, 0.87f, 0.15f, 0.03f, 0.02f, -90f); // Great Australian Bight
        fillProb += GetInfluence(nx, ny, 0.95f, 0.26f, 0.02f, 0.04f, -70f); // Great Barrier Reef

        // ANTARCTICA
        if (ny < 0.10f)
        {
            float antarcticaInfluence = 1f - (ny / 0.10f);
            fillProb += antarcticaInfluence * 95f;
        }

        fillProb += GetInfluence(nx, ny, 0.24f, 0.13f, 0.08f, 0.02f, -100f); // Drake Passage

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