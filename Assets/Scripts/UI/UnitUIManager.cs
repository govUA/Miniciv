using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitUIManager : MonoBehaviour
{
    [Header("UI Panels")] public GameObject unitActionPanel;

    [Header("Icons")] public Image unitIconImage;

    [Header("Text Elements")] public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI combatStrengthText;

    [Header("Action Buttons")] public Button moveButton;
    public Button fortifyButton;
    public Button healButton;
    public Button skipTurnButton;
    public Button foundCityButton;

    private Unit selectedUnit;

    private CityManager cityManager;
    private UnitManager unitManager;
    private HexInteraction hexInteraction;

    private void Awake()
    {
        cityManager = Object.FindAnyObjectByType<CityManager>();
        unitManager = Object.FindAnyObjectByType<UnitManager>();
        hexInteraction = Object.FindAnyObjectByType<HexInteraction>();

        if (fortifyButton != null) fortifyButton.onClick.AddListener(OnFortifyClicked);
        if (healButton != null) healButton.onClick.AddListener(OnHealClicked);
        if (skipTurnButton != null) skipTurnButton.onClick.AddListener(OnSkipTurnClicked);
        if (foundCityButton != null) foundCityButton.onClick.AddListener(OnFoundCityClicked);
        if (moveButton != null) moveButton.onClick.AddListener(OnMoveClicked);
    }

    private void Start()
    {
        if (selectedUnit == null && unitActionPanel != null)
        {
            unitActionPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (unitActionPanel != null && unitActionPanel.activeSelf && selectedUnit == null)
        {
            SelectUnit(null);
        }
    }

    public void SelectUnit(Unit unit)
    {
        selectedUnit = unit;

        if (selectedUnit == null)
        {
            if (unitActionPanel != null) unitActionPanel.SetActive(false);
            return;
        }

        if (unitActionPanel != null) unitActionPanel.SetActive(true);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (selectedUnit == null) return;

        unitNameText.text = selectedUnit.unitName;

        if (unitIconImage != null)
        {
            Sprite icon = Resources.Load<Sprite>($"Icons/Projects/Units/{selectedUnit.unitName}");

            if (icon != null)
            {
                unitIconImage.sprite = icon;
                unitIconImage.enabled = true;
            }
            else
            {
                unitIconImage.enabled = false;
                Debug.LogWarning($"[UnitUI] Icon not found at: Icons/Projects/Units/{selectedUnit.unitName}");
            }
        }

        statsText.text =
            $"HP: {selectedUnit.currentHP}/{selectedUnit.maxHP}   MP: {selectedUnit.currentMP}/{selectedUnit.maxMP}";

        if (selectedUnit.unitClass != UnitClass.Civilian)
        {
            combatStrengthText.gameObject.SetActive(true);
            combatStrengthText.text = $"Melee: {selectedUnit.meleeStrength}   Ranged: {selectedUnit.rangedStrength}";
        }
        else
        {
            combatStrengthText.gameObject.SetActive(false);
        }

        foundCityButton.gameObject.SetActive(selectedUnit.isSettler);

        bool hasMovement = selectedUnit.currentMP > 0;
        moveButton.interactable = hasMovement;
        fortifyButton.interactable = hasMovement;
        healButton.interactable = hasMovement;
        skipTurnButton.interactable = hasMovement;
        if (selectedUnit.isSettler) foundCityButton.interactable = hasMovement;
    }

    private void OnFortifyClicked()
    {
        if (selectedUnit != null && selectedUnit.currentMP > 0)
        {
            selectedUnit.isFortified = true;
            selectedUnit.isHealing = false;
            selectedUnit.currentMP = 0;
            UpdateUI();
        }
    }

    private void OnHealClicked()
    {
        if (selectedUnit != null && selectedUnit.currentMP > 0)
        {
            selectedUnit.isHealing = true;
            selectedUnit.isFortified = false;
            selectedUnit.currentMP = 0;
            UpdateUI();
        }
    }

    private void OnSkipTurnClicked()
    {
        if (selectedUnit != null && selectedUnit.currentMP > 0)
        {
            selectedUnit.currentMP = 0;
            UpdateUI();
        }
    }

    private void OnMoveClicked()
    {
        Debug.Log("[UI] Натисніть правою кнопкою миші на карті для переміщення.");
    }

    private void OnFoundCityClicked()
    {
        if (selectedUnit != null && selectedUnit.isSettler)
        {
            if (cityManager != null && unitManager != null)
            {
                if (cityManager.FoundCity(selectedUnit))
                {
                    Unit unitToKill = selectedUnit;

                    if (hexInteraction != null)
                    {
                        hexInteraction.ClearSelection();
                    }
                    else
                    {
                        SelectUnit(null);
                    }

                    unitManager.RemoveUnit(unitToKill);
                }
            }
        }
    }
}