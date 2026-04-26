using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    public HexNode CurrentNode { get; private set; }
    public float moveSpeed = 5f;

    private bool isMoving = false;

    public void Initialize(HexNode startNode, Vector3 worldPosition)
    {
        CurrentNode = startNode;
        transform.position = worldPosition;
    }

    public void MoveAlongPath(List<HexNode> path, Tilemap tilemap)
    {
        if (isMoving) return;
        StartCoroutine(FollowPath(path, tilemap));
    }

    private IEnumerator FollowPath(List<HexNode> path, Tilemap tilemap)
    {
        isMoving = true;

        foreach (HexNode node in path)
        {
            Vector3 targetPos = tilemap.CellToWorld(new Vector3Int(node.y, node.x, 0));

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
            CurrentNode = node;
        }

        isMoving = false;
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}