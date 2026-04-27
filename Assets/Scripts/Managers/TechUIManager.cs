using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TechUIManager : MonoBehaviour
{
    public TMP_Dropdown techDropdown;
    public TextMeshProUGUI currentResearchText;
    public TechManager techManager;
    public TurnManager turnManager;

    public int humanPlayerId = 0;
    private List<TechType> availableTechs = new List<TechType>();

    void OnEnable()
    {
        if (turnManager != null) turnManager.OnPlayerChanged += HandleTurnChange;
    }

    void OnDisable()
    {
        if (turnManager != null) turnManager.OnPlayerChanged -= HandleTurnChange;
    }

    void Start()
    {
        UpdateUI();
    }

    private void HandleTurnChange(int playerId)
    {
        if (playerId == humanPlayerId)
        {
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        if (techManager == null || techDropdown == null) return;

        techDropdown.ClearOptions();
        availableTechs.Clear();
        List<string> options = new List<string>();

        options.Add("Select Tech...");

        foreach (TechType tech in System.Enum.GetValues(typeof(TechType)))
        {
            if (techManager.CanResearch(humanPlayerId, tech))
            {
                availableTechs.Add(tech);
                int cost = techManager.GetTechCost(tech);
                options.Add($"{tech} ({cost} Sci)");
            }
        }

        techDropdown.AddOptions(options);
        techDropdown.value = 0;

        techDropdown.onValueChanged.RemoveAllListeners();
        techDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        UpdateResearchText();
    }

    public void OnDropdownValueChanged(int index)
    {
        if (index > 0 && index <= availableTechs.Count)
        {
            TechType selectedTech = availableTechs[index - 1];
            techManager.SetResearch(humanPlayerId, selectedTech);
            UpdateResearchText();
        }
    }

    private void UpdateResearchText()
    {
        if (currentResearchText == null) return;

        TechType? current = techManager.GetCurrentResearch(humanPlayerId);

        if (current.HasValue)
        {
            int currentSci = techManager.GetAccumulatedResearch(humanPlayerId);
            int totalSci = techManager.GetTechCost(current.Value);
            currentResearchText.text = $"Researching: {current.Value} ({currentSci}/{totalSci})";
        }
        else
        {
            currentResearchText.text = "No Active Research";
        }
    }
}