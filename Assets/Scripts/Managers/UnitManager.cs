using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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
    public GameObject unitPrefab;
    public Tilemap mainTilemap;
    public TurnManager turnManager;

    public List<UnitVisuals> unitVisualSettings;

    private List<Unit> activeUnits = new List<Unit>();
    private HexGrid grid;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }

    public Unit SpawnUnit(HexNode spawnNode, int playerId, string unitName = "Settler")
    {
        if (spawnNode == null || !spawnNode.isLand || unitPrefab == null || turnManager == null) return null;

        Vector3 spawnPos = mainTilemap.CellToWorld(new Vector3Int(spawnNode.y, spawnNode.x, 0));
        GameObject unitObj = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

        Unit newUnit = unitObj.GetComponent<Unit>();

        Sprite mainSprite = null;
        Sprite iconSprite = null;

        foreach (var setting in unitVisualSettings)
        {
            if (setting.unitName == unitName)
            {
                mainSprite = setting.mainSprite;
                iconSprite = setting.iconSprite;
                break;
            }
        }

        newUnit.Initialize(spawnNode, spawnPos, mainTilemap, turnManager, playerId, unitName, grid, mainSprite,
            iconSprite);

        activeUnits.Add(newUnit);

        Debug.Log($"[UNIT] Spawned {unitName} for Player {playerId} at [{spawnNode.x},{spawnNode.y}]");
        return newUnit;
    }

    public void RemoveUnit(Unit unit)
    {
        if (activeUnits.Contains(unit))
        {
            activeUnits.Remove(unit);
            Destroy(unit.gameObject);
        }
    }

    public Unit GetUnitAtNode(HexNode node)
    {
        foreach (Unit u in activeUnits)
            if (u.CurrentNode == node)
                return u;
        return null;
    }

    public List<Unit> GetActiveUnits() => activeUnits;
}