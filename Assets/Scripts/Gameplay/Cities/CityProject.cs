public enum ProjectType
{
    Unit,
    Building,
    Process
}

public class CityProject
{
    public string name;
    public ProjectType type;
    public int cost;
    public bool requiresTech;
    public TechType requiredTech;

    public CityProject(string name, ProjectType type, int cost)
    {
        this.name = name;
        this.type = type;
        this.cost = cost;
        this.requiresTech = false;
    }

    public CityProject(string name, ProjectType type, int cost, TechType requiredTech)
    {
        this.name = name;
        this.type = type;
        this.cost = cost;
        this.requiresTech = true;
        this.requiredTech = requiredTech;
    }
}