using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(HexGrid))]
[RequireComponent(typeof(Pathfinder))]
public class HexInteraction : MonoBehaviour
{
    public Tilemap highlightTilemap;
    public Tilemap mainTilemap;
    public TileBase highlightTile;
    public TileBase pathTile;

    public UnitManager unitManager;
    public TurnManager turnManager;
    public CityManager cityManager;
    public PlayerManager playerManager;
    public CombatManager combatManager;

    private HexGrid hexGrid;
    private Pathfinder pathfinder;
    private TechManager techManager;

    private Vector3Int previousHoverPosition = new Vector3Int(-9999, -9999, -9999);
    private HexNode selectedNode;
    private Unit selectedUnit;
    private City selectedCity;
    private List<HexNode> currentPath;

    void Awake()
    {
        hexGrid = GetComponent<HexGrid>();
        pathfinder = GetComponent<Pathfinder>();
        techManager = Object.FindAnyObjectByType<TechManager>();
        combatManager = Object.FindAnyObjectByType<CombatManager>();
        if (unitManager == null) unitManager = Object.FindAnyObjectByType<UnitManager>();
        if (cityManager == null) cityManager = Object.FindAnyObjectByType<CityManager>();
        if (playerManager == null) playerManager = Object.FindAnyObjectByType<PlayerManager>();
        if (turnManager == null) turnManager = Object.FindAnyObjectByType<TurnManager>();
    }

    void Update()
    {
        if (highlightTilemap == null || mainTilemap == null) return;
        HandleHover();
        HandleClick();
        HandleHotkeys();
    }

    void HandleHover()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int cellPosition = mainTilemap.WorldToCell(mousePosition);

        if (cellPosition != previousHoverPosition)
        {
            DrawOverlays(cellPosition);
            previousHoverPosition = cellPosition;
        }
    }

    void HandleClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3Int cellPosition = mainTilemap.WorldToCell(mousePosition);
            HexNode clickedNode = hexGrid.GetNode(cellPosition.y, cellPosition.x);

            if (clickedNode == null) return;
            selectedNode = clickedNode;
            selectedCity = null;
            selectedUnit = null;

            bool foundSomething = false;

            if (cityManager != null)
            {
                foreach (City city in cityManager.GetActiveCities())
                {
                    if (city.centerNode == clickedNode && city.ownerID == turnManager.CurrentPlayerID)
                    {
                        selectedCity = city;
                        string projStr = selectedCity.currentProject != null
                            ? selectedCity.currentProject.name
                            : "None";
                        int reqFood = selectedCity.population * 10 + 10;

                        Debug.Log(
                            $"[UI] Selected City: {selectedCity.cityName} | Pop: {selectedCity.population} | Food: {selectedCity.storedFood}/{reqFood} | Prod: {selectedCity.storedProduction} | Project: {projStr}");
                        foundSomething = true;
                        break;
                    }
                }
            }

            if (unitManager != null && turnManager != null)
            {
                Unit clickedUnit = null;
                List<Unit> unitsAtTile = unitManager.GetUnitsAtNode(clickedNode);

                if (unitsAtTile.Count == 1)
                {
                    clickedUnit = unitsAtTile[0];
                }
                else if (unitsAtTile.Count > 1)
                {
                    float minDistance = float.MaxValue;
                    foreach (Unit u in unitsAtTile)
                    {
                        float dist = Vector2.Distance(mousePosition, u.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            clickedUnit = u;
                        }
                    }
                }

                if (clickedUnit != null && clickedUnit.ownerID == turnManager.CurrentPlayerID)
                {
                    selectedUnit = clickedUnit;
                    selectedCity = null;
                    Debug.Log(
                        $"[UI] Selected Unit: {selectedUnit.unitName} | MP: {selectedUnit.currentMP}/{selectedUnit.maxMP} | HP: {selectedUnit.currentHP}/{selectedUnit.maxHP}");
                    foundSomething = true;
                }
            }

            if (!foundSomething) Debug.Log($"[UI] Selected empty tile [{clickedNode.x},{clickedNode.y}].");

            currentPath = null;
            DrawOverlays(cellPosition);
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3Int cellPosition = mainTilemap.WorldToCell(mousePosition);
            HexNode targetNode = hexGrid.GetNode(cellPosition.y, cellPosition.x);

            if (targetNode == null) return;

            Unit targetUnit = unitManager.GetUnitAtNode(targetNode);
            City targetCity = null;
            if (cityManager != null)
            {
                foreach (City c in cityManager.GetActiveCities())
                    if (c.centerNode == targetNode)
                        targetCity = c;
            }

            bool isEnemyUnit = targetUnit != null && targetUnit.ownerID != turnManager.CurrentPlayerID;
            bool isEnemyCity = targetCity != null && targetCity.ownerID != turnManager.CurrentPlayerID;

            if (selectedUnit != null && !selectedUnit.IsAnimating())
            {
                if ((isEnemyUnit || isEnemyCity) && !selectedUnit.hasAttackedThisTurn &&
                    selectedUnit.unitClass != UnitClass.Civilian)
                {
                    int enemyOwner = isEnemyUnit ? targetUnit.ownerID : targetCity.ownerID;
                    if (playerManager != null && !playerManager.IsAtWar(turnManager.CurrentPlayerID, enemyOwner))
                    {
                        Debug.Log("[UI] Cannot attack in peacetime! Declare war first.");
                        return;
                    }

                    int dist = hexGrid.GetDistance(selectedUnit.CurrentNode, targetNode);
                    if (dist <= selectedUnit.attackRange)
                    {
                        if (combatManager != null)
                            combatManager.ResolveUnitCombat(selectedUnit, targetUnit, targetCity);
                        return;
                    }
                    else
                    {
                        List<HexNode> attackPositions = hexGrid.GetNodesInRange(targetNode, selectedUnit.attackRange);
                        List<HexNode> bestPath = null;

                        bool canSail = techManager != null &&
                                       techManager.HasTech(turnManager.CurrentPlayerID, "Sailing");

                        foreach (HexNode pos in attackPositions)
                        {
                            if (selectedUnit.unitClass == UnitClass.Naval && pos.isLand) continue;
                            if (selectedUnit.unitClass != UnitClass.Naval && !pos.isLand && !canSail) continue;

                            bool canStand = true;
                            foreach (Unit u in unitManager.GetUnitsAtNode(pos))
                            {
                                if (u.ownerID != turnManager.CurrentPlayerID) canStand = false;
                                else if ((u.unitClass == UnitClass.Civilian) ==
                                         (selectedUnit.unitClass == UnitClass.Civilian)) canStand = false;
                            }

                            if (!canStand && pos != selectedUnit.CurrentNode) continue;

                            bool isEnemyCityTile = false;
                            if (cityManager != null)
                            {
                                foreach (City c in cityManager.GetActiveCities())
                                {
                                    if (c.centerNode == pos && c.ownerID != turnManager.CurrentPlayerID)
                                    {
                                        isEnemyCityTile = true;
                                        break;
                                    }
                                }
                            }

                            if (isEnemyCityTile) continue;

                            List<HexNode> path = pathfinder.FindPath(selectedUnit, pos);
                            if (path != null)
                            {
                                if (bestPath == null || path.Count < bestPath.Count)
                                {
                                    bestPath = path;
                                }
                            }
                        }

                        if (bestPath != null && bestPath.Count > 0)
                        {
                            currentPath = bestPath;
                            DrawOverlays(cellPosition);

                            Debug.Log($"[UI] Pathing to attack position. Length: {bestPath.Count}");

                            Unit capturedUnit = selectedUnit;
                            HexNode capturedTarget = targetNode;

                            capturedUnit.SetPath(bestPath, () =>
                            {
                                if (capturedUnit != null && !capturedUnit.hasAttackedThisTurn)
                                {
                                    Unit currentTargetUnit = null;
                                    if (unitManager != null)
                                        currentTargetUnit = unitManager.GetUnitAtNode(capturedTarget);

                                    City currentTargetCity = null;
                                    if (cityManager != null)
                                    {
                                        foreach (City c in cityManager.GetActiveCities())
                                            if (c.centerNode == capturedTarget)
                                                currentTargetCity = c;
                                    }

                                    bool hasEnemy = false;
                                    if (currentTargetUnit != null &&
                                        currentTargetUnit.ownerID != turnManager.CurrentPlayerID) hasEnemy = true;
                                    if (currentTargetCity != null &&
                                        currentTargetCity.ownerID != turnManager.CurrentPlayerID) hasEnemy = true;

                                    if (hasEnemy)
                                    {
                                        int newDist = hexGrid.GetDistance(capturedUnit.CurrentNode, capturedTarget);
                                        if (newDist <= capturedUnit.attackRange)
                                        {
                                            if (combatManager != null)
                                                combatManager.ResolveUnitCombat(capturedUnit, currentTargetUnit,
                                                    currentTargetCity);
                                        }
                                    }
                                }
                            });
                            return;
                        }
                        else
                        {
                            Debug.Log("[UI] No valid path to approach the target.");
                            return;
                        }
                    }
                }

                bool isTargetBlocked = false;
                if (targetNode.GetVision(turnManager.CurrentPlayerID) == VisionState.Visible)
                {
                    foreach (Unit u in unitManager.GetActiveUnits())
                    {
                        if (u == selectedUnit) continue;

                        if (u.ownerID != turnManager.CurrentPlayerID)
                        {
                            if (u.CurrentNode == targetNode) isTargetBlocked = true;
                        }
                        else if ((u.unitClass == UnitClass.Civilian) == (selectedUnit.unitClass == UnitClass.Civilian))
                        {
                            if (u.GetExpectedEndTurnNode() == targetNode) isTargetBlocked = true;
                        }
                    }
                }

                if (isTargetBlocked) return;

                int remOwner = targetNode.GetRememberedOwner(turnManager.CurrentPlayerID);
                if (remOwner != -1 && remOwner != turnManager.CurrentPlayerID)
                {
                    if (playerManager != null && !playerManager.IsAtWar(turnManager.CurrentPlayerID, remOwner))
                    {
                        Debug.Log("[UI] Cannot enter foreign territory in peacetime. Declare war first!");
                        return;
                    }
                }

                currentPath = pathfinder.FindPath(selectedUnit, targetNode);
                DrawOverlays(cellPosition);

                if (currentPath != null && currentPath.Count > 0)
                {
                    Debug.Log($"[UI] Path generated. Length: {currentPath.Count}");
                    selectedUnit.SetPath(currentPath);
                }
                else
                {
                    Debug.Log("[UI] No valid path to target.");
                }
            }
            else if (selectedCity != null && !selectedCity.hasAttackedThisTurn)
            {
                if (isEnemyUnit)
                {
                    int enemyOwner = targetUnit.ownerID;
                    if (playerManager != null && !playerManager.IsAtWar(turnManager.CurrentPlayerID, enemyOwner))
                    {
                        Debug.Log("[UI] City cannot attack in peacetime!");
                        return;
                    }

                    int dist = hexGrid.GetDistance(selectedCity.centerNode, targetNode);
                    if (dist <= selectedCity.attackRange)
                    {
                        if (combatManager != null) combatManager.ResolveCityCombat(selectedCity, targetUnit);
                        return;
                    }
                }
            }
        }
    }

    private void HandleHotkeys()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (playerManager != null)
            {
                if (playerManager.IsAtWar(0, 1)) playerManager.MakePeace(0, 1);
                else playerManager.DeclareWar(0, 1);
            }
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (selectedUnit != null && selectedUnit.currentMP > 0)
            {
                selectedUnit.isHealing = true;
                selectedUnit.isFortified = false;
                selectedUnit.currentMP = 0;
                Debug.Log($"[UI] Unit {selectedUnit.unitName} is resting to heal.");
            }
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (selectedUnit != null && !selectedUnit.IsAnimating())
            {
                if (selectedUnit.isSettler && cityManager != null)
                {
                    if (cityManager.FoundCity(selectedUnit))
                    {
                        unitManager.RemoveUnit(selectedUnit);
                        selectedUnit = null;
                        selectedNode = null;
                        currentPath = null;
                        DrawOverlays(new Vector3Int(-9999, -9999, -9999));
                    }
                }
                else if (!selectedUnit.isSettler && selectedUnit.currentMP > 0)
                {
                    selectedUnit.isFortified = true;
                    selectedUnit.isHealing = false;
                    selectedUnit.currentMP = 0;
                    Debug.Log($"[UI] Unit {selectedUnit.unitName} is fortified (+25% Defense).");
                }
            }
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame && turnManager.CurrentPlayerID == 0)
        {
            if (turnManager != null)
            {
                selectedUnit = null;
                selectedNode = null;
                selectedCity = null;
                currentPath = null;
                DrawOverlays(new Vector3Int(-9999, -9999, -9999));
                turnManager.EndTurn();
            }
        }
    }

    private void DrawOverlays(Vector3Int hoverPos)
    {
        highlightTilemap.ClearAllTiles();

        int startOffset = hexGrid.wrapWorld ? -1 : 0;
        int endOffset = hexGrid.wrapWorld ? 1 : 0;
        int mapWidth = hexGrid.GetWidth();

        if (hexGrid.GetNode(hoverPos.y, hoverPos.x) != null)
            highlightTilemap.SetTile(hoverPos, highlightTile);

        if (selectedNode != null)
        {
            for (int offset = startOffset; offset <= endOffset; offset++)
            {
                highlightTilemap.SetTile(new Vector3Int(selectedNode.y, selectedNode.x + (offset * mapWidth), 0),
                    highlightTile);
            }
        }

        if (currentPath != null)
        {
            foreach (HexNode node in currentPath)
            {
                for (int offset = startOffset; offset <= endOffset; offset++)
                {
                    highlightTilemap.SetTile(new Vector3Int(node.y, node.x + (offset * mapWidth), 0), pathTile);
                }
            }
        }
    }
}