using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public enum UnitState
{
    Idle,
    Moving,
    OutOfMovement
}

public class Unit : MonoBehaviour
{
    public HexNode CurrentNode { get; private set; }
    public float moveSpeed = 5f;
    public int maxMP = 30;
    public int currentMP;

    public UnitState State { get; private set; }

    private Queue<HexNode> pathQueue = new Queue<HexNode>();
    private Tilemap tilemap;
    private TurnManager turnManager;
    private bool isAnimating = false;

    public void Initialize(HexNode startNode, Vector3 worldPosition, Tilemap map, TurnManager tm)
    {
        CurrentNode = startNode;
        transform.position = worldPosition;
        tilemap = map;
        turnManager = tm;

        currentMP = maxMP;
        State = UnitState.Idle;

        if (turnManager != null)
        {
            turnManager.OnTurnEnded += HandleNewTurn;
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnEnded -= HandleNewTurn;
        }
    }

    public void SetPath(List<HexNode> path)
    {
        pathQueue.Clear();
        foreach (var node in path)
        {
            pathQueue.Enqueue(node);
        }

        ProcessMovement();
    }

    private void HandleNewTurn()
    {
        currentMP = maxMP;

        if (pathQueue.Count > 0)
        {
            ProcessMovement();
        }
        else
        {
            State = UnitState.Idle;
        }
    }

    private void ProcessMovement()
    {
        if (isAnimating) return;
        StartCoroutine(MovementRoutine());
    }

    private IEnumerator MovementRoutine()
    {
        isAnimating = true;
        State = UnitState.Moving;

        while (pathQueue.Count > 0 && currentMP > 0)
        {
            HexNode nextNode = pathQueue.Peek();
            int cost = (int)nextNode.movementCost;

            if (currentMP >= cost)
            {
                pathQueue.Dequeue();
                currentMP -= cost;

                Vector3 targetPos = tilemap.CellToWorld(new Vector3Int(nextNode.y, nextNode.x, 0));

                while (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }

                transform.position = targetPos;
                CurrentNode = nextNode;
            }
            else
            {
                break;
            }
        }

        isAnimating = false;

        if (pathQueue.Count > 0)
        {
            State = UnitState.OutOfMovement;
            Debug.Log("Unit out of MP. Remaining path: " + pathQueue.Count + " nodes.");
        }
        else
        {
            State = UnitState.Idle;
            Debug.Log("Unit reached destination.");
        }
    }

    public bool IsAnimating() => isAnimating;
}