public enum ProjectType
{
    Unit,
    Building
}

public class CityProject
{
    public string name;
    public ProjectType type;
    public int cost;
    public string requiredTech;

    public CityProject(string name, ProjectType type, int cost, string requiredTech = "")
    {
        this.name = name;
        this.type = type;
        this.cost = cost;
        this.requiredTech = requiredTech;
    }
}