using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class City : MonoBehaviour
{
    public int ownerID;
    public HexNode centerNode;
    public string cityName;
    public int visionRange = 2;

    public int maxHP = 120;
    public int currentHP;
    public int garrisonStrength = 15;
    public int attackRange = 2;
    public bool hasAttackedThisTurn = false;

    public int population = 1;
    public int storedFood = 0;
    public int storedProduction = 0;
    public int storedScience = 0;
    public int storedCulture = 0;

    public int borderExpansions = 0;

    public CityProject currentProject;
    public List<string> builtBuildings = new List<string>();
    public List<HexNode> territoryNodes = new List<HexNode>();

    [Header("UI Elements")] public TextMeshProUGUI nameText;
    public Image nameBackgroundRenderer;
    public TextMeshProUGUI populationText;
    public Image projectIcon;

    private UnitManager unitManager;
    private CityManager cityManager;
    private PlayerManager playerManager;
    private TechManager techManager;
    private EconomyManager economyManager;

    public void Initialize(HexNode node, int playerId, string name, UnitManager um, CityManager cm, PlayerManager pm)
    {
        centerNode = node;
        ownerID = playerId;
        cityName = name;
        unitManager = um;
        cityManager = cm;
        playerManager = pm;
        techManager = FindAnyObjectByType<TechManager>();
        economyManager = FindObjectOfType<EconomyManager>();

        currentHP = maxHP;
        UpdateColor();
        UpdateNameplateUI();
    }

    private void UpdateColor()
    {
        if (nameText != null) nameText.text = cityName;

        if (playerManager != null)
        {
            PlayerData pd = playerManager.GetPlayer(ownerID);
            if (pd != null)
            {
                if (nameText != null) nameText.color = pd.primaryColor;
                if (nameBackgroundRenderer != null) nameBackgroundRenderer.color = pd.secondaryColor;
            }
        }
    }

    public void UpdateNameplateUI()
    {
        if (populationText != null) populationText.text = population.ToString();

        if (projectIcon != null)
        {
            bool isMyCity = (ownerID == 0);

            if (!isMyCity)
            {
                PlayerData pd = playerManager?.GetPlayer(ownerID);
                string civName = pd?.civilization != null ? pd.civilization.civName : "Unknown";
                Sprite civSprite = Resources.Load<Sprite>($"Icons/Civilizations/{civName}");

                projectIcon.sprite = civSprite;
                projectIcon.enabled = (civSprite != null);
            }
            else
            {
                if (currentProject != null)
                {
                    string folder = currentProject.type switch
                    {
                        ProjectType.Building => "Icons/Projects/Buildings",
                        ProjectType.Unit => "Icons/Projects/Units",
                        ProjectType.Process => "Icons/Projects/Projects",
                        _ => "Icons/Projects"
                    };
                    Sprite loadedIcon = Resources.Load<Sprite>($"{folder}/{currentProject.name}");
                    projectIcon.sprite = loadedIcon;
                    projectIcon.enabled = (loadedIcon != null);
                }
                else
                {
                    Sprite noneSprite = Resources.Load<Sprite>("Icons/Projects/Projects/None");
                    projectIcon.sprite = noneSprite;
                    projectIcon.enabled = (noneSprite != null);
                }
            }
        }
    }

    public void TakeDamage(int amount, int attackerId)
    {
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        Debug.Log($"[COMBAT] City {cityName} took {amount} damage. HP: {currentHP}/{maxHP}");
        if (currentHP <= 0) CaptureCity(attackerId);
    }

    private void CaptureCity(int newOwner)
    {
        Debug.Log($"[COMBAT] City {cityName} captured by Player {newOwner}!");
        ownerID = newOwner;
        currentHP = maxHP / 2;
        currentProject = null;
        hasAttackedThisTurn = true;

        UpdateColor();
        UpdateNameplateUI();
        if (cityManager != null) cityManager.UpdateCityOwnership(this);
    }

    public void SetProject(CityProject project)
    {
        if (techManager != null && project.requiresTech && !string.IsNullOrEmpty(project.requiredTech))
        {
            if (!techManager.HasTech(ownerID, project.requiredTech))
            {
                Debug.LogWarning($"[CITY] Cannot build {project.name}. Requires tech: {project.requiredTech}!");
                return;
            }
        }

        if (project.type == ProjectType.Unit && unitManager != null)
        {
            if (unitManager.unitDatabaseDict.TryGetValue(project.name, out UnitDataModel uData))
            {
                if (uData.requiredPopulation > 0 && population < uData.requiredPopulation)
                {
                    Debug.LogWarning(
                        $"[CITY] Cannot build {project.name}. Requires population: {uData.requiredPopulation}!");
                    return;
                }
            }
        }

        currentProject = project;
        Debug.Log($"[CITY] {cityName} started building: {project.name} ({project.cost} Prod)");
        UpdateNameplateUI();
    }

    public void ProcessTurn(HexGrid grid)
    {
        hasAttackedThisTurn = false;

        int turnFood = 1;
        int turnProd = 1;
        int turnSci = 1;
        int turnCulture = 1;

        foreach (HexNode node in territoryNodes)
        {
            turnFood += node.foodYield;
            turnProd += node.prodYield;
            turnSci += node.sciYield;
        }

        int militaryProdBonus = 0;
        int navalProdBonus = 0;

        foreach (string buildingName in builtBuildings)
        {
            if (cityManager != null &&
                cityManager.buildingDatabaseDict.TryGetValue(buildingName, out BuildingDataModel bData))
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

        int finalTurnProd = turnProd;
        if (currentProject != null && currentProject.type == ProjectType.Unit)
        {
            if (unitManager != null &&
                unitManager.unitDatabaseDict.TryGetValue(currentProject.name, out UnitDataModel uData))
            {
                int appliedBonus = (uData.unitClass == "Naval")
                    ? navalProdBonus
                    : (uData.unitClass != "Civilian" ? militaryProdBonus : 0);

                if (appliedBonus > 0)
                {
                    float multiplier = 1f + (appliedBonus / 100f);
                    finalTurnProd = Mathf.RoundToInt(turnProd * multiplier);
                }
            }
        }

        int currentHappiness = economyManager != null ? economyManager.GetHappiness(ownerID) : 10;

        if (currentHappiness < 0)
        {
            turnFood = Mathf.RoundToInt(turnFood * 0.25f);
            Debug.Log($"[CITY] {cityName} growth dramatically slowed due to Unhappiness!");
        }
        else if (currentHappiness >= 20)
        {
            turnFood = Mathf.RoundToInt(turnFood * 1.20f);
        }

        if (currentHappiness <= -10)
        {
            finalTurnProd = Mathf.RoundToInt(finalTurnProd * 0.5f);
            turnSci = Mathf.RoundToInt(turnSci * 0.5f);
            Debug.Log($"[CITY] {cityName} production/science halved due to EXTREME Unhappiness!");
        }

        storedFood += turnFood;
        storedProduction += finalTurnProd;
        storedCulture += turnCulture;

        if (techManager != null && turnSci > 0)
        {
            techManager.AddScience(ownerID, turnSci);
        }

        int foodToGrow = 8 + 8 * (population - 1) + (int)Mathf.Pow(population - 1, 1.5f);

        if (storedFood >= foodToGrow)
        {
            storedFood -= foodToGrow;
            population++;

            int nextFoodReq = 8 + 8 * (population - 1) + (int)Mathf.Pow(population - 1, 1.5f);
            Debug.Log(
                $"[CITY] {cityName} grew! Population is now {population}. Next citizen needs {nextFoodReq} food.");

            UpdateNameplateUI();
        }

        int cultureThreshold = 5 + (int)(6 * Mathf.Pow(borderExpansions, 1.3f));

        if (storedCulture >= cultureThreshold)
        {
            storedCulture -= cultureThreshold;
            borderExpansions++;

            if (cityManager != null)
            {
                cityManager.ExpandTerritoryByOne(this);
            }
        }

        if (currentProject != null)
        {
            if (currentProject.type == ProjectType.Process && currentProject.name == "Repair")
            {
                if (currentHP < maxHP)
                {
                    currentHP = Mathf.Clamp(currentHP + 20, 0, maxHP);
                    if (currentHP == maxHP)
                    {
                        currentProject = null;
                        UpdateNameplateUI();
                    }
                }
                else
                {
                    currentProject = null;
                    UpdateNameplateUI();
                }
            }
            else
            {
                if (storedProduction >= currentProject.cost)
                {
                    storedProduction -= currentProject.cost;
                    FinishProject();
                }
            }
        }
    }

    private void FinishProject()
    {
        Debug.Log($"[CITY] {cityName} completed: {currentProject.name}!");

        if (currentProject.type == ProjectType.Building)
        {
            builtBuildings.Add(currentProject.name);

            if (cityManager != null &&
                cityManager.buildingDatabaseDict.TryGetValue(currentProject.name, out BuildingDataModel bData))
            {
                if (bData.effects != null)
                {
                    foreach (var effect in bData.effects)
                    {
                        if (effect.type == "MaxHP")
                        {
                            maxHP += effect.amount;
                            currentHP += effect.amount;
                        }
                        else if (effect.type == "Garrison")
                        {
                            garrisonStrength += effect.amount;
                        }
                    }
                }
            }
        }
        else if (currentProject.type == ProjectType.Unit)
        {
            if (unitManager != null &&
                unitManager.unitDatabaseDict.TryGetValue(currentProject.name, out UnitDataModel uData))
            {
                if (uData.populationCost > 0)
                {
                    population -= uData.populationCost;
                    population = Mathf.Max(1, population);
                }

                unitManager.SpawnUnit(centerNode, ownerID, currentProject.name);
            }
        }

        currentProject = null;
        UpdateNameplateUI();
    }

    public void SetVisibility(bool isVisible)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = isVisible;

        Canvas cityCanvas = GetComponentInChildren<Canvas>();
        if (cityCanvas != null) cityCanvas.enabled = isVisible;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(screenPosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                CityUIManager uiManager = FindAnyObjectByType<CityUIManager>();
                if (uiManager != null) uiManager.OpenCityUI(this);
            }
        }
    }
}