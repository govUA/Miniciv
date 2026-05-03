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

        int cultureThreshold = 10 + (int)(Mathf.Pow(currentCity.borderExpansions, 1.5f) * 10);

        accumulatedStatsText.text = $"Accumulated:\n" +
                                    $"<sprite name=\"Food\"> Food: {currentCity.storedFood}\n" +
                                    $"<sprite name=\"Production\"> Production: {currentCity.storedProduction}\n" +
                                    $"<sprite name=\"Culture\"> Culture: {currentCity.storedCulture} / {cultureThreshold}\n" +
                                    $"Health: {currentCity.currentHP}/{currentCity.maxHP}";

        int turnFood = 1, turnProd = 1, turnSci = 1;
        int turnCulture = 1;
        int militaryProdBonus = 0;
        int navalProdBonus = 0;

        foreach (var node in currentCity.territoryNodes)
        {
            turnFood += node.foodYield;
            turnProd += node.prodYield;
            turnSci += node.sciYield;
        }

        CityManager cityManager = FindAnyObjectByType<CityManager>();
        if (cityManager != null)
        {
            foreach (string buildingName in currentCity.builtBuildings)
            {
                if (cityManager.buildingDatabaseDict.TryGetValue(buildingName, out BuildingDataModel bData))
                {
                    if (bData.effects != null)
                    {
                        foreach (var effect in bData.effects)
                        {
                            switch (effect.type)
                            {
                                case "Food": turnFood += effect.amount; break;
                                case "Production": turnProd += effect.amount; break;
                                case "Science": turnSci += effect.amount; break;
                                case "Culture": turnCulture += effect.amount; break;
                                case "MilitaryProdBonus": militaryProdBonus += effect.amount; break;
                                case "NavalProdBonus": navalProdBonus += effect.amount; break;
                            }
                        }
                    }
                }
            }
        }

        int displayedProd = turnProd;
        if (currentCity.currentProject != null && currentCity.currentProject.type == ProjectType.Unit)
        {
            UnitManager uManager = FindAnyObjectByType<UnitManager>();
            if (uManager != null &&
                uManager.unitDatabaseDict.TryGetValue(currentCity.currentProject.name, out UnitDataModel uData))
            {
                int appliedBonus = 0;

                if (uData.unitClass == "Naval")
                {
                    appliedBonus = navalProdBonus;
                }
                else if (uData.unitClass != "Civilian")
                {
                    appliedBonus = militaryProdBonus;
                }

                if (appliedBonus > 0)
                {
                    float multiplier = 1f + (appliedBonus / 100f);
                    displayedProd = Mathf.RoundToInt(turnProd * multiplier);
                }
            }
        }

        perTurnStatsText.text = $"Yield (per turn):\n" +
                                $"+{turnFood} <sprite name=\"Food\"> Food\n" +
                                $"+{displayedProd} <sprite name=\"Production\"> Production\n" +
                                $"+{turnSci} <sprite name=\"Science\"> Science\n" +
                                $"+{turnCulture} <sprite name=\"Culture\"> Culture";

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
        CityManager cityManager = FindAnyObjectByType<CityManager>();
        TechManager techManager = FindAnyObjectByType<TechManager>();

        PlayerManager playerManager = FindAnyObjectByType<PlayerManager>();
        PlayerData pd = playerManager != null ? playerManager.GetPlayer(currentCity.ownerID) : null;

        bool hasWaterNeighbor = false;
        HexGrid hexGrid = cityManager != null ? cityManager.GetComponent<HexGrid>() : null;
        if (hexGrid != null && currentCity != null)
        {
            foreach (HexNode neighbor in hexGrid.GetNeighbors(currentCity.centerNode))
            {
                if (!neighbor.isLand)
                {
                    hasWaterNeighbor = true;
                    break;
                }
            }
        }

        if (currentCity != null)
        {
            if (unitManager != null)
            {
                foreach (var kvp in unitManager.unitDatabaseDict)
                {
                    UnitDataModel unitData = kvp.Value;
                    bool canBuild = true;

                    if (unitData.unitClass == "Naval" && !hasWaterNeighbor)
                    {
                        canBuild = false;
                    }

                    if (!string.IsNullOrEmpty(unitData.requiredTech))
                    {
                        if (techManager == null || !techManager.HasTech(currentCity.ownerID, unitData.requiredTech))
                            canBuild = false;
                    }

                    if (unitData.requiredPopulation > 0 && currentCity.population < unitData.requiredPopulation)
                    {
                        canBuild = false;
                    }

                    if (canBuild)
                    {
                        availableProjects.Add(new CityProject(unitData.name, ProjectType.Unit, unitData.cost,
                            unitData.requiredTech));
                    }
                }
            }

            if (cityManager != null)
            {
                foreach (var kvp in cityManager.buildingDatabaseDict)
                {
                    BuildingDataModel bData = kvp.Value;

                    if (currentCity.builtBuildings.Contains(bData.name))
                        continue;

                    bool canBuild = true;

                    if (bData.isWonder)
                    {
                        if (pd == null || !pd.unlockedWonders.Contains(bData.name))
                            canBuild = false;

                        if (canBuild && cityManager.IsWonderBuiltOrBuilding(currentCity.ownerID, bData.name))
                            canBuild = false;
                    }

                    if (bData.name == "Port" && !hasWaterNeighbor)
                    {
                        canBuild = false;
                    }

                    if (!string.IsNullOrEmpty(bData.requiredTech))
                    {
                        if (techManager == null || !techManager.HasTech(currentCity.ownerID, bData.requiredTech))
                            canBuild = false;
                    }

                    if (canBuild)
                    {
                        availableProjects.Add(new CityProject(bData.name, ProjectType.Building, bData.cost,
                            bData.requiredTech));
                    }
                }
            }
        }

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