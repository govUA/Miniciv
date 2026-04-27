using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CityUIBuilder : EditorWindow
{
    [MenuItem("Tools/Generate City UI")]
    public static void CreateCityUI()
    {
        GameObject canvasObj = new GameObject("CityUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        CityUIManager uiManager = canvasObj.AddComponent<CityUIManager>();

        GameObject mainPanel = CreateUIObject("CityUIPanel", canvasObj.transform);
        Image panelImage = mainPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.6f);
        SetStretch(mainPanel.GetComponent<RectTransform>());

        GameObject closeBtnObj = CreateUIObject("CloseButton", mainPanel.transform);
        Button closeButton = closeBtnObj.AddComponent<Button>();
        closeBtnObj.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);
        RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0, 30);
        closeRect.sizeDelta = new Vector2(250, 60);

        TextMeshProUGUI closeText = CreateText("Text", closeBtnObj.transform, "CLOSE", 24);
        closeText.alignment = TextAlignmentOptions.Center;
        SetStretch(closeText.GetComponent<RectTransform>());

        GameObject leftPanel = CreateUIObject("LeftPanel_Projects", mainPanel.transform);
        leftPanel.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0f, 1f);
        leftRect.pivot = new Vector2(0f, 0.5f);
        leftRect.sizeDelta = new Vector2(350, 0);

        GameObject projectContainer = CreateUIObject("ProjectListContainer", leftPanel.transform);
        SetStretch(projectContainer.GetComponent<RectTransform>());
        VerticalLayoutGroup leftLayout = projectContainer.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(20, 20, 20, 20);
        leftLayout.spacing = 15;
        leftLayout.childControlHeight = false;
        leftLayout.childControlWidth = true;

        GameObject rightPanel = CreateUIObject("RightPanel_Stats", mainPanel.transform);
        rightPanel.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.pivot = new Vector2(1f, 0.5f);
        rightRect.sizeDelta = new Vector2(350, 0);

        VerticalLayoutGroup rightLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(20, 20, 20, 20);
        rightLayout.spacing = 25;
        rightLayout.childControlHeight = true;
        rightLayout.childControlWidth = true;

        TextMeshProUGUI nameText = CreateText("CityNameText", rightPanel.transform, "City Name", 32);
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI popText = CreateText("PopulationText", rightPanel.transform, "Population: 1", 20);
        TextMeshProUGUI accumText = CreateText("AccumulatedStatsText", rightPanel.transform,
            "Accumulated:\nFood: 0\nProduction: 0", 20);
        TextMeshProUGUI turnText =
            CreateText("PerTurnStatsText", rightPanel.transform, "Yield (per turn):\n+0 Food", 20);
        TextMeshProUGUI projInfoText =
            CreateText("CurrentProjectText", rightPanel.transform, "Current project: None", 20);
        projInfoText.color = new Color(1f, 0.8f, 0.2f);

        GameObject btnTemplate = CreateUIObject("ProjectButtonTemplate", null);
        btnTemplate.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
        btnTemplate.AddComponent<Button>();
        btnTemplate.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);

        TextMeshProUGUI btnText = CreateText("Text", btnTemplate.transform, "Project Name", 20);
        btnText.alignment = TextAlignmentOptions.Center;
        SetStretch(btnText.GetComponent<RectTransform>());

        uiManager.cityUIPanel = mainPanel;
        uiManager.closeButton = closeButton;
        uiManager.projectListContainer = projectContainer.transform;
        uiManager.projectButtonPrefab = btnTemplate;

        uiManager.cityNameText = nameText;
        uiManager.populationText = popText;
        uiManager.accumulatedStatsText = accumText;
        uiManager.perTurnStatsText = turnText;
        uiManager.currentProjectInfoText = projInfoText;

        Selection.activeGameObject = canvasObj;
        Debug.Log("<color=green>City UI successfully generated and connected!</color>");
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
        return tmp;
    }
}