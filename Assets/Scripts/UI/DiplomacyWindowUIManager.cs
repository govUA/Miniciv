using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiplomacyWindowUIManager : MonoBehaviour
{
    [Header("UI Elements")] public GameObject windowPanel;
    public TextMeshProUGUI leaderNameText;
    public TextMeshProUGUI relationshipStatusText;
    public TextMeshProUGUI feedbackText;

    [Header("Action Buttons")] public Button declareWarButton;
    public Button proposePeaceButton;
    public Button formAllianceButton;
    public Button closeButton;

    private PlayerManager playerManager;
    private TurnManager turnManager;
    private DiplomacyManager diplomacyManager;
    private int currentTargetId = -1;

    void Awake()
    {
        playerManager = FindAnyObjectByType<PlayerManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
        diplomacyManager = FindAnyObjectByType<DiplomacyManager>();

        closeButton.onClick.AddListener(CloseWindow);
        declareWarButton.onClick.AddListener(OnDeclareWarClicked);
        proposePeaceButton.onClick.AddListener(OnProposePeaceClicked);
        formAllianceButton.onClick.AddListener(OnFormAllianceClicked);

        windowPanel.SetActive(false);
    }

    public void OpenWindow(int targetPlayerId)
    {
        currentTargetId = targetPlayerId;
        windowPanel.SetActive(true);
        feedbackText.text = "";
        UpdateUI();
    }

    private void CloseWindow()
    {
        windowPanel.SetActive(false);
        currentTargetId = -1;
    }

    private void UpdateUI()
    {
        if (currentTargetId == -1) return;

        PlayerData targetData = playerManager.GetPlayer(currentTargetId);
        int myId = turnManager.CurrentPlayerID;
        DiplomaticState state = diplomacyManager.GetState(myId, currentTargetId);

        leaderNameText.text = targetData.civilization != null
            ? targetData.civilization.leaderName
            : $"Player {currentTargetId}";

        relationshipStatusText.text = $"Status: {state}";
        switch (state)
        {
            case DiplomaticState.War: relationshipStatusText.color = Color.red; break;
            case DiplomaticState.Alliance: relationshipStatusText.color = Color.cyan; break;
            default: relationshipStatusText.color = Color.white; break;
        }

        declareWarButton.gameObject.SetActive(state != DiplomaticState.War);
        proposePeaceButton.gameObject.SetActive(state == DiplomaticState.War);
        formAllianceButton.gameObject.SetActive(state == DiplomaticState.Neutral);
    }

    private void OnDeclareWarClicked()
    {
        int myId = turnManager.CurrentPlayerID;
        diplomacyManager.SetState(myId, currentTargetId, DiplomaticState.War);
        feedbackText.text = "You declared war!";
        UpdateUI();
    }

    private void OnProposePeaceClicked()
    {
        int myId = turnManager.CurrentPlayerID;
        bool accepted = diplomacyManager.ProposePeace(myId, currentTargetId);

        if (accepted)
        {
            feedbackText.text = "They accepted your peace treaty.";
        }
        else
        {
            feedbackText.text = "They REFUSED your peace offer!";
        }

        UpdateUI();
    }

    private void OnFormAllianceClicked()
    {
        int myId = turnManager.CurrentPlayerID;
        bool accepted = diplomacyManager.ProposeAlliance(myId, currentTargetId);

        if (accepted)
        {
            feedbackText.text = "An alliance has been formed.";
        }
        else
        {
            feedbackText.text = "They are not interested in an alliance right now.";
        }

        UpdateUI();
    }
}