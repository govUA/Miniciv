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

        bool isMeleeAttack = attacker.unitClass == UnitClass.Melee ||
                             attacker.unitClass == UnitClass.AntiCavalry ||
                             (attacker.unitClass == UnitClass.Cavalry && attacker.attackRange == 1);

        if (!isMeleeAttack)
        {
            HexNode targetNode = defUnit != null ? defUnit.CurrentNode : (defCity != null ? defCity.centerNode : null);
            if (targetNode != null && HasObstacleInLOS(attacker.CurrentNode, targetNode))
            {
                attModifier -= 0.20f;
                Debug.Log("[COMBAT] Ranged attack passes over forest or mountains! -20% damage.");
            }
        }

        float attStrength = baseAttStrength * attModifier;

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

                Debug.Log(
                    $"[COMBAT] {attacker.unitName} attacks {defUnit.unitName} for {dmgToDef} damage! ({defUnit.currentHP}/{defUnit.maxHP} HP left)");

                if (isMeleeAttack && defUnit.currentHP > 0)
                {
                    float rngRet = Random.Range(0.85f, 1.15f);
                    int dmgToAtt = Mathf.RoundToInt(30f * (defStrength / attStrength) * rngRet);
                    attacker.TakeDamage(dmgToAtt);

                    Debug.Log(
                        $"[COMBAT] {defUnit.unitName} counter-attacks {attacker.unitName} for {dmgToAtt} damage! ({attacker.currentHP}/{attacker.maxHP} HP left)");
                }
            }
        }
        else if (defCity != null)
        {
            float rngHit = Random.Range(0.85f, 1.15f);
            int dmgToCity = Mathf.RoundToInt(30f * (attStrength / defCity.garrisonStrength) * rngHit);

            int oldOwner = defCity.ownerID;
            defCity.TakeDamage(dmgToCity, attacker.ownerID);

            Debug.Log(
                $"[COMBAT] {attacker.unitName} attacks City {defCity.cityName} for {dmgToCity} damage! ({defCity.currentHP}/{defCity.maxHP} HP left)");

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

                    Debug.Log(
                        $"[COMBAT] City {defCity.cityName} counter-attacks {attacker.unitName} for {dmgToAtt} damage! ({attacker.currentHP}/{attacker.maxHP} HP left)");
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

            float effectiveGarrison = attacker.garrisonStrength;
            if (HasObstacleInLOS(attacker.centerNode, defUnit.CurrentNode))
            {
                effectiveGarrison *= 0.8f;
                Debug.Log("[COMBAT] City attack passes over a forest/mountain! -20% to attack damage.");
            }

            float defStrength = defUnit.meleeStrength * defModifier;
            float rngHit = Random.Range(0.85f, 1.15f);

            int dmgToDef = Mathf.RoundToInt(30f * (effectiveGarrison / defStrength) * rngHit);
            defUnit.TakeDamage(dmgToDef);

            Debug.Log(
                $"[COMBAT] City {attacker.cityName} bombards {defUnit.unitName} for {dmgToDef} damage! ({defUnit.currentHP}/{defUnit.maxHP} HP left)");
        }
    }

    private bool HasObstacleInLOS(HexNode start, HexNode end)
    {
        int targetX = end.x;
        int targetY = end.y;

        if (hexGrid.wrapWorld)
        {
            int dx = start.x - end.x;
            if (Mathf.Abs(dx) > hexGrid.GetWidth() / 2)
            {
                targetX = end.x + (dx > 0 ? hexGrid.GetWidth() : -hexGrid.GetWidth());
            }
        }

        Vector3 startCube = OffsetToCube(start.x, start.y);
        Vector3 endCube = OffsetToCube(targetX, targetY);

        int dist = Mathf.Max(
            Mathf.Abs(Mathf.RoundToInt(startCube.x - endCube.x)),
            Mathf.Abs(Mathf.RoundToInt(startCube.y - endCube.y)),
            Mathf.Abs(Mathf.RoundToInt(startCube.z - endCube.z))
        );

        if (dist <= 1) return false;


        for (int i = 1; i < dist; i++)
        {
            float t = (float)i / dist;
            Vector3 pointCube = CubeLerp(startCube, endCube, t);
            Vector3Int roundedCube = CubeRound(pointCube);

            Vector2Int offsetCoord = CubeToOffset(roundedCube.x, roundedCube.y, roundedCube.z);
            HexNode nodeOnLine = hexGrid.GetNode(offsetCoord.x, offsetCoord.y);

            if (nodeOnLine != null)
            {
                if (nodeOnLine.terrainType == TerrainType.Forest || nodeOnLine.terrainType == TerrainType.Mountain)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Vector3 OffsetToCube(int x, int y)
    {
        int q = x;
        int r = y - (x - (x & 1)) / 2;
        int s = -q - r;
        return new Vector3(q, r, s);
    }

    private Vector2Int CubeToOffset(int q, int r, int s)
    {
        int x = q;
        int y = r + (q - (q & 1)) / 2;
        return new Vector2Int(x, y);
    }

    private Vector3 CubeLerp(Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            Mathf.Lerp(a.x, b.x, t) + 1e-6f,
            Mathf.Lerp(a.y, b.y, t) + 1e-6f,
            Mathf.Lerp(a.z, b.z, t) - 2e-6f
        );
    }

    private Vector3Int CubeRound(Vector3 frac)
    {
        int q = Mathf.RoundToInt(frac.x);
        int r = Mathf.RoundToInt(frac.y);
        int s = Mathf.RoundToInt(frac.z);

        float q_diff = Mathf.Abs(q - frac.x);
        float r_diff = Mathf.Abs(r - frac.y);
        float s_diff = Mathf.Abs(s - frac.z);

        if (q_diff > r_diff && q_diff > s_diff) q = -r - s;
        else if (r_diff > s_diff) r = -q - s;
        else s = -q - r;

        return new Vector3Int(q, r, s);
    }
}