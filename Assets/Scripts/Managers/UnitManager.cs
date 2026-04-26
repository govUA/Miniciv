using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class UnitManager : MonoBehaviour
{
    public GameObject unitPrefab;
    public Tilemap mainTilemap;
    public TurnManager turnManager;

    private List<Unit> activeUnits = new List<Unit>();

    public Unit SpawnUnit(HexNode spawnNode, int playerId)
    {
        if (spawnNode == null || !spawnNode.isLand || unitPrefab == null || turnManager == null)
        {
            Debug.LogWarning("Cannot spawn unit. Missing references or invalid node.");
            return null;
        }

        Vector3 spawnPos = mainTilemap.CellToWorld(new Vector3Int(spawnNode.y, spawnNode.x, 0));
        GameObject unitObj = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

        Unit newUnit = unitObj.GetComponent<Unit>();
        newUnit.Initialize(spawnNode, spawnPos, mainTilemap, turnManager, playerId);

        activeUnits.Add(newUnit);
        return newUnit;
    }

    public Unit GetUnitAtNode(HexNode node)
    {
        foreach (Unit u in activeUnits)
        {
            if (u.CurrentNode == node) return u;
        }

        return null;
    }

    public List<Unit> GetActiveUnits()
    {
        return activeUnits;
    }

    public void RemoveUnit(Unit unit)
    {
        if (activeUnits.Contains(unit))
        {
            activeUnits.Remove(unit);
            Destroy(unit.gameObject);
        }
    }
}