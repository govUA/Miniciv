using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class UnitManager : MonoBehaviour
{
    public GameObject unitPrefab;
    public Tilemap mainTilemap;
    
    private List<Unit> activeUnits = new List<Unit>();

    public Unit SpawnUnit(HexNode spawnNode)
    {
        if (spawnNode == null || !spawnNode.isLand || unitPrefab == null)
        {
            Debug.LogWarning("Cannot spawn unit. Invalid node or missing prefab.");
            return null;
        }

        Vector3 spawnPos = mainTilemap.CellToWorld(new Vector3Int(spawnNode.y, spawnNode.x, 0));
        GameObject unitObj = Instantiate(unitPrefab, spawnPos, Quaternion.identity);
        
        Unit newUnit = unitObj.GetComponent<Unit>();
        newUnit.Initialize(spawnNode, spawnPos);
        
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
}