using UnityEngine;
using System.Collections.Generic;

public class City : MonoBehaviour
{
    public int ownerID;
    public HexNode centerNode;
    public string cityName;
    public int visionRange = 2;

    public int population = 1;
    public int storedFood = 0;
    public int storedProduction = 0;
    public int storedScience = 0;

    public CityProject currentProject;
    public List<string> builtBuildings = new List<string>();
    public List<HexNode> territoryNodes = new List<HexNode>();

    private UnitManager unitManager;
    private CityManager cityManager;

    public void Initialize(HexNode node, int playerId, string name, UnitManager um, CityManager cm)
    {
        centerNode = node;
        ownerID = playerId;
        cityName = name;
        unitManager = um;
        cityManager = cm;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = (ownerID == 0) ? Color.blue : Color.red;
        }
    }

    public void SetProject(CityProject project)
    {
        currentProject = project;
        Debug.Log($"City {cityName} started building: {project.name} ({project.cost} Prod)");
    }

    public void ProcessTurn(HexGrid grid)
    {
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
        storedScience += turnSci;

        int foodToGrow = population * 10 + 10;
        if (storedFood >= foodToGrow)
        {
            storedFood -= foodToGrow;
            population++;
            Debug.Log($"City {cityName} grew! Population is now {population}.");
        }

        if (currentProject != null)
        {
            if (storedProduction >= currentProject.cost)
            {
                storedProduction -= currentProject.cost;
                FinishProject();
            }
        }
    }

    private void FinishProject()
    {
        Debug.Log($"City {cityName} completed: {currentProject.name}!");

        if (currentProject.type == ProjectType.Building)
        {
            builtBuildings.Add(currentProject.name);

            if (currentProject.name == "Monument")
            {
                if (cityManager != null)
                {
                    cityManager.ExpandTerritoryByOne(this);
                }
            }
        }
        else if (currentProject.type == ProjectType.Unit)
        {
            if (unitManager != null)
            {
                Unit newUnit = unitManager.SpawnUnit(centerNode, ownerID);
                if (newUnit != null)
                {
                    SpriteRenderer sr = newUnit.GetComponent<SpriteRenderer>();
                    if (currentProject.name == "Settler")
                    {
                        newUnit.isSettler = true;
                        if (sr != null) sr.color = (ownerID == 0) ? Color.cyan : new Color(1f, 0.5f, 0f);
                    }
                    else
                    {
                        newUnit.isSettler = false;
                        if (sr != null) sr.color = (ownerID == 0) ? Color.blue : Color.red;
                    }
                }
            }
        }

        currentProject = null;
    }

    public void SetVisibility(bool isVisible)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = isVisible;
        }
    }
}