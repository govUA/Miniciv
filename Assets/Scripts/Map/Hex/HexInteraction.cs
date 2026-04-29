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
                        ResolveUnitCombat(selectedUnit, targetUnit, targetCity);
                        return;
                    }
                    else
                    {
                        List<HexNode> attackPositions = hexGrid.GetNodesInRange(targetNode, selectedUnit.attackRange);
                        List<HexNode> bestPath = null;

                        foreach (HexNode pos in attackPositions)
                        {
                            if (!pos.isLand) continue;

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
                                            ResolveUnitCombat(capturedUnit, currentTargetUnit, currentTargetCity);
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
                        ResolveCityCombat(selectedCity, targetUnit);
                        return;
                    }
                }
            }
        }
    }

    private void ResolveUnitCombat(Unit attacker, Unit defUnit, City defCity)
    {
        attacker.hasAttackedThisTurn = true;
        attacker.currentMP = 0;
        attacker.isFortified = false;
        attacker.isHealing = false;

        float attModifier = 1.05f;
        if (attacker.CurrentNode.terrainType == TerrainType.Mountain) attModifier += 0.10f;

        float attStrength = (attacker.unitClass == UnitClass.Melee ? attacker.meleeStrength : attacker.rangedStrength) *
                            attModifier;

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

                float defStrength = defUnit.meleeStrength * defModifier;

                float rngHit = Random.Range(0.85f, 1.15f);
                int dmgToDef = Mathf.RoundToInt(30f * (attStrength / defStrength) * rngHit);
                defUnit.TakeDamage(dmgToDef);

                if (attacker.unitClass == UnitClass.Melee && defUnit.currentHP > 0)
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
                    List<HexNode> capturePath =
                        pathfinder.FindPath(attacker, defCity.centerNode);
                    if (capturePath != null && capturePath.Count > 0)
                    {
                        attacker.currentMP += 99;
                        attacker.SetPath(capturePath, () => { attacker.currentMP = 0; });
                    }
                }
            }
            else
            {
                if (attacker.unitClass == UnitClass.Melee && defCity.ownerID != attacker.ownerID)
                {
                    float rngRet = Random.Range(0.85f, 1.15f);
                    int dmgToAtt = Mathf.RoundToInt(30f * ((float)defCity.garrisonStrength / attStrength) * rngRet);
                    attacker.TakeDamage(dmgToAtt);
                }
            }
        }
    }

    private void ResolveCityCombat(City attacker, Unit defUnit)
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

        if (Keyboard.current.enterKey.wasPressedThisFrame)
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