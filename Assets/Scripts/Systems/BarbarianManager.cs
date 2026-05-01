using UnityEngine;
using System.Collections.Generic;

public class BarbarianManager : MonoBehaviour
{
    public HexGrid grid;
    public UnitManager unitManager;
    public TurnManager turnManager;

    [Header("Barbarian Settings")] [Tooltip("Як часто (у ходах) відбуватиметься спавн")]
    public int spawnInterval = 5;

    public int maxBarbariansActive = 12;

    [Tooltip("Радіус перевірки слабких місць")]
    public float weaknessRadius = 4f;

    private int BarbarianID => turnManager.TotalPlayers - 1;

    void Start()
    {
        if (turnManager != null)
            turnManager.OnTurnEnded += CheckForSpawns;
    }

    private void CheckForSpawns()
    {
        if (turnManager.CurrentTurn % spawnInterval != 0) return;

        int currentCount = 0;
        foreach (Unit u in unitManager.GetActiveUnits())
        {
            if (u.ownerID == BarbarianID) currentCount++;
        }

        if (currentCount >= maxBarbariansActive) return;

        int spawns = Random.Range(1, 4);
        for (int i = 0; i < spawns; i++)
        {
            HexNode spawnNode = FindWeakSpot();
            if (spawnNode != null)
            {
                string[] types = { "Warrior", "Archer", "Horseman", "Chariot Archer", "Spearman", "Scout" };
                string type = types[Random.Range(0, types.Length)];

                unitManager.SpawnUnit(spawnNode, BarbarianID, type);
                Debug.Log($"[BARBARIAN] Spawned {type} at [{spawnNode.x},{spawnNode.y}]");
            }
        }
    }

    private HexNode FindWeakSpot()
    {
        List<HexNode> candidates = new List<HexNode>();
        Dictionary<HexNode, float> weaknessScores = new Dictionary<HexNode, float>();

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                HexNode node = grid.GetNode(x, y);

                if (node == null || !node.isLand || node.ownerID != -1 || grid.IsNodeOccupied(node))
                    continue;

                bool nearBorder = false;
                foreach (HexNode n in grid.GetNeighbors(node))
                {
                    if (n.ownerID != -1) nearBorder = true;
                }

                if (nearBorder) continue;

                candidates.Add(node);
                weaknessScores[node] = CalculateWeakness(node);
            }
        }

        if (candidates.Count == 0) return null;

        candidates.Sort((a, b) => weaknessScores[b].CompareTo(weaknessScores[a]));

        int topCount = Mathf.Max(1, candidates.Count / 10);
        return candidates[Random.Range(0, topCount)];
    }

    private float CalculateWeakness(HexNode node)
    {
        float militaryPresence = 0f;
        List<HexNode> nearby = grid.GetNodesInRange(node, Mathf.RoundToInt(weaknessRadius));

        foreach (HexNode n in nearby)
        {
            foreach (Unit u in unitManager.GetUnitsAtNode(n))
            {
                if (u.ownerID != BarbarianID && u.unitClass != UnitClass.Civilian)
                {
                    militaryPresence += (u.meleeStrength + u.rangedStrength);
                }
            }

            if (n.hasCity)
            {
                militaryPresence += 20f;
            }
        }

        return 1000f - militaryPresence;
    }

    void OnDestroy()
    {
        if (turnManager != null) turnManager.OnTurnEnded -= CheckForSpawns;
    }
}