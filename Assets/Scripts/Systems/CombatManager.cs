using UnityEngine;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    [Header("Dependencies")] public HexGrid hexGrid;
    public UnitManager unitManager;
    public CityManager cityManager;
    public Pathfinder pathfinder;

    public void ResolveUnitCombat(Unit attacker, Unit defUnit, City defCity)
    {
        attacker.hasAttackedThisTurn = true;
        attacker.currentMP = 0;
        attacker.isFortified = false;
        attacker.isHealing = false;

        float attModifier = 1.05f;
        if (attacker.CurrentNode.terrainType == TerrainType.Mountain) attModifier += 0.10f;

        float baseAttStrength = (attacker.unitClass == UnitClass.Melee || attacker.unitClass == UnitClass.AntiCavalry)
            ? attacker.meleeStrength
            : attacker.rangedStrength;

        if (attacker.unitClass == UnitClass.Cavalry)
        {
            baseAttStrength = attacker.attackRange > 1 ? attacker.rangedStrength : attacker.meleeStrength;
        }

        if (attacker.unitClass == UnitClass.Melee && defUnit != null)
        {
            bool hasCavalryFlank = false;
            List<HexNode> neighbors = hexGrid.GetNeighbors(defUnit.CurrentNode);

            foreach (HexNode neighbor in neighbors)
            {
                List<Unit> adjacentUnits = unitManager.GetUnitsAtNode(neighbor);
                foreach (Unit u in adjacentUnits)
                {
                    if (u.ownerID == attacker.ownerID && u.unitClass == UnitClass.Cavalry)
                    {
                        hasCavalryFlank = true;
                        break;
                    }
                }

                if (hasCavalryFlank) break;
            }

            if (hasCavalryFlank)
            {
                attModifier += 0.20f;
                Debug.Log("[COMBAT] Flanking bonus from neighbouring cavalry activated: +20% to attack damage!");
            }
        }

        if (attacker.unitClass == UnitClass.AntiCavalry && defUnit != null && defUnit.unitClass == UnitClass.Cavalry)
        {
            attModifier += 0.50f;
            Debug.Log("[COMBAT] Anti-Cavalry bonus activated: +50% attack against Cavalry!");
        }

        float attStrength = baseAttStrength * attModifier;

        bool isMeleeAttack = attacker.unitClass == UnitClass.Melee ||
                             attacker.unitClass == UnitClass.AntiCavalry ||
                             (attacker.unitClass == UnitClass.Cavalry && attacker.attackRange == 1);

        if (defUnit != null)
        {
            City cityOnTile = null;
            if (cityManager != null)
            {
                foreach (City c in cityManager.GetActiveCities())
                    if (c.centerNode == defUnit.CurrentNode && c.ownerID == defUnit.ownerID)
                        cityOnTile = c;
            }

            if (cityOnTile != null)
            {
                defCity = cityOnTile;
                defUnit = null;
                Debug.Log("[COMBAT] Unit protected by city! City takes the damage instead.");
            }
        }

        if (defUnit != null)
        {
            if (defUnit.unitClass == UnitClass.Civilian) defUnit.TakeDamage(999);
            else
            {
                defUnit.isHealing = false;

                float defModifier = 1.0f;
                if (defUnit.isFortified) defModifier += 0.25f;
                if (defUnit.CurrentNode.terrainType == TerrainType.Forest) defModifier += 0.15f;

                if (defUnit.unitClass == UnitClass.AntiCavalry && attacker.unitClass == UnitClass.Cavalry)
                {
                    defModifier += 0.50f;
                    Debug.Log("[COMBAT] Anti-Cavalry bonus activated: +50% defense against Cavalry!");
                }

                float defStrength = defUnit.meleeStrength * defModifier;

                float rngHit = Random.Range(0.85f, 1.15f);
                int dmgToDef = Mathf.RoundToInt(30f * (attStrength / defStrength) * rngHit);
                defUnit.TakeDamage(dmgToDef);

                if (isMeleeAttack && defUnit.currentHP > 0)
                {
                    float rngRet = Random.Range(0.85f, 1.15f);
                    int dmgToAtt = Mathf.RoundToInt(30f * (defStrength / attStrength) * rngRet);
                    attacker.TakeDamage(dmgToAtt);
                }
            }
        }
        else if (defCity != null)
        {
            float rngHit = Random.Range(0.85f, 1.15f);
            int dmgToCity = Mathf.RoundToInt(30f * (attStrength / defCity.garrisonStrength) * rngHit);

            int oldOwner = defCity.ownerID;
            defCity.TakeDamage(dmgToCity, attacker.ownerID);

            if (defCity.ownerID == attacker.ownerID && oldOwner != attacker.ownerID)
            {
                Unit garrison = null;
                if (unitManager != null)
                {
                    garrison = unitManager.GetUnitAtNode(defCity.centerNode);
                    if (garrison != null && garrison.ownerID != attacker.ownerID)
                    {
                        garrison.TakeDamage(9999);
                    }
                }

                if (pathfinder != null)
                {
                    List<HexNode> capturePath = pathfinder.FindPath(attacker, defCity.centerNode);
                    if (capturePath != null && capturePath.Count > 0)
                    {
                        attacker.currentMP += 99;
                        attacker.SetPath(capturePath, () => { attacker.currentMP = 0; });
                    }
                }
            }
            else
            {
                if (isMeleeAttack && defCity.ownerID != attacker.ownerID)
                {
                    float rngRet = Random.Range(0.85f, 1.15f);
                    int dmgToAtt = Mathf.RoundToInt(30f * ((float)defCity.garrisonStrength / attStrength) * rngRet);
                    attacker.TakeDamage(dmgToAtt);
                }
            }
        }
    }

    public void ResolveCityCombat(City attacker, Unit defUnit)
    {
        attacker.hasAttackedThisTurn = true;
        if (defUnit.unitClass == UnitClass.Civilian) defUnit.TakeDamage(999);
        else
        {
            defUnit.isHealing = false;
            float defModifier = 1.0f;
            if (defUnit.isFortified) defModifier += 0.25f;
            if (defUnit.CurrentNode.terrainType == TerrainType.Forest) defModifier += 0.15f;

            float defStrength = defUnit.meleeStrength * defModifier;
            float rngHit = Random.Range(0.85f, 1.15f);

            int dmgToDef = Mathf.RoundToInt(30f * ((float)attacker.garrisonStrength / defStrength) * rngHit);
            defUnit.TakeDamage(dmgToDef);
        }
    }
}