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

    [Header("Left Panel: Projects")] public Transform projectListContainer;
    public GameObject projectButtonPrefab;
    public TextMeshProUGUI currentProjectInfoText;

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

        if (currentCity.currentProject != null)
            currentProjectInfoText.text =
                $"Current Project: {currentCity.currentProject.name} ({currentCity.storedProduction}/{currentCity.currentProject.cost})";
        else
            currentProjectInfoText.text = "Current Project: None";
    }

    private void PopulateProjects()
    {
        foreach (Transform child in projectListContainer)
        {
            Destroy(child.gameObject);
        }

        List<CityProject> availableProjects = new List<CityProject>
        {
            new CityProject("Warrior", ProjectType.Unit, 40),
            new CityProject("Settler", ProjectType.Unit, 80),
            new CityProject("Monument", ProjectType.Building, 60),
            new CityProject("Repair", ProjectType.Process, 0)
        };

        foreach (CityProject proj in availableProjects)
        {
            GameObject btnObj = Instantiate(projectButtonPrefab, projectListContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            btnText.text = proj.type == ProjectType.Process
                ? proj.name
                : $"{proj.name} ({proj.cost} Prod.)";

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