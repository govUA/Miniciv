public enum MapType
{
    Continents,
    Archipelago,
    ContinentsAndIslands,
    Pangaea,
    InlandSea,
    SevenSeas,
    Earth
}

public static class MapPresetFactory
{
    public static MapPreset GetPreset(MapType type)
    {
        switch (type)
        {
            case MapType.Continents: return new ContinentsPreset();
            case MapType.Archipelago: return new ArchipelagoPreset();
            case MapType.ContinentsAndIslands: return new ContinentsAndIslandsPreset();
            case MapType.Pangaea: return new PangaeaPreset();
            case MapType.InlandSea: return new InlandSeaPreset();
            case MapType.SevenSeas: return new SevenSeasPreset();
            case MapType.Earth: return new EarthPreset();
            default: return new ContinentsPreset();
        }
    }
}