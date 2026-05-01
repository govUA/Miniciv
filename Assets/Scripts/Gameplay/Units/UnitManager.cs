using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class UnitDataModel
{
    public string name;
    public string unitClass;
    public int cost;
    public int meleeStrength;
    public int rangedStrength;
    public int attackRange;
    public int visionRange;
    public int maxMP;
    public string requiredTech;
    public int requiredPopulation;
    public int populationCost;

    [System.NonSerialized] public Sprite mainSprite;
    [System.NonSerialized] public Sprite iconSprite;
}

[System.Serializable]
public class UnitDatabase
{
    public List<UnitDataModel> units;
}

[System.Serializable]
public struct UnitVisuals
{
    public string unitName;
    public Sprite mainSprite;
    public Sprite iconSprite;
}

[RequireComponent(typeof(HexGrid))]
public class UnitManager : MonoBehaviour
{
    [Header("Data")] public TextAsset unitsJsonFile;
    public Dictionary<string, UnitDataModel> unitDatabaseDict = new Dictionary<string, UnitDataModel>();

    [Header("Settings")] public GameObject unitPrefab;
    public Tilemap mainTilemap;
    public TurnManager turnManager;

    private List<Unit> activeUnits = new List<Unit>();
    private HexGrid grid;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
        LoadUnitDatabase();
    }

    private void LoadUnitDatabase()
    {
        if (unitsJsonFile == null)
        {
            Debug.LogError("[UnitManager] JSON file with units not found!");
            return;
        }

        UnitDatabase db = JsonUtility.FromJson<UnitDatabase>(unitsJsonFile.text);
        if (db != null && db.units != null)
        {
            foreach (var unit in db.units)
            {
                unit.mainSprite = Resources.Load<Sprite>($"Sprites/Units/{unit.name}");
                unit.iconSprite = Resources.Load<Sprite>($"Icons/Units/{unit.name}");

                if (unit.mainSprite == null)
                    Debug.LogWarning($"[UnitManager] Unit sprite not found for: {unit.name}");

                unitDatabaseDict[unit.name] = unit;
            }

            Debug.Log($"[UnitManager] Unit database loaded. Visuals linked automatically.");
        }
    }

    public Unit SpawnUnit(HexNode spawnNode, int playerId, string unitName = "Settler")
    {
        if (spawnNode == null || unitPrefab == null || turnManager == null) return null;

        if (!unitDatabaseDict.ContainsKey(unitName))
        {
            Debug.LogError($"[UnitManager] Unit {unitName} not found in the database!");
            return null;
        }

        UnitDataModel unitData = unitDatabaseDict[unitName];
        HexNode actualSpawnNode = spawnNode;

        if (unitData.unitClass == "Naval" && spawnNode.isLand)
        {
            actualSpawnNode = null;
            foreach (HexNode neighbor in grid.GetNeighbors(spawnNode))
            {
                if (!neighbor.isLand && GetUnitAtNode(neighbor) == null)
                {
                    actualSpawnNode = neighbor;
                    break;
                }
            }

            if (actualSpawnNode == null)
            {
                Debug.LogWarning($"[UNIT] Cannot spawn {unitName}: no free adjacent water tiles.");
                return null;
            }
        }
        else if (unitData.unitClass != "Naval" && !spawnNode.isLand)
        {
            return null;
        }

        Vector3 spawnPos = mainTilemap.CellToWorld(new Vector3Int(actualSpawnNode.y, actualSpawnNode.x, 0));
        GameObject unitObj = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

        Unit newUnit = unitObj.GetComponent<Unit>();

        newUnit.Initialize(
            actualSpawnNode,
            spawnPos,
            mainTilemap,
            turnManager,
            playerId,
            unitData,
            grid,
            unitData.mainSprite,
            unitData.iconSprite
        );

        activeUnits.Add(newUnit);

        UpdateUnitOffsets(actualSpawnNode);

        Debug.Log($"[UNIT] Spawned {unitName} for Player {playerId} at [{actualSpawnNode.x},{actualSpawnNode.y}]");
        return newUnit;
    }

    public void RemoveUnit(Unit unit)
    {
        if (activeUnits.Contains(unit))
        {
            HexNode node = unit.CurrentNode;
            activeUnits.Remove(unit);
            Destroy(unit.gameObject);
            UpdateUnitOffsets(node);
        }
    }

    public Unit GetUnitAtNode(HexNode node)
    {
        Unit foundCiv = null;
        foreach (Unit u in activeUnits)
        {
            if (u.CurrentNode == node)
            {
                if (u.unitClass != UnitClass.Civilian)
                    return u;
                foundCiv = u;
            }
        }

        return foundCiv;
    }

    public List<Unit> GetUnitsAtNode(HexNode node)
    {
        List<Unit> units = new List<Unit>();
        foreach (Unit u in activeUnits)
        {
            if (u.CurrentNode == node)
                units.Add(u);
        }

        return units;
    }

    public List<Unit> GetActiveUnits() => activeUnits;

    void OnEnable()
    {
        Unit.OnUnitMoved += HandleUnitMoved;
    }

    void OnDisable()
    {
        Unit.OnUnitMoved -= HandleUnitMoved;
    }

    private void HandleUnitMoved(Unit unit)
    {
        UpdateUnitOffsets(unit.previousNode);
        UpdateUnitOffsets(unit.CurrentNode);
    }

    public void UpdateUnitOffsets(HexNode node)
    {
        if (node == null) return;
        List<Unit> unitsOnNode = GetUnitsAtNode(node);
        Vector3 centerPos = mainTilemap.CellToWorld(new Vector3Int(node.y, node.x, 0));

        if (unitsOnNode.Count == 1)
        {
            if (!unitsOnNode[0].IsAnimating())
                unitsOnNode[0].transform.position = centerPos;
        }
        else if (unitsOnNode.Count > 1)
        {
            foreach (Unit u in unitsOnNode)
            {
                if (!u.IsAnimating())
                {
                    if (u.unitClass == UnitClass.Civilian)
                        u.transform.position = centerPos + new Vector3(-0.25f, 0, 0);
                    else
                        u.transform.position = centerPos + new Vector3(0.25f, 0, 0);
                }
            }
        }
    }
}