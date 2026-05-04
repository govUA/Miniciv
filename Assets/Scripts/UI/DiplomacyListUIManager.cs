using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DiplomacyListUIManager : MonoBehaviour
{
    [Header("UI Panels")] public GameObject listPanel;
    public Transform contentContainer;
    public GameObject factionButtonPrefab;

    [Header("Dependencies")] public DiplomacyWindowUIManager diplomacyWindow;

    private PlayerManager playerManager;
    private TurnManager turnManager;

    void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
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
                FactionButtonUI btnUI = btnObj.GetComponent<FactionButtonUI>();

                string civName = pData.civilization != null ? pData.civilization.civName : $"Faction {i}";
                string leaderName = pData.civilization != null ? pData.civilization.leaderName : "Unknown Leader";

                btnUI.leaderNameText.text = leaderName;
                btnUI.civNameText.text = civName;

                Sprite leaderSprite = Resources.Load<Sprite>($"Icons/Leaders/{leaderName}");
                if (leaderSprite != null) btnUI.leaderIcon.sprite = leaderSprite;

                Sprite civSprite = Resources.Load<Sprite>($"Icons/Civilizations/{civName}");
                if (civSprite != null) btnUI.civIcon.sprite = civSprite;

                int targetFactionId = i;
                btnUI.button.onClick.AddListener(() => OnFactionClicked(targetFactionId));
            }
        }
    }

    private void OnFactionClicked(int factionId)
    {
        ClosePanel();
        diplomacyWindow.OpenWindow(factionId);
    }
}