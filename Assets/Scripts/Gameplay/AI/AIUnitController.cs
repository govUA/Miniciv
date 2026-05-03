using UnityEngine;
using System.Collections.Generic;

public class AIUnitController : MonoBehaviour
{
    public UnitManager unitManager;
    public CityManager cityManager;
    private PlayerManager playerManager;
    private CombatManager combatManager;

    public void ExecuteUnitActions(int playerId)
    {
        if (unitManager == null) unitManager = FindObjectOfType<UnitManager>();
        if (cityManager == null) cityManager = FindObjectOfType<CityManager>();
        if (playerManager == null) playerManager = FindObjectOfType<PlayerManager>();
        if (combatManager == null) combatManager = FindObjectOfType<CombatManager>();

        TechManager techManager = FindObjectOfType<TechManager>();
        bool canSail = techManager != null && techManager.HasTech(playerId, "Sailing");
        HexGrid grid = FindObjectOfType<HexGrid>();

        City targetEnemyCity = DetermineCampaignTarget(playerId, grid);
        HexNode rallyPoint = null;
        bool isArmyReady = false;

        if (targetEnemyCity != null)
        {
            rallyPoint = DetermineRallyPoint(targetEnemyCity, playerId, grid);
            isArmyReady = CheckArmyStrength(playerId, targetEnemyCity);

            if (isArmyReady)
                Debug.Log($"[AI COMMAND] Player {playerId} launches FULL ATTACK on {targetEnemyCity.cityName}!");
            else if (rallyPoint != null)
                Debug.Log(
                    $"[AI COMMAND] Player {playerId} is gathering forces at Rally Point to attack {targetEnemyCity.cityName}.");
        }

        List<Unit> allUnits = new List<Unit>(unitManager.GetActiveUnits());
        Pathfinder pathfinder = FindObjectOfType<Pathfinder>();

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;
            if (unit.ownerID != playerId || unit.State != UnitState.Idle || unit.currentMP <= 0) continue;

            bool attacked = false;
            if (unit.unitClass != UnitClass.Civilian && !unit.hasAttackedThisTurn && !unit.isHealing)
            {
                attacked = TryAttack(unit, grid);
            }

            if (attacked) continue;

            HexNode targetNode = EvaluateBestDestination(unit, grid, canSail, targetEnemyCity, rallyPoint, isArmyReady);

            if (targetNode != null)
            {
                if (targetNode == unit.CurrentNode)
                {
                    if (unit.isSettler)
                    {
                        if (cityManager.FoundCity(unit)) unitManager.RemoveUnit(unit);
                    }
                    else
                    {
                        unit.isFortified = true;
                        unit.currentMP = 0;
                    }
                }
                else
                {
                    List<HexNode> path = pathfinder.FindPath(unit, targetNode);
                    if (path != null && path.Count > 0)
                    {
                        unit.MoveAlongPath(path);
                    }
                }
            }
        }
    }

    private HexNode EvaluateBestDestination(Unit unit, HexGrid grid, bool canSail, City targetCity, HexNode rallyPoint,
        bool isArmyReady)
    {
        if (unit.isSettler) return FindBestSettlerDestination(unit, grid, canSail);

        TurnManager tm = FindObjectOfType<TurnManager>();
        bool isBarbarian = (tm != null && unit.ownerID == tm.TotalPlayers - 1);

        if (unit.currentHP < 30 || (unit.isHealing && unit.currentHP < 90))
        {
            unit.isHealing = true;
            City nearestFriendlyCity = GetNearestFriendlyCity(unit, grid);

            if (nearestFriendlyCity != null)
            {
                int distToCity = grid.GetDistance(unit.CurrentNode, nearestFriendlyCity.centerNode);
                if (distToCity <= 2)
                {
                    unit.isFortified = true;
                    return unit.CurrentNode;
                }

                return nearestFriendlyCity.centerNode;
            }
        }
        else
        {
            unit.isHealing = false;
        }

        if (targetCity != null && !isBarbarian)
        {
            if (isArmyReady)
            {
                if (unit.unitClass == UnitClass.Naval && targetCity.centerNode.isLand)
                {
                    // TODO: Fleet logic
                }
                else return targetCity.centerNode;
            }
            else if (rallyPoint != null)
            {
                int distToRally = grid.GetDistance(unit.CurrentNode, rallyPoint);
                if (distToRally > 2)
                {
                    return rallyPoint;
                }
                else
                {
                    unit.isFortified = true;
                    return unit.CurrentNode;
                }
            }
        }

        HexNode bestTargetNode = null;
        int minDistance = int.MaxValue;

        foreach (Unit u in unitManager.GetActiveUnits())
        {
            if (IsHostile(unit.ownerID, u.ownerID) &&
                (isBarbarian || u.CurrentNode.GetVision(unit.ownerID) == VisionState.Visible))
            {
                if (unit.unitClass == UnitClass.Naval && u.CurrentNode.isLand) continue;
                if (unit.unitClass != UnitClass.Naval && !u.CurrentNode.isLand && !canSail) continue;

                int dist = grid.GetDistance(unit.CurrentNode, u.CurrentNode);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestTargetNode = u.CurrentNode;
                }
            }
        }

        if (bestTargetNode != null) return bestTargetNode;

        List<HexNode> nearbyNodes = grid.GetNodesInRange(unit.CurrentNode, 10);
        foreach (HexNode node in nearbyNodes)
        {
            if (node.GetVision(unit.ownerID) == VisionState.Unexplored)
            {
                if (unit.unitClass == UnitClass.Naval && node.isLand) continue;
                if (unit.unitClass != UnitClass.Naval && !node.isLand && !canSail) continue;
                return node;
            }
        }

        City cityToGuard = GetNearestFriendlyCity(unit, grid);
        return cityToGuard != null ? cityToGuard.centerNode : unit.CurrentNode;
    }

    private City DetermineCampaignTarget(int playerId, HexGrid grid)
    {
        City bestTarget = null;
        float bestTargetScore = -9999f;

        foreach (City c in cityManager.GetActiveCities())
        {
            if (IsHostile(playerId, c.ownerID) && c.centerNode.GetVision(playerId) != VisionState.Unexplored)
            {
                City myNearestCity = GetNearestFriendlyCity(c.centerNode, playerId, grid);
                if (myNearestCity != null)
                {
                    int d = grid.GetDistance(myNearestCity.centerNode, c.centerNode);

                    int enemyWonderCount = 0;
                    foreach (string b in c.builtBuildings)
                    {
                        if (cityManager.buildingDatabaseDict.TryGetValue(b, out var bData) && bData.isWonder)
                            enemyWonderCount++;
                    }

                    float targetScore = -d;

                    if (enemyWonderCount > 0)
                    {
                        targetScore += (enemyWonderCount * 10f);
                    }

                    if (targetScore > bestTargetScore)
                    {
                        bestTargetScore = targetScore;
                        bestTarget = c;
                    }
                }
            }
        }

        return bestTarget;
    }

    private HexNode DetermineRallyPoint(City targetCity, int playerId, HexGrid grid)
    {
        City myNearestCity = GetNearestFriendlyCity(targetCity.centerNode, playerId, grid);
        if (myNearestCity == null) return targetCity.centerNode;

        HexNode bestRally = myNearestCity.centerNode;
        int shortestToEnemy = grid.GetDistance(bestRally, targetCity.centerNode);

        List<HexNode> candidateNodes = grid.GetNodesInRange(myNearestCity.centerNode, 3);
        foreach (HexNode n in candidateNodes)
        {
            if (n.isLand && n.ownerID != targetCity.ownerID)
            {
                int distToEnemy = grid.GetDistance(n, targetCity.centerNode);
                if (distToEnemy < shortestToEnemy)
                {
                    shortestToEnemy = distToEnemy;
                    bestRally = n;
                }
            }
        }

        return bestRally;
    }

    private bool CheckArmyStrength(int playerId, City targetCity)
    {
        int totalArmyStrength = 0;
        foreach (Unit u in unitManager.GetActiveUnits())
        {
            if (u.ownerID == playerId && u.unitClass != UnitClass.Civilian && !u.isHealing)
            {
                totalArmyStrength += (u.meleeStrength + u.rangedStrength);
            }
        }

        float enemyDefensivePower = (targetCity.garrisonStrength * 1.5f) + (targetCity.currentHP / 4f);

        return totalArmyStrength >= enemyDefensivePower;
    }

    private City GetNearestFriendlyCity(Unit unit, HexGrid grid)
    {
        return GetNearestFriendlyCity(unit.CurrentNode, unit.ownerID, grid);
    }

    private City GetNearestFriendlyCity(HexNode fromNode, int playerId, HexGrid grid)
    {
        City nearest = null;
        int minDist = int.MaxValue;
        foreach (City c in cityManager.GetActiveCities())
        {
            if (c.ownerID == playerId)
            {
                int d = grid.GetDistance(fromNode, c.centerNode);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = c;
                }
            }
        }

        return nearest;
    }

    private bool TryAttack(Unit unit, HexGrid grid)
    {
        List<Unit> potentialUnitTargets = new List<Unit>();
        List<City> potentialCityTargets = new List<City>();

        List<HexNode> nodesInRange = grid.GetNodesInRange(unit.CurrentNode, unit.GetEffectiveAttackRange());

        foreach (HexNode node in nodesInRange)
        {
            foreach (Unit u in unitManager.GetUnitsAtNode(node))
            {
                if (IsHostile(unit.ownerID, u.ownerID))
                {
                    potentialUnitTargets.Add(u);
                }
            }

            foreach (City c in cityManager.GetActiveCities())
            {
                if (c.centerNode == node && IsHostile(unit.ownerID, c.ownerID))
                {
                    potentialCityTargets.Add(c);
                }
            }
        }

        Unit bestUnitTarget = null;
        City bestCityTarget = null;
        float bestScore = -9999f;

        foreach (Unit u in potentialUnitTargets)
        {
            float score = 100f - u.currentHP;
            if (u.unitClass == UnitClass.Civilian) score += 50f;

            if (score > bestScore)
            {
                bestScore = score;
                bestUnitTarget = u;
                bestCityTarget = null;
            }
        }

        foreach (City c in potentialCityTargets)
        {
            float score = 150f - (c.currentHP * 0.2f);
            if (score > bestScore)
            {
                bestScore = score;
                bestCityTarget = c;
                bestUnitTarget = null;
            }
        }

        if (bestUnitTarget != null || bestCityTarget != null)
        {
            combatManager.ResolveUnitCombat(unit, bestUnitTarget, bestCityTarget);
            return true;
        }

        return false;
    }

    private HexNode FindBestSettlerDestination(Unit settler, HexGrid grid, bool canSail)
    {
        List<HexNode> candidateNodes = grid.GetNodesInRange(settler.CurrentNode, 12);

        HexNode bestNode = null;
        float highestUtility = -1000f;

        foreach (HexNode node in candidateNodes)
        {
            if (!node.isLand) continue;
            if (node.GetVision(settler.ownerID) == VisionState.Unexplored) continue;

            bool isOccupiedBySameType = false;
            foreach (Unit u in unitManager.GetUnitsAtNode(node))
            {
                if (u != settler && u.ownerID == settler.ownerID && u.unitClass == UnitClass.Civilian)
                {
                    isOccupiedBySameType = true;
                    break;
                }
            }

            if (isOccupiedBySameType) continue;

            float utility = EvaluateSettlerTile(settler, node, grid);

            if (utility > highestUtility)
            {
                highestUtility = utility;
                bestNode = node;
            }
        }

        return bestNode ?? settler.CurrentNode;
    }

    private float EvaluateSettlerTile(Unit settler, HexNode candidateNode, HexGrid grid)
    {
        foreach (City city in cityManager.GetActiveCities())
        {
            if (grid.GetDistance(candidateNode, city.centerNode) <= 3) return -1000f;
        }

        if (candidateNode.hasCity) return -1000f;

        List<HexNode> initialTerritory = grid.GetNodesInRange(candidateNode, 1);
        float yieldScore = 0f;

        foreach (HexNode n in initialTerritory)
        {
            yieldScore += (n.foodYield * 1.5f) + (n.prodYield * 1.2f) + (n.sciYield * 1.0f);
        }

        bool hasWaterNeighbor = false;
        foreach (HexNode neighbor in grid.GetNeighbors(candidateNode))
        {
            if (!neighbor.isLand) hasWaterNeighbor = true;
        }

        if (hasWaterNeighbor) yieldScore += 3f;

        int distance = grid.GetDistance(settler.CurrentNode, candidateNode);
        float distanceScore = 20f / (distance + 1f);

        return yieldScore + distanceScore;
    }

    private bool IsHostile(int id1, int id2)
    {
        if (id1 == id2) return false;

        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm != null)
        {
            int barbId = tm.TotalPlayers - 1;
            if (id1 == barbId || id2 == barbId) return true;
        }

        if (playerManager != null && playerManager.IsAtWar(id1, id2)) return true;
        return false;
    }
}