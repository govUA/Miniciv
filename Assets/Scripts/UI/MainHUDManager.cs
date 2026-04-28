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
    public TextMeshProUGUI diplomacyText;
    public TextMeshProUGUI turnText;
    public TMP_Dropdown techDropdown;

    [Header("Action Elements")] public Button nextTurnButton;

    private EconomyManager economyManager;
    private TechManager techManager;
    private TurnManager turnManager;
    private PlayerManager playerManager;

    private List<TechType> displayedTechs = new List<TechType>();

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
            nextTurnButton.onClick.AddListener(turnManager.EndTurn);
        }

        if (techDropdown != null)
        {
            techDropdown.onValueChanged.AddListener(OnTechSelected);
        }

        if (turnManager != null)
        {
            turnManager.OnPlayerChanged += RefreshTechDropdown;
        }

        RefreshTechDropdown(turnManager != null ? turnManager.CurrentPlayerID : 0);
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

                goldText.text = $"💰 {currentGold} ({sign}{income})";
            }
        }

        if (techManager != null)
        {
            int science = techManager.GetAccumulatedResearch(playerId);
            scienceText.text = $"🧪 {science}";
        }

        diplomacyText.text = "🤝 Diplomacy";
        turnText.text = $"TURN: {turnManager.CurrentTurn}";
    }

    private void HideHUD()
    {
        if (hudContentPanel != null)
        {
            hudContentPanel.SetActive(false);
        }
    }

    private void ShowHUD()
    {
        if (hudContentPanel != null)
        {
            hudContentPanel.SetActive(true);
        }
    }

    public void RefreshTechDropdown(int playerId)
    {
        if (techDropdown == null || techManager == null) return;

        techDropdown.ClearOptions();
        displayedTechs.Clear();

        List<string> options = new List<string>();
        options.Add("--- Select Research ---");

        foreach (TechType tech in Enum.GetValues(typeof(TechType)))
        {
            if (techManager.CanResearch(playerId, tech) && !techManager.HasTech(playerId, tech))
            {
                options.Add(tech.ToString());
                displayedTechs.Add(tech);
            }
        }

        techDropdown.AddOptions(options);

        TechType? current = techManager.GetCurrentResearch(playerId);
        if (current.HasValue)
        {
            int index = displayedTechs.IndexOf(current.Value);
            techDropdown.SetValueWithoutNotify(index + 1);
        }
    }

    private void OnTechSelected(int index)
    {
        if (index == 0) return;

        int playerId = turnManager.CurrentPlayerID;
        TechType selectedTech = displayedTechs[index - 1];

        techManager.SetResearch(playerId, selectedTech);
        Debug.Log($"[HUD] Research started: {selectedTech}");
    }

    private void OnDestroy()
    {
        if (turnManager != null) turnManager.OnPlayerChanged -= RefreshTechDropdown;
    }
}