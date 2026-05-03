using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TechTreeUIManager : MonoBehaviour
{
    [Header("UI References")] public GameObject techTreePanel;
    public RectTransform contentContainer;

    [Header("Prefabs")] public GameObject nodePrefab;
    public GameObject linePrefab;

    [Header("Layout Settings")] public float xSpacing = 300f;
    public float ySpacing = 150f;
    public Vector2 nodeScale = new Vector2(0.8f, 0.8f);

    [Header("Zoom Settings")] public float minZoom = 0.4f;
    public float maxZoom = 1.5f;
    public float zoomSensitivity = 0.1f;
    private float currentZoom = 1f;

    private TechManager techManager;
    private TurnManager turnManager;

    private Dictionary<string, TechNodeUI> spawnedNodes = new Dictionary<string, TechNodeUI>();

    void Start()
    {
        techManager = FindAnyObjectByType<TechManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
        if (techTreePanel != null) techTreePanel.SetActive(false);
    }

    void Update()
    {
        if (techTreePanel.activeSelf)
        {
            HandleZoom();
        }
    }

    private void HandleZoom()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            float scrollDirection = 0f;
            if (scroll > 0) scrollDirection = 1f;
            else if (scroll < 0) scrollDirection = -1f;

            if (scrollDirection != 0)
            {
                currentZoom = Mathf.Clamp(currentZoom + scrollDirection * zoomSensitivity, minZoom, maxZoom);
                contentContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
            }
        }
    }

    public void ToggleTechTree()
    {
        bool isActive = techTreePanel.activeSelf;
        techTreePanel.SetActive(!isActive);

        if (!isActive)
        {
            GenerateTree();
        }
    }

    public void GenerateTree()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        spawnedNodes.Clear();

        List<string> allTechIds = techManager.GetAllTechIds();

        Dictionary<string, int> techTiers = new Dictionary<string, int>();
        foreach (string techId in allTechIds)
        {
            CalculateTier(techId, techTiers);
        }

        Dictionary<int, List<string>> tierGroups = new Dictionary<int, List<string>>();
        foreach (var kvp in techTiers)
        {
            if (!tierGroups.ContainsKey(kvp.Value)) tierGroups[kvp.Value] = new List<string>();
            tierGroups[kvp.Value].Add(kvp.Key);
        }

        float maxMaxWidth = 0;
        float maxMaxHeight = 0;

        foreach (var kvp in tierGroups)
        {
            int tier = kvp.Key;
            List<string> techsInTier = kvp.Value;

            for (int i = 0; i < techsInTier.Count; i++)
            {
                string techId = techsInTier[i];
                GameObject nodeObj = Instantiate(nodePrefab, contentContainer);
                RectTransform rect = nodeObj.GetComponent<RectTransform>();

                rect.localScale = new Vector3(nodeScale.x, nodeScale.y, 1f);

                float xPos = tier * xSpacing;
                float yPos = -(i * ySpacing) + ((techsInTier.Count - 1) * ySpacing / 2f);
                rect.anchoredPosition = new Vector2(xPos, yPos);

                if (xPos + xSpacing > maxMaxWidth) maxMaxWidth = xPos + xSpacing;
                float absoluteY = Mathf.Abs(yPos) + ySpacing;
                if (absoluteY > maxMaxHeight) maxMaxHeight = absoluteY;

                TechNodeUI nodeUI = nodeObj.GetComponent<TechNodeUI>();
                nodeUI.Initialize(techId, techManager.GetTechName(techId), techManager.GetTechCost(techId), this);
                spawnedNodes[techId] = nodeUI;
            }
        }

        contentContainer.sizeDelta = new Vector2(maxMaxWidth, maxMaxHeight * 2);

        DrawConnections();
        RefreshAllNodes();
    }

    private int CalculateTier(string techId, Dictionary<string, int> tiers)
    {
        if (tiers.ContainsKey(techId)) return tiers[techId];

        List<string> prereqs = techManager.GetPrerequisites(techId);
        if (prereqs == null || prereqs.Count == 0)
        {
            tiers[techId] = 0;
            return 0;
        }

        int maxPrereqTier = 0;
        foreach (string prereq in prereqs)
        {
            int prereqTier = CalculateTier(prereq, tiers);
            if (prereqTier > maxPrereqTier) maxPrereqTier = prereqTier;
        }

        tiers[techId] = maxPrereqTier + 1;
        return tiers[techId];
    }

    private void DrawConnections()
    {
        foreach (var kvp in spawnedNodes)
        {
            string techId = kvp.Key;
            TechNodeUI targetNode = kvp.Value;
            List<string> prereqs = techManager.GetPrerequisites(techId);

            foreach (string prereq in prereqs)
            {
                if (spawnedNodes.TryGetValue(prereq, out TechNodeUI sourceNode))
                {
                    DrawLine(sourceNode.GetComponent<RectTransform>(), targetNode.GetComponent<RectTransform>());
                }
            }
        }
    }

    private void DrawLine(RectTransform start, RectTransform end)
    {
        GameObject lineObj = Instantiate(linePrefab, contentContainer);
        lineObj.transform.SetAsFirstSibling();
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();

        Vector2 startPos = start.anchoredPosition;
        Vector2 endPos = end.anchoredPosition;
        Vector2 dir = endPos - startPos;

        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRect.sizeDelta = new Vector2(distance, 6f);
        lineRect.anchoredPosition = startPos;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    public void OnTechNodeSelected(string techId)
    {
        Debug.Log($"[TechTree] Спроба вивчити: {techId}");
        int playerId = turnManager.CurrentPlayerID;
        techManager.SetResearch(playerId, techId);
        RefreshAllNodes();
    }

    public void RefreshAllNodes()
    {
        int playerId = turnManager.CurrentPlayerID;
        string currentResearch = techManager.GetCurrentResearch(playerId);

        foreach (var kvp in spawnedNodes)
        {
            string techId = kvp.Key;
            bool isUnlocked = techManager.HasTech(playerId, techId);
            bool canResearch = techManager.CanResearch(playerId, techId);
            bool isResearching = (techId == currentResearch);

            kvp.Value.UpdateVisualState(isUnlocked, isResearching, canResearch);
        }
    }

    public void ZoomIn()
    {
        currentZoom = Mathf.Clamp(currentZoom + zoomSensitivity, minZoom, maxZoom);
        contentContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
    }

    public void ZoomOut()
    {
        currentZoom = Mathf.Clamp(currentZoom - zoomSensitivity, minZoom, maxZoom);
        contentContainer.localScale = new Vector3(currentZoom, currentZoom, 1f);
    }
}