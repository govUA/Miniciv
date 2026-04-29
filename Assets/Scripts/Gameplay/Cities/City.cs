using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class City : MonoBehaviour
{
    public int ownerID;
    public HexNode centerNode;
    public string cityName;
    public int visionRange = 2;

    public int maxHP = 200;
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

    public TextMeshPro nameText;
    public SpriteRenderer nameBackgroundRenderer;

    private UnitManager unitManager;
    private CityManager cityManager;
    private PlayerManager playerManager;
    private TechManager techManager;

    public void Initialize(HexNode node, int playerId, string name, UnitManager um, CityManager cm, PlayerManager pm)
    {
        centerNode = node;
        ownerID = playerId;
        cityName = name;
        unitManager = um;
        cityManager = cm;
        playerManager = pm;
        techManager = UnityEngine.Object.FindAnyObjectByType<TechManager>();

        currentHP = maxHP;

        UpdateColor();
    }

    private void UpdateColor()
    {
        if (nameText != null)
        {
            nameText.text = cityName;
        }

        if (playerManager != null)
        {
            PlayerData pd = playerManager.GetPlayer(ownerID);
            if (pd != null)
            {
                if (nameText != null)
                    nameText.color = pd.primaryColor;

                if (nameBackgroundRenderer != null)
                    nameBackgroundRenderer.color = pd.secondaryColor;
            }
        }
    }

    public void TakeDamage(int amount, int attackerId)
    {
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        Debug.Log($"[COMBAT] City {cityName} took {amount} damage. HP: {currentHP}/{maxHP}");
        if (currentHP <= 0)
        {
            CaptureCity(attackerId);
        }
    }

    private void CaptureCity(int newOwner)
    {
        Debug.Log($"[COMBAT] City {cityName} captured by Player {newOwner}!");
        ownerID = newOwner;
        currentHP = maxHP / 2;
        currentProject = null;
        hasAttackedThisTurn = true;

        UpdateColor();
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
                            case "MilitaryProdBonus":
                                militaryProdBonus += effect.amount; break;
                        }
                    }
                }
            }
        }

        int finalTurnProd = turnProd;
        if (militaryProdBonus > 0 && currentProject != null && currentProject.type == ProjectType.Unit)
        {
            if (unitManager != null &&
                unitManager.unitDatabaseDict.TryGetValue(currentProject.name, out UnitDataModel uData))
            {
                if (uData.unitClass != "Civilian")
                {
                    float multiplier = 1f + (militaryProdBonus / 100f);
                    finalTurnProd = Mathf.RoundToInt(turnProd * multiplier);
                    Debug.Log(
                        $"[CITY] {cityName} застосовує бонус {militaryProdBonus}% до виробництва. Base: {turnProd}, Final: {finalTurnProd}");
                }
            }
        }

        storedFood += turnFood;
        storedProduction += finalTurnProd;
        storedCulture += turnCulture;


        if (techManager != null && turnSci > 0)
        {
            techManager.AddScience(ownerID, turnSci);
        }

        int foodToGrow = population * 10 + 10;
        if (storedFood >= foodToGrow)
        {
            storedFood -= foodToGrow;
            population++;
            Debug.Log($"[CITY] {cityName} grew! Population is now {population}.");
        }

        int cultureThreshold = 10 + (int)(Mathf.Pow(borderExpansions, 1.5f) * 10);

        if (storedCulture >= cultureThreshold)
        {
            storedCulture -= cultureThreshold;
            borderExpansions++;

            if (cityManager != null)
            {
                cityManager.ExpandTerritoryByOne(this);
                Debug.Log(
                    $"[CITY] {cityName} expands its borders due to culture! Next threshold: {10 + (int)(Mathf.Pow(borderExpansions, 1.5f) * 10)}");
            }
        }

        if (currentProject != null)
        {
            if (currentProject.type == ProjectType.Process && currentProject.name == "Repair")
            {
                if (currentHP < maxHP)
                {
                    currentHP = Mathf.Clamp(currentHP + 20, 0, maxHP);
                    Debug.Log($"[CITY] {cityName} is repairing. HP: {currentHP}/{maxHP}");
                    if (currentHP == maxHP) currentProject = null;
                }
                else
                {
                    currentProject = null;
                }
            }
            else
            {
                Debug.Log(
                    $"[CITY] {cityName} producing {currentProject.name}: {storedProduction}/{currentProject.cost} Prod.");
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
            if (unitManager != null)
            {
                if (unitManager.unitDatabaseDict.TryGetValue(currentProject.name, out UnitDataModel uData))
                {
                    if (uData.populationCost > 0)
                    {
                        population -= uData.populationCost;
                        population = Mathf.Max(1, population);
                        Debug.Log(
                            $"[CITY] {cityName} spent {uData.populationCost} population to build {uData.name}. Population is now {population}.");
                    }
                }

                unitManager.SpawnUnit(centerNode, ownerID, currentProject.name);
            }
        }

        currentProject = null;
    }

    public void SetVisibility(bool isVisible)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = isVisible;
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
                if (uiManager != null)
                {
                    uiManager.OpenCityUI(this);
                }
            }
        }
    }
}