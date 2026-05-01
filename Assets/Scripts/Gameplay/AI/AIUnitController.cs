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

        List<Unit> allUnits = new List<Unit>(unitManager.GetActiveUnits());
        Pathfinder pathfinder = FindObjectOfType<Pathfinder>();
        HexGrid grid = FindObjectOfType<HexGrid>();

        foreach (Unit unit in allUnits)
        {
            if (unit == null) continue;
            if (unit.ownerID != playerId || unit.State != UnitState.Idle || unit.currentMP <= 0) continue;

            bool attacked = false;
            if (unit.unitClass != UnitClass.Civilian && !unit.hasAttackedThisTurn)
            {
                attacked = TryAttack(unit, grid);
            }

            if (attacked) continue;

            HexNode targetNode = EvaluateBestDestination(unit, grid, canSail);

            if (targetNode != null)
            {
                if (targetNode == unit.CurrentNode)
                {
                    if (unit.isSettler)
                    {
                        if (cityManager.FoundCity(unit))
                        {
                            unitManager.RemoveUnit(unit);
                        }
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

    private HexNode EvaluateBestDestination(Unit unit, HexGrid grid, bool canSail)
    {
        HexNode bestNode = null;
        float highestUtility = -1000f;

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                HexNode node = grid.GetNode(x, y);

                if (node == null) continue;

                if (unit.unitClass == UnitClass.Naval && node.isLand) continue;
                if (unit.unitClass != UnitClass.Naval && !node.isLand && (!canSail || unit.isSettler)) continue;

                bool isOccupiedBySameType = false;
                foreach (Unit u in unitManager.GetUnitsAtNode(node))
                {
                    if (u != unit && u.ownerID == unit.ownerID &&
                        (u.unitClass == UnitClass.Civilian) == (unit.unitClass == UnitClass.Civilian))
                    {
                        isOccupiedBySameType = true;
                        break;
                    }
                }

                if (isOccupiedBySameType) continue;

                float utility = 0f;

                if (unit.isSettler)
                {
                    utility = EvaluateSettlerTile(unit, node, grid);
                }
                else
                {
                    utility = EvaluateMilitaryTile(unit, node, grid);
                }

                if (utility > highestUtility)
                {
                    highestUtility = utility;
                    bestNode = node;
                }
            }
        }

        return bestNode ?? unit.CurrentNode;
    }

    private float EvaluateMilitaryTile(Unit unit, HexNode candidateNode, HexGrid grid)
    {
        float score = 0f;
        VisionState vision = candidateNode.GetVision(unit.ownerID);

        TurnManager tm = FindObjectOfType<TurnManager>();
        bool isBarbarian = (tm != null && unit.ownerID == tm.TotalPlayers - 1);

        float minDistToEnemy = 999f;

        foreach (Unit u in unitManager.GetActiveUnits())
        {
            if (IsHostile(unit.ownerID, u.ownerID))
            {
                if (isBarbarian || u.CurrentNode.GetVision(unit.ownerID) == VisionState.Visible)
                {
                    float d = grid.GetDistance(candidateNode, u.CurrentNode);
                    if (d < minDistToEnemy) minDistToEnemy = d;
                }
            }
        }

        foreach (City c in cityManager.GetActiveCities())
        {
            if (IsHostile(unit.ownerID, c.ownerID))
            {
                if (isBarbarian || c.centerNode.GetVision(unit.ownerID) != VisionState.Unexplored)
                {
                    float d = grid.GetDistance(candidateNode, c.centerNode);
                    if (d < minDistToEnemy) minDistToEnemy = d;
                }
            }
        }

        int attackRange = unit.GetEffectiveAttackRange();

        if (minDistToEnemy < 999f)
        {
            if (minDistToEnemy <= attackRange)
                score += 2000f;
            else
                score += 1000f - (minDistToEnemy * 10f);
        }
        else
        {
            if (vision == VisionState.Unexplored) score += 300f;

            float minDistToOwnCity = 999f;
            foreach (City c in cityManager.GetActiveCities())
            {
                if (c.ownerID == unit.ownerID)
                {
                    float d = grid.GetDistance(candidateNode, c.centerNode);
                    if (d < minDistToOwnCity) minDistToOwnCity = d;
                }
            }

            if (minDistToOwnCity < 999f)
            {
                if (minDistToOwnCity <= 2) score += 500f;
                else score += 400f - (minDistToOwnCity * 5f);
            }
        }

        int distance = grid.GetDistance(unit.CurrentNode, candidateNode);
        float distancePenalty = distance * 2f;

        float randomBonus = Random.Range(0f, 2f);

        return score - distancePenalty + randomBonus;
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