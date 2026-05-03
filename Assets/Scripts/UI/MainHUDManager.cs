using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class MainHUDManager : MonoBehaviour
{
    [Header("UI Panels")] [Tooltip("Link a HUD panel here, so that it disappears with city UI opening")]
    public GameObject hudContentPanel;

    [Header("Top Bar Elements")] public TextMeshProUGUI goldText;
    public TextMeshProUGUI scienceText;
    public TextMeshProUGUI happinessText;
    public TextMeshProUGUI turnText;
    public Button openTechTreeButton;
    public TechTreeUIManager techTreeUI;

    [Header("Action Elements")] public Button nextTurnButton;

    private EconomyManager economyManager;
    private TechManager techManager;
    private TurnManager turnManager;
    private PlayerManager playerManager;

    private List<string> displayedTechs = new List<string>();

    private void OnEnable()
    {
        CityUIManager.OnCityUIOpened += HideHUD;
        CityUIManager.OnCityUIClosed += ShowHUD;
    }

    private void OnDisable()
    {
        CityUIManager.OnCityUIOpened -= HideHUD;
        CityUIManager.OnCityUIClosed -= ShowHUD;
    }

    void Start()
    {
        economyManager = FindAnyObjectByType<EconomyManager>();
        techManager = FindAnyObjectByType<TechManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
        playerManager = FindAnyObjectByType<PlayerManager>();

        if (nextTurnButton != null && turnManager != null)
        {
            nextTurnButton.onClick.AddListener(() =>
            {
                if (turnManager.CurrentPlayerID == 0)
                {
                    turnManager.EndTurn();
                }
            });
        }

        if (openTechTreeButton != null) openTechTreeButton.onClick.AddListener(() => techTreeUI.ToggleTechTree());
    }

    void Update()
    {
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (turnManager == null) return;
        int playerId = turnManager.CurrentPlayerID;

        if (economyManager != null && playerManager != null)
        {
            var data = playerManager.GetPlayer(playerId);
            if (data != null)
            {
                int currentGold = data.gold;
                int income = economyManager.GetIncome(playerId);
                string sign = income >= 0 ? "+" : "";
                goldText.text = $"<sprite name=\"Gold\"> {currentGold} ({sign}{income})";
            }

            if (happinessText != null)
            {
                int happiness = economyManager.GetHappiness(playerId);
                happinessText.text = $"<sprite name=\"Happiness\"> {happiness}";

                if (happiness < 0) happinessText.color = Color.red;
                else if (happiness < 10) happinessText.color = Color.white;
                else happinessText.color = Color.green;
            }
        }

        if (techManager != null)
        {
            int science = techManager.GetAccumulatedResearch(playerId);
            scienceText.text = $"<sprite name=\"Science\"> {science}";
        }

        turnText.text = $"TURN: {turnManager.CurrentTurn}";

        if (nextTurnButton != null)
        {
            nextTurnButton.interactable = (playerId == 0);
        }
    }

    private void HideHUD()
    {
        if (hudContentPanel != null) hudContentPanel.SetActive(false);
    }

    private void ShowHUD()
    {
        if (hudContentPanel != null) hudContentPanel.SetActive(true);
    }
}