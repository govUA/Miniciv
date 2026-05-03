using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechNodeUI : MonoBehaviour
{
    public string techId;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Image backgroundImage;
    public Button nodeButton;

    private TechTreeUIManager manager;

    public void Initialize(string id, string techName, int cost, TechTreeUIManager mgr)
    {
        techId = id;
        nameText.text = techName;
        costText.text = $"{cost} 🧪";
        manager = mgr;

        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnNodeClicked);
    }

    public void UpdateVisualState(bool isUnlocked, bool isResearching, bool canResearch)
    {
        if (isUnlocked)
        {
            backgroundImage.color = new Color(0.2f, 0.8f, 0.2f);
            nodeButton.interactable = false;
        }
        else if (isResearching)
        {
            backgroundImage.color = new Color(0.2f, 0.6f, 1f);
            nodeButton.interactable = false;
        }
        else if (canResearch)
        {
            backgroundImage.color = Color.white;
            nodeButton.interactable = true;
        }
        else
        {
            backgroundImage.color = Color.gray;
            nodeButton.interactable = false;
        }
    }

    private void OnNodeClicked()
    {
        manager.OnTechNodeSelected(techId);
    }
}