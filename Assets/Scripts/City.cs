using UnityEngine;
using System.Collections.Generic;
using TMPro;

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

    public CityProject currentProject;
    public List<string> builtBuildings = new List<string>();
    public List<HexNode> territoryNodes = new List<HexNode>();

    public TextMeshPro nameText;
    public SpriteRenderer nameBackgroundRenderer;

    private UnitManager unitManager;
    private CityManager cityManager;
    private PlayerManager playerManager;

    public void Initialize(HexNode node, int playerId, string name, UnitManager um, CityManager cm, PlayerManager pm)
    {
        centerNode = node;
        ownerID = playerId;
        cityName = name;
        unitManager = um;
        cityManager = cm;
        playerManager = pm;

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
        if (playerManager != null && !string.IsNullOrEmpty(project.requiredTech))
        {
            PlayerData p = playerManager.GetPlayer(ownerID);
            if (p != null && !p.unlockedTechs.Contains(project.requiredTech))
            {
                Debug.LogWarning($"[CITY] Cannot build {project.name}. Requires tech: {project.requiredTech}!");
                return;
            }
        }

        currentProject = project;
        Debug.Log($"[CITY] {cityName} started building: {project.name} ({project.cost} Prod)");
    }

    public void ProcessTurn(HexGrid grid)
    {
        hasAttackedThisTurn = false;

        int turnFood = 0;
        int turnProd = 0;
        int turnSci = 0;

        foreach (HexNode node in territoryNodes)
        {
            turnFood += node.foodYield;
            turnProd += node.prodYield;
            turnSci += node.sciYield;
        }

        storedFood += turnFood;
        storedProduction += turnProd;

        if (playerManager != null && turnSci > 0) playerManager.AddScience(ownerID, turnSci);

        int foodToGrow = population * 10 + 10;
        if (storedFood >= foodToGrow)
        {
            storedFood -= foodToGrow;
            population++;
            Debug.Log($"[CITY] {cityName} grew! Population is now {population}.");
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
            if (currentProject.name == "Monument" && cityManager != null) cityManager.ExpandTerritoryByOne(this);
        }
        else if (currentProject.type == ProjectType.Unit)
        {
            if (unitManager != null) unitManager.SpawnUnit(centerNode, ownerID, currentProject.name);
        }

        currentProject = null;
    }

    public void SetVisibility(bool isVisible)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = isVisible;
    }
}