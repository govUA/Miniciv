using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class CityUIManager : MonoBehaviour
{
    public static event Action OnCityUIOpened;
    public static event Action OnCityUIClosed;

    [Header("Main UI")] public GameObject cityUIPanel;
    public Button closeButton;

    [Header("Left Panel: Projects List")] public Transform projectListContainer;
    public GameObject projectButtonPrefab;

    [Header("Left Panel: Current Project")]
    public GameObject currentProjectContainer;

    public Image currentProjectIcon;
    public TextMeshProUGUI currentProjectNameText;
    public TextMeshProUGUI currentProjectProgressText;
    public Image currentProjectProgressBar;

    [Header("Right Panel: City Statistics")]
    public TextMeshProUGUI cityNameText;

    public TextMeshProUGUI populationText;
    public TextMeshProUGUI accumulatedStatsText;
    public TextMeshProUGUI perTurnStatsText;

    private City currentCity;

    void Start()
    {
        closeButton.onClick.AddListener(CloseCityUI);
        cityUIPanel.SetActive(false);
    }

    public void OpenCityUI(City city)
    {
        currentCity = city;
        cityUIPanel.SetActive(true);
        UpdateCityStats();
        PopulateProjects();

        OnCityUIOpened?.Invoke();
    }

    public void CloseCityUI()
    {
        cityUIPanel.SetActive(false);
        currentCity = null;

        OnCityUIClosed?.Invoke();
    }

    private void UpdateCityStats()
    {
        if (currentCity == null) return;

        cityNameText.text = currentCity.cityName;
        populationText.text = $"Population: {currentCity.population}";

        accumulatedStatsText.text = $"Accumulated:\n" +
                                    $"Food: {currentCity.storedFood}\n" +
                                    $"Production: {currentCity.storedProduction}\n" +
                                    $"Health: {currentCity.currentHP}/{currentCity.maxHP}";

        int turnFood = 0, turnProd = 0, turnSci = 0;
        foreach (var node in currentCity.territoryNodes)
        {
            turnFood += node.foodYield;
            turnProd += node.prodYield;
            turnSci += node.sciYield;
        }

        perTurnStatsText.text = $"Yield (per turn):\n" +
                                $"+{turnFood} Food\n" +
                                $"+{turnProd} Production\n" +
                                $"+{turnSci} Science";

        UpdateCurrentProjectUI();
    }

    private void UpdateCurrentProjectUI()
    {
        if (currentCity.currentProject != null)
        {
            currentProjectContainer.SetActive(true);
            CityProject proj = currentCity.currentProject;

            currentProjectNameText.text = proj.name;

            if (proj.type == ProjectType.Process)
            {
                currentProjectProgressText.text = "Active Process";
                if (currentProjectProgressBar != null) currentProjectProgressBar.fillAmount = 1f;
            }
            else
            {
                currentProjectProgressText.text = $"{currentCity.storedProduction} / {proj.cost} Prod.";
                if (currentProjectProgressBar != null && proj.cost > 0)
                {
                    currentProjectProgressBar.fillAmount = (float)currentCity.storedProduction / proj.cost;
                }
            }

            string folder = proj.type switch
            {
                ProjectType.Building => "Icons/Projects/Buildings",
                ProjectType.Unit => "Icons/Projects/Units",
                ProjectType.Process => "Icons/Projects/Projects",
                _ => "Icons/Projects"
            };

            Sprite loadedIcon = Resources.Load<Sprite>($"{folder}/{proj.name}");
            if (loadedIcon != null && currentProjectIcon != null)
            {
                currentProjectIcon.sprite = loadedIcon;
            }
        }
        else
        {
            currentProjectContainer.SetActive(false);
        }
    }

    private void PopulateProjects()
    {
        foreach (Transform child in projectListContainer)
        {
            Destroy(child.gameObject);
        }

        List<CityProject> availableProjects = new List<CityProject>();

        UnitManager unitManager = FindAnyObjectByType<UnitManager>();
        TechManager techManager = FindAnyObjectByType<TechManager>();

        if (unitManager != null && currentCity != null)
        {
            foreach (var kvp in unitManager.unitDatabaseDict)
            {
                UnitDataModel unitData = kvp.Value;
                bool canBuild = true;

                if (!string.IsNullOrEmpty(unitData.requiredTech))
                {
                    if (techManager == null || !techManager.HasTech(currentCity.ownerID, unitData.requiredTech))
                    {
                        canBuild = false;
                    }
                }

                if (canBuild)
                {
                    availableProjects.Add(new CityProject(unitData.name, ProjectType.Unit, unitData.cost,
                        unitData.requiredTech));
                }
            }
        }

        availableProjects.Add(new CityProject("Monument", ProjectType.Building, 60));
        availableProjects.Add(new CityProject("Repair", ProjectType.Process, 0));

        foreach (CityProject proj in availableProjects)
        {
            GameObject btnObj = Instantiate(projectButtonPrefab, projectListContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            btnText.text = proj.type == ProjectType.Process
                ? proj.name
                : $"{proj.name}\n<size=80%>{proj.cost} Prod.</size>";

            Image projectIcon = btnObj.transform.Find("Icon")?.GetComponent<Image>();
            if (projectIcon != null)
            {
                string folder = proj.type switch
                {
                    ProjectType.Building => "Icons/Projects/Buildings",
                    ProjectType.Unit => "Icons/Projects/Units",
                    ProjectType.Process => "Icons/Projects/Projects",
                    _ => "Icons/Projects"
                };

                Sprite loadedIcon = Resources.Load<Sprite>($"{folder}/{proj.name}");
                if (loadedIcon != null) projectIcon.sprite = loadedIcon;
            }

            Button button = btnObj.GetComponent<Button>();
            button.onClick.AddListener(() => OnProjectSelected(proj));
        }
    }

    private void OnProjectSelected(CityProject project)
    {
        if (currentCity != null)
        {
            currentCity.SetProject(project);
            UpdateCityStats();
        }
    }
}