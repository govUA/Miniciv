using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainHUDBuilder : EditorWindow
{
    [MenuItem("Tools/Generate Main HUD")]
    public static void CreateMainHUD()
    {
        GameObject canvasObj = new GameObject("MainHUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        MainHUDManager hudManager = canvasObj.AddComponent<MainHUDManager>();

        GameObject topBar = CreateUIObject("TopBar", canvasObj.transform);
        topBar.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        RectTransform topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(0, 70);

        HorizontalLayoutGroup layout = topBar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 30;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;

        TMP_Dropdown dropdown = CreateRobustDropdown(topBar.transform);
        hudManager.techDropdown = dropdown;

        hudManager.goldText = CreateText("GoldText", topBar.transform, "💰 0", 22);
        hudManager.scienceText = CreateText("ScienceText", topBar.transform, "🧪 0", 22);
        hudManager.diplomacyText = CreateText("DiplomacyText", topBar.transform, "🤝 Diplomacy", 22);

        hudManager.turnText = CreateText("TurnText", topBar.transform, "TURN: 1", 22);
        hudManager.turnText.alignment = TextAlignmentOptions.Right;

        GameObject nextTurnObj = CreateUIObject("NextTurnButton", canvasObj.transform);
        Button btn = nextTurnObj.AddComponent<Button>();
        nextTurnObj.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f, 1f);

        RectTransform btnRect = nextTurnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 0);
        btnRect.anchorMax = new Vector2(1, 0);
        btnRect.pivot = new Vector2(1, 0);
        btnRect.anchoredPosition = new Vector2(-30, 30);
        btnRect.sizeDelta = new Vector2(220, 80);

        TextMeshProUGUI btnText = CreateText("Text", nextTurnObj.transform, "NEXT TURN", 20);
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        SetStretch(btnText.GetComponent<RectTransform>());

        hudManager.nextTurnButton = btn;

        Selection.activeGameObject = canvasObj;
        Debug.Log("<color=green>HUD generated successfully.</color>");
    }

    private static TMP_Dropdown CreateRobustDropdown(Transform parent)
    {
        GameObject root = CreateUIObject("TechDropdown", parent);
        root.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);

        GameObject label = CreateUIObject("Label", root.transform);
        TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = 18;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = Color.white;
        RectTransform labelRT = label.GetComponent<RectTransform>();
        SetStretch(labelRT);
        labelRT.offsetMin = new Vector2(10, 0);
        dropdown.captionText = labelText;

        GameObject template = CreateUIObject("Template", root.transform);
        template.SetActive(false);
        template.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        RectTransform templateRT = template.GetComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = new Vector2(0, 0);
        templateRT.sizeDelta = new Vector2(0, 150);
        dropdown.template = templateRT;

        GameObject viewport = CreateUIObject("Viewport", template.transform);
        viewport.AddComponent<Image>().color = Color.white;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        SetStretch(viewport.GetComponent<RectTransform>());
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 28);
        scrollRect.content = contentRT;

        GameObject item = CreateUIObject("Item", content.transform);
        Toggle toggle = item.AddComponent<Toggle>();
        RectTransform itemRT = item.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 25);

        GameObject itemBg = CreateUIObject("Item Background", item.transform);
        Image bgImage = itemBg.AddComponent<Image>();
        bgImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        SetStretch(itemBg.GetComponent<RectTransform>());
        toggle.targetGraphic = bgImage;

        GameObject itemLabel = CreateUIObject("Item Label", item.transform);
        TextMeshProUGUI itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
        itemLabelText.fontSize = 18;
        itemLabelText.alignment = TextAlignmentOptions.Left;
        itemLabelText.color = Color.white;
        RectTransform itemLabelRT = itemLabel.GetComponent<RectTransform>();
        SetStretch(itemLabelRT);
        itemLabelRT.offsetMin = new Vector2(10, 0);

        dropdown.itemText = itemLabelText;

        return dropdown;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string placeholder, int fontSize)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = placeholder;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(180, 50);
        return tmp;
    }
}