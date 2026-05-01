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
    Ranged,
    Cavalry,
    AntiCavalry
}

public class Unit : MonoBehaviour
{
    public static event Action<Unit> OnUnitMoved;
    public static event Action<Unit> OnUnitDied;

    public HexNode CurrentNode { get; private set; }
    public HexNode previousNode { get; private set; }
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
    public bool isSettler => unitClass == UnitClass.Civilian && unitName.ToLower() == "settler";
    public bool isFortified = false;
    public bool isHealing = false;
    public SpriteRenderer factionIconRenderer;

    private Queue<HexNode> pathQueue = new Queue<HexNode>();
    private Tilemap tilemap;
    private TurnManager turnManager;
    private HexGrid hexGrid;
    private TechManager techManager;
    private PlayerManager playerManager;
    private UnitManager unitManager;

    public bool isAnimating = false;
    public event Action onDestinationReached;

    public void Initialize(HexNode startNode, Vector3 worldPosition, Tilemap map, TurnManager tm, int playerId,
        UnitDataModel data, HexGrid grid, Sprite mainSprite = null, Sprite iconSprite = null)
    {
        CurrentNode = startNode;
        previousNode = startNode;
        transform.position = worldPosition;
        tilemap = map;
        turnManager = tm;
        ownerID = playerId;
        unitName = data.name;
        hexGrid = grid;

        techManager = UnityEngine.Object.FindAnyObjectByType<TechManager>();
        playerManager = UnityEngine.Object.FindAnyObjectByType<PlayerManager>();
        unitManager = UnityEngine.Object.FindAnyObjectByType<UnitManager>();

        if (System.Enum.TryParse(data.unitClass, out UnitClass parsedClass))
        {
            unitClass = parsedClass;
        }
        else
        {
            unitClass = UnitClass.Melee;
        }

        meleeStrength = data.meleeStrength;
        rangedStrength = data.rangedStrength;
        attackRange = data.attackRange;
        visionRange = data.visionRange;
        maxMP = data.maxMP;

        currentHP = maxHP;
        currentMP = maxMP;
        State = UnitState.Idle;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && mainSprite != null) sr.sprite = mainSprite;

        if (factionIconRenderer != null && iconSprite != null)
        {
            factionIconRenderer.sprite = iconSprite;
            if (sr != null) factionIconRenderer.sortingOrder = sr.sortingOrder + 1;
        }

        if (playerManager != null && factionIconRenderer != null)
        {
            PlayerData pd = playerManager.GetPlayer(ownerID);
            if (pd != null)
            {
                Material mat = new Material(factionIconRenderer.material);
                mat.SetColor("_ColorWhite", pd.primaryColor);
                mat.SetColor("_ColorBlack", pd.secondaryColor);
                factionIconRenderer.material = mat;
            }
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
        if (currentHP <= 0) Die();
    }

    public void Die()
    {
        OnUnitDied?.Invoke(this);
        if (unitManager != null) unitManager.RemoveUnit(this);
    }

    private void HandleNewTurn()
    {
        currentMP = maxMP;
        hasAttackedThisTurn = false;
        if (isHealing)
        {
            int healAmount = (CurrentNode.ownerID == ownerID) ? 20 : 10;
            currentHP = Mathf.Clamp(currentHP + healAmount, 0, maxHP);
            if (currentHP == maxHP) isHealing = false;
        }

        if (pathQueue.Count > 0) ProcessMovement();
        else State = UnitState.Idle;
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
            if (!CanReachSafeTileThisTurn())
            {
                pathQueue.Clear();
                break;
            }

            HexNode nextNode = pathQueue.Peek();

            bool hasEnemy = false;
            if (unitManager != null)
            {
                foreach (Unit u in unitManager.GetUnitsAtNode(nextNode))
                {
                    if (u.ownerID != ownerID) hasEnemy = true;
                }
            }

            bool isForeign = nextNode.ownerID != -1 && nextNode.ownerID != ownerID;
            bool isAtWar = isForeign && playerManager != null && playerManager.IsAtWar(ownerID, nextNode.ownerID);
            bool canSail = techManager != null && techManager.HasTech(ownerID, "Sailing");

            if ((!nextNode.isLand && !canSail) || hasEnemy || (isForeign && !isAtWar))
            {
                pathQueue.Clear();
                break;
            }

            int cost = (int)nextNode.movementCost;
            if (!CurrentNode.isLand && !nextNode.isLand) cost = 10;
            else if (CurrentNode.isLand != nextNode.isLand) cost = 20;

            if (currentMP >= cost)
            {
                pathQueue.Dequeue();
                currentMP -= cost;
                int dx = nextNode.x - CurrentNode.x;
                if (hexGrid.wrapWorld && Mathf.Abs(dx) > hexGrid.GetWidth() / 2)
                    dx = (dx > 0) ? dx - hexGrid.GetWidth() : dx + hexGrid.GetWidth();

                Vector3 targetPos =
                    tilemap.CellToWorld(new Vector3Int(nextNode.y, tilemap.WorldToCell(transform.position).y + dx, 0));

                while (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }

                transform.position = targetPos;
                previousNode = CurrentNode;
                CurrentNode = nextNode;
                OnUnitMoved?.Invoke(this);
            }
            else break;
        }

        isAnimating = false;
        State = pathQueue.Count > 0 ? UnitState.OutOfMovement : UnitState.Idle;
        if (pathQueue.Count == 0) onDestinationReached?.Invoke();
    }

    public void SetVisibility(bool isVisible)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = isVisible;
    }

    public bool IsAnimating() => isAnimating;

    public void MoveAlongPath(List<HexNode> newPath)
    {
        pathQueue = new Queue<HexNode>(newPath);
        State = UnitState.Moving;
        StartCoroutine(FollowPathRoutine());
    }

    private IEnumerator FollowPathRoutine()
    {
        isAnimating = true;

        while (pathQueue.Count > 0 && currentMP > 0)
        {
            if (!CanReachSafeTileThisTurn())
            {
                pathQueue.Clear();
                break;
            }

            HexNode nextNode = pathQueue.Peek();

            bool hasEnemy = false;
            if (unitManager != null)
            {
                foreach (Unit u in unitManager.GetUnitsAtNode(nextNode))
                {
                    if (u.ownerID != ownerID) hasEnemy = true;
                }
            }

            bool isForeign = nextNode.ownerID != -1 && nextNode.ownerID != ownerID;
            bool isAtWar = isForeign && playerManager != null && playerManager.IsAtWar(ownerID, nextNode.ownerID);
            bool canSail = techManager != null && techManager.HasTech(ownerID, "Sailing");

            if ((!nextNode.isLand && !canSail) || hasEnemy || (isForeign && !isAtWar))
            {
                pathQueue.Clear();
                break;
            }

            int cost = (int)nextNode.movementCost;
            if (!CurrentNode.isLand && !nextNode.isLand) cost = 10;
            else if (CurrentNode.isLand != nextNode.isLand) cost = 20;

            if (currentMP >= cost)
            {
                pathQueue.Dequeue();
                currentMP -= cost;

                int dx = nextNode.x - CurrentNode.x;
                if (hexGrid.wrapWorld && Mathf.Abs(dx) > hexGrid.GetWidth() / 2)
                    dx = (dx > 0) ? dx - hexGrid.GetWidth() : dx + hexGrid.GetWidth();

                Vector3 targetPos =
                    tilemap.CellToWorld(new Vector3Int(nextNode.y, tilemap.WorldToCell(transform.position).y + dx, 0));

                while (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }

                transform.position = targetPos;
                previousNode = CurrentNode;
                CurrentNode = nextNode;
                OnUnitMoved?.Invoke(this);
            }
            else
            {
                break;
            }
        }

        isAnimating = false;
        State = pathQueue.Count > 0 ? UnitState.OutOfMovement : UnitState.Idle;
        if (pathQueue.Count == 0) onDestinationReached?.Invoke();
    }

    private bool CanReachSafeTileThisTurn()
    {
        int simulatedMP = currentMP;
        HexNode simCurrentNode = CurrentNode;

        foreach (HexNode node in pathQueue)
        {
            int cost = (int)node.movementCost;
            if (!simCurrentNode.isLand && !node.isLand) cost = 10;
            else if (simCurrentNode.isLand != node.isLand) cost = 20;

            if (simulatedMP >= cost)
            {
                simulatedMP -= cost;
                simCurrentNode = node;

                bool hasSameTypeFriendly = false;
                bool hasEnemy = false;

                if (unitManager != null)
                {
                    foreach (Unit u in unitManager.GetActiveUnits())
                    {
                        if (u == this) continue;

                        if (u.ownerID != ownerID)
                        {
                            if (u.CurrentNode == node && node.GetVision(ownerID) == VisionState.Visible)
                                hasEnemy = true;
                        }
                        else if ((u.unitClass == UnitClass.Civilian) == (unitClass == UnitClass.Civilian))
                        {
                            if (u.GetExpectedEndTurnNode() == node)
                            {
                                hasSameTypeFriendly = true;
                            }
                        }
                    }
                }

                if (hasEnemy) return false;

                if (!hasSameTypeFriendly)
                {
                    return true;
                }
            }
            else
            {
                if (simCurrentNode == CurrentNode) return true;
                return false;
            }
        }

        return false;
    }

    public HexNode GetExpectedEndTurnNode()
    {
        if (pathQueue == null || pathQueue.Count == 0) return CurrentNode;

        int simulatedMP = currentMP;
        HexNode simNode = CurrentNode;

        foreach (HexNode node in pathQueue)
        {
            int cost = (int)node.movementCost;
            if (!simNode.isLand && !node.isLand) cost = 10;
            else if (simNode.isLand != node.isLand) cost = 20;

            if (simulatedMP >= cost)
            {
                simulatedMP -= cost;
                simNode = node;
            }
            else
            {
                break;
            }
        }

        return simNode;
    }
}