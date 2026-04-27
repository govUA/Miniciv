using UnityEngine;
using TMPro;

public class EconomyUIManager : MonoBehaviour
{
    public TextMeshProUGUI goldText;
    public PlayerManager playerManager;
    public EconomyManager economyManager;
    public TurnManager turnManager;

    public int humanPlayerId = 0;

    void OnEnable()
    {
        if (turnManager != null) turnManager.OnPlayerChanged += HandleTurnChange;
    }

    void OnDisable()
    {
        if (turnManager != null) turnManager.OnPlayerChanged -= HandleTurnChange;
    }

    void Start() => UpdateUI();

    private void HandleTurnChange(int playerId)
    {
        if (playerId == humanPlayerId) UpdateUI();
    }

    public void UpdateUI()
    {
        var data = playerManager.GetPlayer(humanPlayerId);
        if (data == null || goldText == null) return;

        int currentGold = data.gold;
        int income = economyManager.GetIncome(humanPlayerId);
        string sign = income >= 0 ? "+" : "";

        goldText.text = $"Gold: {currentGold} ({sign}{income})";
    }
}