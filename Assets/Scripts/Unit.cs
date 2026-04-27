using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections;
using System.Collections.Generic;

public enum UnitState
{
    Idle,
    Moving,
    OutOfMovement
}

public enum UnitClass
{
    Civilian,
    Melee,
    Ranged
}

public class Unit : MonoBehaviour
{
    public static event Action<Unit> OnUnitMoved;
    public static event Action<Unit> OnUnitDied;

    public HexNode CurrentNode { get; private set; }
    public string unitName;
    public int ownerID;

    public UnitClass unitClass;
    public int maxHP = 100;
    public int currentHP;
    public int meleeStrength;
    public int rangedStrength;
    public int attackRange;
    public int visionRange = 2;

    public float moveSpeed = 5f;
    public int maxMP = 30;
    public int currentMP;
    public bool hasAttackedThisTurn = false;

    public UnitState State { get; private set; }
    public bool isSettler => unitClass == UnitClass.Civilian && unitName == "Settler";
    public bool isFortified = false;
    public bool isHealing = false;

    private Queue<HexNode> pathQueue = new Queue<HexNode>();
    private Tilemap tilemap;
    private TurnManager turnManager;
    private HexGrid hexGrid;
    private bool isAnimating = false;
    private Action onDestinationReached;

    public void Initialize(HexNode startNode, Vector3 worldPosition, Tilemap map, TurnManager tm, int playerId,
        string name, HexGrid grid)
    {
        CurrentNode = startNode;
        transform.position = worldPosition;
        tilemap = map;
        turnManager = tm;
        ownerID = playerId;
        unitName = name;
        hexGrid = grid;

        currentHP = maxHP;
        currentMP = maxMP;
        State = UnitState.Idle;

        if (unitName == "Settler" || unitName == "Builder")
        {
            unitClass = UnitClass.Civilian;
            meleeStrength = 0;
            rangedStrength = 0;
            attackRange = 0;
        }
        else if (unitName == "Warrior")
        {
            unitClass = UnitClass.Melee;
            meleeStrength = 20;
            rangedStrength = 0;
            attackRange = 1;
        }
        else if (unitName == "Archer")
        {
            unitClass = UnitClass.Ranged;
            meleeStrength = 10;
            rangedStrength = 25;
            attackRange = 2;
        }
        else
        {
            unitClass = UnitClass.Civilian;
            meleeStrength = 0;
            rangedStrength = 0;
            attackRange = 0;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (unitClass == UnitClass.Civilian)
                sr.color = (ownerID == 0) ? Color.cyan : new Color(1f, 0.5f, 0f);
            else
                sr.color = (ownerID == 0) ? Color.blue : Color.red;
        }

        if (turnManager != null) turnManager.OnTurnEnded += HandleNewTurn;
        OnUnitMoved?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (turnManager != null) turnManager.OnTurnEnded -= HandleNewTurn;
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        Debug.Log($"[COMBAT] {unitName} (Player {ownerID}) took {amount} damage. HP: {currentHP}/{maxHP}");
        if (currentHP <= 0) Die();
    }

    public void Die()
    {
        Debug.Log($"[COMBAT] {unitName} (Player {ownerID}) died!");
        OnUnitDied?.Invoke(this);
        UnitManager um = UnityEngine.Object.FindAnyObjectByType<UnitManager>();
        if (um != null) um.RemoveUnit(this);
    }

    private void HandleNewTurn()
    {
        currentMP = maxMP;
        hasAttackedThisTurn = false;

        if (isHealing)
        {
            int healAmount = (CurrentNode.ownerID == ownerID) ? 20 : 10;
            currentHP = Mathf.Clamp(currentHP + healAmount, 0, maxHP);
            Debug.Log($"[UNIT] {unitName} healed for {healAmount}. HP: {currentHP}/{maxHP}");
            if (currentHP == maxHP) isHealing = false;
        }

        if (pathQueue.Count > 0)
        {
            Debug.Log($"[UNIT] {unitName} auto-resuming movement. Remaining path: {pathQueue.Count}");
            ProcessMovement();
        }
        else
        {
            State = UnitState.Idle;
        }
    }

    public void SetPath(List<HexNode> path, Action onComplete = null)
    {
        isFortified = false;
        isHealing = false;

        pathQueue.Clear();
        foreach (var node in path) pathQueue.Enqueue(node);

        onDestinationReached = onComplete;

        ProcessMovement();
    }

    private void ProcessMovement()
    {
        if (isAnimating || hasAttackedThisTurn) return;
        StartCoroutine(MovementRoutine());
    }

    private IEnumerator MovementRoutine()
    {
        isAnimating = true;
        State = UnitState.Moving;

        while (pathQueue.Count > 0 && currentMP > 0)
        {
            HexNode nextNode = pathQueue.Peek();

            PlayerManager pm = UnityEngine.Object.FindAnyObjectByType<PlayerManager>();
            bool isForeignTerritory = nextNode.ownerID != -1 && nextNode.ownerID != this.ownerID;
            bool isAtWar = isForeignTerritory && pm != null && pm.IsAtWar(this.ownerID, nextNode.ownerID);

            if (!nextNode.isLand ||
                UnityEngine.Object.FindAnyObjectByType<UnitManager>().GetUnitAtNode(nextNode) != null ||
                (isForeignTerritory && !isAtWar))
            {
                Debug.Log($"[UNIT] {unitName} path blocked! (Obstacle or borders)");
                pathQueue.Clear();
                onDestinationReached = null;
                break;
            }

            int cost = (int)nextNode.movementCost;
            if (currentMP >= cost)
            {
                pathQueue.Dequeue();
                currentMP -= cost;

                int dx = nextNode.x - CurrentNode.x;
                int width = hexGrid.GetWidth();
                if (hexGrid.wrapWorld)
                {
                    if (dx > width / 2) dx -= width;
                    else if (dx < -width / 2) dx += width;
                }

                Vector3Int currentVisualCell = tilemap.WorldToCell(transform.position);
                int targetVisualX = currentVisualCell.y + dx;
                int targetVisualY = nextNode.y;

                Vector3 targetPos = tilemap.CellToWorld(new Vector3Int(targetVisualY, targetVisualX, 0));

                while (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }

                transform.position = targetPos;
                CurrentNode = nextNode;
                OnUnitMoved?.Invoke(this);
            }
            else
            {
                Debug.Log($"[UNIT] {unitName} out of MP. Will continue next turn.");
                break;
            }
        }

        isAnimating = false;
        State = pathQueue.Count > 0 ? UnitState.OutOfMovement : UnitState.Idle;

        if (pathQueue.Count == 0 && onDestinationReached != null)
        {
            Action callback = onDestinationReached;
            onDestinationReached = null;
            callback.Invoke();
        }
    }

    public void SetVisibility(bool isVisible)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = isVisible;
    }

    public bool IsAnimating() => isAnimating;
}