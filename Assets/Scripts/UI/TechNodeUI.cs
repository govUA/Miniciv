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
    private string baseTechName;

    public void Initialize(string id, string techName, int cost, TechTreeUIManager mgr)
    {
        techId = id;
        baseTechName = techName;
        nameText.text = techName;
        costText.text = $"{cost} <sprite name=\"Science\">";
        manager = mgr;

        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnNodeClicked);
    }

    public void UpdateCompletions(int count)
    {
        if (count > 1)
        {
            nameText.text = $"{baseTechName} ({ToRoman(count)})";
        }
        else
        {
            nameText.text = baseTechName;
        }
    }

    public void UpdateVisualState(bool isUnlocked, bool isResearching, bool canResearch)
    {
        if (isResearching)
        {
            backgroundImage.color = new Color(0.2f, 0.6f, 1f);
            nodeButton.interactable = false;
        }
        else if (canResearch)
        {
            backgroundImage.color = Color.white;
            nodeButton.interactable = true;
        }
        else if (isUnlocked)
        {
            backgroundImage.color = new Color(0.2f, 0.8f, 0.2f);
            nodeButton.interactable = false;
        }
        else
        {
            backgroundImage.color = Color.gray;
            nodeButton.interactable = false;
        }
    }

    private void OnNodeClicked() => manager.OnTechNodeSelected(techId);

    private string ToRoman(int number)
    {
        if (number < 1) return string.Empty;
        if (number >= 1000) return "M" + ToRoman(number - 1000);
        if (number >= 900) return "CM" + ToRoman(number - 900);
        if (number >= 500) return "D" + ToRoman(number - 500);
        if (number >= 400) return "CD" + ToRoman(number - 400);
        if (number >= 100) return "C" + ToRoman(number - 100);
        if (number >= 90) return "XC" + ToRoman(number - 90);
        if (number >= 50) return "L" + ToRoman(number - 50);
        if (number >= 40) return "XL" + ToRoman(number - 40);
        if (number >= 10) return "X" + ToRoman(number - 10);
        if (number >= 9) return "IX" + ToRoman(number - 9);
        if (number >= 5) return "V" + ToRoman(number - 5);
        if (number >= 4) return "IV" + ToRoman(number - 4);
        if (number >= 1) return "I" + ToRoman(number - 1);
        return string.Empty;
    }
}