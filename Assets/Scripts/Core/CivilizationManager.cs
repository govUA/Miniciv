using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CivilizationData
{
    public string civName;
    public string leaderName;
    public string uniqueUnit;
    public string uniqueBuilding;
    public string primaryColorHex;
    public string secondaryColorHex;
    public List<string> cityNames;
}

[System.Serializable]
public class CivilizationDatabase
{
    public List<CivilizationData> civilizations;
}

public class CivilizationManager : MonoBehaviour
{
    [Tooltip("Drag your Civilizations.json here")]
    public TextAsset jsonFile;

    public List<CivilizationData> availableCivilizations = new List<CivilizationData>();

    void Awake()
    {
        LoadCivilizations();
    }

    private void LoadCivilizations()
    {
        if (jsonFile != null)
        {
            CivilizationDatabase database = JsonUtility.FromJson<CivilizationDatabase>(jsonFile.text);
            availableCivilizations = database.civilizations;
            Debug.Log($"[CIV] Successfully loaded {availableCivilizations.Count} civilizations from JSON.");
        }
        else
        {
            Debug.LogError("[CIV] JSON file with civilizations not assigned in inspector!");
        }
    }

    public CivilizationData AssignRandomCivilization(List<CivilizationData> alreadyAssigned)
    {
        List<CivilizationData> pool = new List<CivilizationData>(availableCivilizations);

        foreach (var assigned in alreadyAssigned)
        {
            pool.Remove(assigned);
        }

        if (pool.Count > 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        Debug.LogWarning("[CIV] Not enough free civilizations in the pool!");
        return null;
    }
}