using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DiplomacyListUIManager : MonoBehaviour
{
    [Header("UI Panels")] public GameObject listPanel;
    public Transform contentContainer;
    public GameObject factionButtonPrefab;
    public Button closeButton;

    [Header("Dependencies")] public DiplomacyWindowUIManager diplomacyWindow;

    private PlayerManager playerManager;
    private TurnManager turnManager;

    void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
        turnManager = FindAnyObjectByType<TurnManager>();

        closeButton.onClick.AddListener(ClosePanel);
        listPanel.SetActive(false);
    }

    public void TogglePanel()
    {
        if (listPanel.activeSelf) ClosePanel();
        else OpenPanel();
    }

    private void OpenPanel()
    {
        listPanel.SetActive(true);
        PopulateList();
    }

    private void ClosePanel()
    {
        listPanel.SetActive(false);
    }

    private void PopulateList()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        int myPlayerId = turnManager.CurrentPlayerID;

        for (int i = 0; i < turnManager.TotalPlayers - 1; i++)
        {
            if (i == myPlayerId) continue;

            PlayerData pData = playerManager.GetPlayer(i);
            if (pData != null)
            {
                GameObject btnObj = Instantiate(factionButtonPrefab, contentContainer);
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

                string civName = pData.civilization != null ? pData.civilization.civName : $"Player {i}";
                string leaderName = pData.civilization != null ? pData.civilization.leaderName : "Unknown Leader";

                btnText.text = $"{leaderName} ({civName})";
                btnText.color = pData.primaryColor;

                int targetFactionId = i; // Кешуємо для лямбди
                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => OnFactionClicked(targetFactionId));
            }
        }
    }

    private void OnFactionClicked(int factionId)
    {
        ClosePanel();
        diplomacyWindow.OpenWindow(factionId);
    }
}